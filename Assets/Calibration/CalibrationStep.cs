namespace IC.Calibration
{
    public enum CalibrationStep
    {
        SelectProfile,      // seleciona ou cria perfil
        CollectingZero,     // ninguém na plataforma — coleta zero
        CollectingBaseline, // pessoa parada — coleta baseline
        CalibratingRight,   // move para direita até trigger
        CalibratingLeft,
        CalibratingUp,
        CalibratingDown,
        Completed           // calibração concluída, salva perfil
    }
}
