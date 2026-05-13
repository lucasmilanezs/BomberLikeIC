using System;

namespace IC.Calibration
{
    [Serializable]
    public class SensorProfile
    {
        public string id;
        public string patientName;
        public string createdAt;
        public string updatedAt;

        // ── Dados de calibração ──────────────────────────────────

        // Zero — média sem ninguém na plataforma
        public float zeroX;
        public float zeroY;

        // Baseline — média com pessoa parada (relativo ao zero)
        public float baselineX;
        public float baselineY;

        // Desvio padrão do baseline — zona morta adaptativa
        public float baselineStdDevX;
        public float baselineStdDevY;

        // Thresholds assimétricos por direção cardinal
        public float thresholdRight;
        public float thresholdLeft;
        public float thresholdUp;
        public float thresholdDown;

        // Excursão máxima por eixo — normalização
        public float maxRangeX;
        public float maxRangeY;

        // Janela de média móvel calibrada por perfil
        public int movingAverageWindow = 10;

        // Histerese — threshold de desativação (fracção do threshold de ativação)
        public float hysteresisFactor = 0.6f;

        // Velocidade mínima — amostras consecutivas acima do threshold para ativar
        public int activationSamples = 3;

        // ── Toggles de features ──────────────────────────────────

        // Toggle raiz — bypassa tudo e usa sinal bruto
        public bool rawMode = false;

        // Média móvel + janela calibrada por perfil + zona morta adaptativa
        public bool useMovingAverage = true;
        public bool useAdaptiveWindowSize = true;
        public bool useAdaptiveDeadZone = true;

        // Zero e baseline offset
        public bool useZeroBaseline = true;

        // Threshold + assimétrico + normalização
        public bool useThreshold = true;
        public bool useAsymmetricThreshold = true;
        public bool useMaxRangeNormalization = true;

        // Velocidade de ativação mínima
        public bool useActivationSamples = true;

        // Histerese de desativação
        public bool useHysteresis = true;

        // Detecção de compensação postural
        public bool useCompensationDetection = true;

        // ── Factory ─────────────────────────────────────────────

        public static SensorProfile CreateNew(string patientName)
        {
            return new SensorProfile
            {
                id = Guid.NewGuid().ToString(),
                patientName = patientName,
                createdAt = DateTime.UtcNow.ToString("o"),
                updatedAt = DateTime.UtcNow.ToString("o"),
                movingAverageWindow = 10,
                hysteresisFactor = 0.6f,
                activationSamples = 3,

                // Todos os toggles ativos por padrão
                rawMode = false,
                useMovingAverage = true,
                useAdaptiveWindowSize = true,
                useAdaptiveDeadZone = true,
                useZeroBaseline = true,
                useThreshold = true,
                useAsymmetricThreshold = true,
                useMaxRangeNormalization = true,
                useActivationSamples = true,
                useHysteresis = true,
                useCompensationDetection = true
            };
        }

        public void MarkUpdated()
        {
            updatedAt = DateTime.UtcNow.ToString("o");
        }
    }
}