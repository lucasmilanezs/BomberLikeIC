using System.Collections.Generic;
using UnityEngine;

namespace IC.Calibration
{
    /// <summary>
    /// Processa o sinal bruto do sensor aplicando:
    /// - Média móvel configurável
    /// - Detecção de compensação postural
    /// - Contador de amostras consecutivas acima do threshold (velocidade de ativação)
    /// - Histerese de desativação
    /// </summary>
    public class SignalProcessor
    {
        private readonly Queue<float> _windowX;
        private readonly Queue<float> _windowY;
        private int _windowSize;

        // Compensação postural
        private float _lastPeakX;
        private float _lastPeakY;
        private float _lastPeakTimeX;
        private float _lastPeakTimeY;
        private readonly float _compensationWindowSeconds;

        // Velocidade de ativação — amostras consecutivas acima do threshold
        private int _consecutiveX;
        private int _consecutiveY;

        // Histerese — estado atual de ativação por eixo
        private bool _activeX;
        private bool _activeY;

        public float SmoothX { get; private set; }
        public float SmoothY { get; private set; }

        public SignalProcessor(int windowSize = 10, float compensationWindowSeconds = 0.3f)
        {
            _windowSize = Mathf.Max(1, windowSize);
            _compensationWindowSeconds = compensationWindowSeconds;
            _windowX = new Queue<float>(_windowSize);
            _windowY = new Queue<float>(_windowSize);
        }

        public void AddSample(float rawX, float rawY)
        {
            EnqueueSample(_windowX, rawX);
            EnqueueSample(_windowY, rawY);

            SmoothX = Average(_windowX);
            SmoothY = Average(_windowY);

            UpdatePeaks(rawX, rawY);
        }

        // ── Compensação postural ─────────────────────────────────

        public bool IsCompensatingX(float smoothValue)
        {
            if (_lastPeakX == 0f) return false;
            if (Time.time - _lastPeakTimeX > _compensationWindowSeconds) return false;
            return Mathf.Sign(smoothValue) != Mathf.Sign(_lastPeakX) &&
                   Mathf.Abs(smoothValue) > 0f;
        }

        public bool IsCompensatingY(float smoothValue)
        {
            if (_lastPeakY == 0f) return false;
            if (Time.time - _lastPeakTimeY > _compensationWindowSeconds) return false;
            return Mathf.Sign(smoothValue) != Mathf.Sign(_lastPeakY) &&
                   Mathf.Abs(smoothValue) > 0f;
        }

        // ── Velocidade de ativação ───────────────────────────────

        /// <summary>
        /// Retorna true se o sinal em X sustentou o threshold por N amostras consecutivas.
        /// </summary>
        public bool HasActivationX(float relativeX, float threshold, int requiredSamples)
        {
            if (Mathf.Abs(relativeX) >= threshold)
                _consecutiveX++;
            else
                _consecutiveX = 0;

            return _consecutiveX >= requiredSamples;
        }

        public bool HasActivationY(float relativeY, float threshold, int requiredSamples)
        {
            if (Mathf.Abs(relativeY) >= threshold)
                _consecutiveY++;
            else
                _consecutiveY = 0;

            return _consecutiveY >= requiredSamples;
        }

        // ── Histerese ────────────────────────────────────────────

        /// <summary>
        /// Aplica histerese: ativa em threshold, desativa em threshold * factor.
        /// Evita flickering na borda da zona morta.
        /// </summary>
        public bool ApplyHysteresisX(float relativeX, float threshold, float factor)
        {
            float activateAt = threshold;
            float deactivateAt = threshold * factor;

            if (!_activeX && Mathf.Abs(relativeX) >= activateAt)
                _activeX = true;
            else if (_activeX && Mathf.Abs(relativeX) < deactivateAt)
                _activeX = false;

            return _activeX;
        }

        public bool ApplyHysteresisY(float relativeY, float threshold, float factor)
        {
            float activateAt = threshold;
            float deactivateAt = threshold * factor;

            if (!_activeY && Mathf.Abs(relativeY) >= activateAt)
                _activeY = true;
            else if (_activeY && Mathf.Abs(relativeY) < deactivateAt)
                _activeY = false;

            return _activeY;
        }

        // ── Janela dinâmica ──────────────────────────────────────

        public void SetWindowSize(int size)
        {
            _windowSize = Mathf.Max(1, size);
        }

        // ── Reset ────────────────────────────────────────────────

        public void Reset()
        {
            _windowX.Clear();
            _windowY.Clear();
            _lastPeakX = 0f;
            _lastPeakY = 0f;
            _consecutiveX = 0;
            _consecutiveY = 0;
            _activeX = false;
            _activeY = false;
            SmoothX = 0f;
            SmoothY = 0f;
        }

        // ── Helpers ──────────────────────────────────────────────

        private void EnqueueSample(Queue<float> window, float value)
        {
            if (window.Count >= _windowSize)
                window.Dequeue();
            window.Enqueue(value);
        }

        private float Average(Queue<float> window)
        {
            if (window.Count == 0) return 0f;
            float sum = 0f;
            foreach (var s in window) sum += s;
            return sum / window.Count;
        }

        private void UpdatePeaks(float rawX, float rawY)
        {
            if (Mathf.Abs(rawX) > Mathf.Abs(_lastPeakX))
            {
                _lastPeakX = rawX;
                _lastPeakTimeX = Time.time;
            }

            if (Mathf.Abs(rawY) > Mathf.Abs(_lastPeakY))
            {
                _lastPeakY = rawY;
                _lastPeakTimeY = Time.time;
            }
        }
    }
}