using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IC.Core;
using IC.Gameplay;

namespace IC.Calibration
{
    /// <summary>
    /// Orquestra o wizard de calibração do sensor.
    /// Calcula: zero, baseline, desvio padrão, thresholds assimétricos e maxRange.
    /// </summary>
    public class CalibrationManager : MonoBehaviour
    {
        public static CalibrationManager Instance { get; private set; }

        [Header("Configuração de coleta")]
        [SerializeField] private int collectionSamples = 60;
        [SerializeField] private float sampleInterval = 0.05f;
        [SerializeField] private int findPlayerRetries = 5;
        [SerializeField] private float findPlayerRetryInterval = 0.1f;

        [Header("Refs")]
        [SerializeField] private LevelSpawner levelSpawner;

        // Estado
        public CalibrationStep CurrentStep { get; private set; } = CalibrationStep.SelectProfile;
        public float CollectionProgress { get; private set; }
        public string StatusMessage { get; private set; } = "";
        public bool WaitingCardinalConfirm { get; private set; } = false;

        // Eventos
        public event Action<CalibrationStep> OnStepChanged;
        public event Action<float> OnProgressChanged;
        public event Action OnCalibrationCompleted;
        public event Action OnCardinalReached;

        private SensorProfile _profile;
        private LerSensor _sensor;
        private SignalProcessor _processor;

        private readonly List<float> _samplesX = new();
        private readonly List<float> _samplesY = new();

        // Picos por direção — thresholds assimétricos
        private float _peakRight, _peakLeft, _peakUp, _peakDown;

        private GameObject _player;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _sensor = GameManager.Instance?.GetComponent<LerSensor>();
            if (_sensor == null)
                Debug.LogWarning("[CalibrationManager] LerSensor não encontrado.");

            _processor = new SignalProcessor(windowSize: 10);
        }

        private void Update()
        {
            if (_sensor == null) return;
            _processor.AddSample(_sensor.XRaw, _sensor.YRaw);
        }

        // =========================================================
        //  API pública
        // =========================================================

        public void SelectProfile(SensorProfile profile)
        {
            _profile = profile;
            SensorProfileRepository.Instance.SetActive(profile);
            AdvanceTo(CalibrationStep.CollectingZero);
            StartCoroutine(CollectZeroRoutine());
        }

        public void ConfirmBaseline()
        {
            if (CurrentStep != CalibrationStep.CollectingBaseline) return;
            StartCoroutine(CollectBaselineRoutine());
        }

        public void RegisterDirectionPeak(CalibrationStep direction)
        {
            if (CurrentStep != direction) return;
            if (WaitingCardinalConfirm) return;

            switch (direction)
            {
                case CalibrationStep.CalibratingRight: _peakRight = _processor.SmoothX; break;
                case CalibrationStep.CalibratingLeft: _peakLeft = _processor.SmoothX; break;
                case CalibrationStep.CalibratingUp: _peakUp = _processor.SmoothY; break;
                case CalibrationStep.CalibratingDown: _peakDown = _processor.SmoothY; break;
            }

            WaitingCardinalConfirm = true;
            OnCardinalReached?.Invoke();
        }

        public void ConfirmCardinal()
        {
            if (!WaitingCardinalConfirm) return;
            WaitingCardinalConfirm = false;
            StartCoroutine(DestroyAndRespawn());
        }

        public void SkipStep()
        {
            StopAllCoroutines();
            WaitingCardinalConfirm = false;
            AdvanceToNextStep();
        }

        // =========================================================
        //  Rotinas de coleta
        // =========================================================

        private IEnumerator CollectZeroRoutine()
        {
            StatusMessage = "Aguarde — medindo plataforma vazia...";
            _samplesX.Clear();
            _samplesY.Clear();
            CollectionProgress = 0f;

            for (int i = 0; i < collectionSamples; i++)
            {
                if (_sensor != null)
                {
                    _samplesX.Add(_sensor.XRaw);
                    _samplesY.Add(_sensor.YRaw);
                }
                CollectionProgress = (float)(i + 1) / collectionSamples;
                OnProgressChanged?.Invoke(CollectionProgress);
                yield return new WaitForSeconds(sampleInterval);
            }

            _profile.zeroX = Average(_samplesX);
            _profile.zeroY = Average(_samplesY);
            StatusMessage = "Zero calibrado. Peça ao paciente que suba na plataforma.";
            AdvanceTo(CalibrationStep.CollectingBaseline);
        }

