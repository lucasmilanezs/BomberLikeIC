using IC.Inputs;
using IC.Calibration;
using IC.Core;
using UnityEngine;

/// <summary>
/// Adapter entre LerSensor e IInputSource.
/// Aplica o SensorProfile ativo com todas as heurísticas configuráveis via toggles.
/// </summary>
public class PlatformInputSource : MonoBehaviour, IInputSource
{
    private LerSensor _sensor;
    private SensorProfile _profile;
    private SignalProcessor _processor;

    private void Start()
    {
        _sensor = GameManager.Instance?.GetComponent<LerSensor>();

        _profile = SensorProfileRepository.Instance != null
            ? SensorProfileRepository.Instance.ActiveProfile
            : null;

        int windowSize = _profile?.movingAverageWindow ?? 10;
        _processor = new SignalProcessor(windowSize);

        if (_sensor == null)
            Debug.LogWarning("[PlatformInputSource] LerSensor não encontrado no GameManager.");

        if (_profile == null)
            Debug.LogWarning("[PlatformInputSource] Nenhum SensorProfile ativo. Usando valores brutos.");
    }

    public Vector2 ReadMove()
    {
        if (_sensor == null) return Vector2.zero;

        // Atualiza perfil ativo em runtime (pode ter sido recalibrado)
        if (SensorProfileRepository.Instance != null)
            _profile = SensorProfileRepository.Instance.ActiveProfile;

        // Raw mode — bypassa tudo
        if (_profile == null || _profile.rawMode)
            return new Vector2(_sensor.XJog, _sensor.YJog);

        // Média móvel
        float rawX = _sensor.XRaw;
        float rawY = _sensor.YRaw;

        if (_profile.useMovingAverage)
        {
            _processor.AddSample(rawX, rawY);
            rawX = _processor.SmoothX;
            rawY = _processor.SmoothY;
        }

        float x = ProcessAxis(
            raw: rawX,
            zero: _profile.zeroX,
            baseline: _profile.baselineX,
            thresholdPositive: _profile.thresholdRight,
            thresholdNegative: _profile.thresholdLeft,
            maxRange: _profile.maxRangeX,
            stdDev: _profile.baselineStdDevX,
            isCompensating: _profile.useCompensationDetection && _processor.IsCompensatingX(rawX),
            activationSamples: _profile.activationSamples,
            hysteresisFactor: _profile.hysteresisFactor,
            isAxisX: true
        );

        float y = ProcessAxis(
            raw: rawY,
            zero: _profile.zeroY,
            baseline: _profile.baselineY,
            thresholdPositive: _profile.thresholdUp,
            thresholdNegative: _profile.thresholdDown,
            maxRange: _profile.maxRangeY,
            stdDev: _profile.baselineStdDevY,
            isCompensating: _profile.useCompensationDetection && _processor.IsCompensatingY(rawY),
            activationSamples: _profile.activationSamples,
            hysteresisFactor: _profile.hysteresisFactor,
            isAxisX: false
        );

        // Descarta diagonal — eixo dominante vence
        if (x != 0 && y != 0)
        {
            if (Mathf.Abs(rawX) >= Mathf.Abs(rawY))
                y = 0;
            else
                x = 0;
        }

        return new Vector2(x, y);
    }

    public bool ConsumePlaceBombPressed()
    {
        // TODO: mapear botão da plataforma
        return false;
    }

    // =========================================================
    //  Pipeline de processamento por eixo
    // =========================================================

    private float ProcessAxis(
        float raw,
        float zero,
        float baseline,
        float thresholdPositive,
        float thresholdNegative,
        float maxRange,
        float stdDev,
        bool isCompensating,
        int activationSamples,
        float hysteresisFactor,
        bool isAxisX)
    {
        // Compensação postural
        if (isCompensating) return 0f;

        // Zero e baseline offset
        float relative = raw;
        if (_profile.useZeroBaseline)
            relative = raw - zero - baseline;

        // Zona morta adaptativa por desvio padrão
        if (_profile.useMovingAverage && _profile.useAdaptiveDeadZone)
        {
            float adaptiveDeadZone = stdDev * 2f;
            if (Mathf.Abs(relative) < adaptiveDeadZone) return 0f;
        }

        if (!_profile.useThreshold) return Mathf.Sign(relative);

        // Normalização por excursão máxima
        float normalizedRelative = relative;
        if (_profile.useMaxRangeNormalization && maxRange > 0f)
            normalizedRelative = relative / maxRange;

        // Threshold — assimétrico ou simétrico
        float threshold;
        if (_profile.useAsymmetricThreshold)
            threshold = relative >= 0 ? thresholdPositive : thresholdNegative;
        else
            threshold = (thresholdPositive + thresholdNegative) * 0.5f;

        // Normaliza o threshold também se normalização ativa
        if (_profile.useMaxRangeNormalization && maxRange > 0f)
            threshold /= maxRange;

        // Velocidade de ativação mínima
        if (_profile.useActivationSamples)
        {
            bool hasActivation = isAxisX
                ? _processor.HasActivationX(normalizedRelative, threshold, activationSamples)
                : _processor.HasActivationY(normalizedRelative, threshold, activationSamples);

            if (!hasActivation) return 0f;
        }

        // Histerese de desativação
        if (_profile.useHysteresis)
        {
            bool active = isAxisX
                ? _processor.ApplyHysteresisX(normalizedRelative, threshold, hysteresisFactor)
                : _processor.ApplyHysteresisY(normalizedRelative, threshold, hysteresisFactor);

            return active ? Mathf.Sign(relative) : 0f;
        }

        // Threshold simples sem histerese
        if (Mathf.Abs(normalizedRelative) < threshold) return 0f;
        return Mathf.Sign(relative);
    }
}