        private IEnumerator CollectBaselineRoutine()
        {
            StatusMessage = "Medindo baseline com paciente parado...";
            _samplesX.Clear();
            _samplesY.Clear();
            CollectionProgress = 0f;

            for (int i = 0; i < collectionSamples; i++)
            {
                if (_sensor != null)
                {
                    _samplesX.Add(_sensor.XRaw);
                    _samplesY.Add(_sensor.YRaw);
                }
                CollectionProgress = (float)(i + 1) / collectionSamples;
                OnProgressChanged?.Invoke(CollectionProgress);
                yield return new WaitForSeconds(sampleInterval);
            }

            // Baseline — média relativa ao zero
            _profile.baselineX = Average(_samplesX) - _profile.zeroX;
            _profile.baselineY = Average(_samplesY) - _profile.zeroY;

            // Desvio padrão — zona morta adaptativa
            _profile.baselineStdDevX = StdDev(_samplesX);
            _profile.baselineStdDevY = StdDev(_samplesY);

            // Janela de média móvel calibrada — maior variância = janela maior
            if (_profile.useAdaptiveWindowSize)
            {
                float avgStdDev = (_profile.baselineStdDevX + _profile.baselineStdDevY) * 0.5f;
                // Mapeia desvio padrão para janela entre 5 e 30
                _profile.movingAverageWindow = Mathf.Clamp(Mathf.RoundToInt(avgStdDev * 0.5f), 5, 30);
                _processor.SetWindowSize(_profile.movingAverageWindow);
            }

            // Aplica perfil parcial em tempo real
            SensorProfileRepository.Instance.Save(_profile);
            SensorProfileRepository.Instance.SetActive(_profile);

            StatusMessage = "Baseline calibrado. Mova-se para a direita!";
            AdvanceTo(CalibrationStep.CalibratingRight);
            StartCoroutine(SpawnAndFindPlayer());
        }

        // =========================================================
        //  Spawn e retry
        // =========================================================

        private IEnumerator DestroyAndRespawn()
        {
            if (_player != null) Destroy(_player);
            _player = null;
            yield return null;

            AdvanceToNextCardinal();

            if (CurrentStep != CalibrationStep.Completed)
                StartCoroutine(SpawnAndFindPlayer());
        }

        private IEnumerator SpawnAndFindPlayer()
        {
            if (levelSpawner == null)
            {
                Debug.LogError("[CalibrationManager] LevelSpawner não configurado.");
                yield break;
            }

            levelSpawner.SpawnLayer(SpawnLayer.Player);

            for (int i = 0; i < findPlayerRetries; i++)
            {
                yield return new WaitForSeconds(findPlayerRetryInterval);

                var playerGo = GameObject.FindGameObjectWithTag("Player");
                if (playerGo != null)
                {
                    _player = playerGo;
                    _processor.Reset();
                    yield break;
                }
            }

            Debug.LogError("[CalibrationManager] Player não encontrado após spawn com retries.");
        }

        // =========================================================
        //  Finalização — thresholds assimétricos + normalização
        // =========================================================

        private void FinalizeCalibration()
        {
            // Thresholds assimétricos por direção
            // Zona morta adaptativa: usa 2*stdDev como piso mínimo do threshold
            float adaptiveFloorX = _profile.baselineStdDevX * 2f;
            float adaptiveFloorY = _profile.baselineStdDevY * 2f;

            _profile.thresholdRight = Mathf.Max(_peakRight * 0.4f, adaptiveFloorX);
            _profile.thresholdLeft = Mathf.Max(Mathf.Abs(_peakLeft) * 0.4f, adaptiveFloorX);
            _profile.thresholdUp = Mathf.Max(_peakUp * 0.4f, adaptiveFloorY);
            _profile.thresholdDown = Mathf.Max(Mathf.Abs(_peakDown) * 0.4f, adaptiveFloorY);

            // Excursão máxima por eixo — para normalização no PlatformInputSource
            _profile.maxRangeX = Mathf.Max(Mathf.Abs(_peakRight), Mathf.Abs(_peakLeft));
            _profile.maxRangeY = Mathf.Max(Mathf.Abs(_peakUp), Mathf.Abs(_peakDown));

            SensorProfileRepository.Instance.Save(_profile);

            if (_player != null) Destroy(_player);

            AdvanceTo(CalibrationStep.Completed);
            OnCalibrationCompleted?.Invoke();
        }

        // =========================================================
        //  Navegação
        // =========================================================

        private void AdvanceToNextCardinal()
        {
            switch (CurrentStep)
            {
                case CalibrationStep.CalibratingRight: AdvanceTo(CalibrationStep.CalibratingLeft); break;
                case CalibrationStep.CalibratingLeft: AdvanceTo(CalibrationStep.CalibratingUp); break;
                case CalibrationStep.CalibratingUp: AdvanceTo(CalibrationStep.CalibratingDown); break;
                case CalibrationStep.CalibratingDown: FinalizeCalibration(); break;
            }
        }

        private void AdvanceTo(CalibrationStep step)
        {
            CurrentStep = step;
            CollectionProgress = 0f;
            OnStepChanged?.Invoke(step);
        }

        private void AdvanceToNextStep()
        {
            int next = (int)CurrentStep + 1;
            if (next <= (int)CalibrationStep.Completed)
                AdvanceTo((CalibrationStep)next);
        }

        // =========================================================
        //  Matemática
        // =========================================================

        private float Average(List<float> samples)
        {
            if (samples.Count == 0) return 0f;
            float sum = 0f;
            foreach (var s in samples) sum += s;
            return sum / samples.Count;
        }

        private float StdDev(List<float> samples)
        {
            if (samples.Count < 2) return 0f;
            float mean = Average(samples);
            float variance = 0f;
            foreach (var s in samples)
                variance += (s - mean) * (s - mean);
            return Mathf.Sqrt(variance / samples.Count);
        }
    }
}