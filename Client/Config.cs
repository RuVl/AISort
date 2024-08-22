using Client.Properties;
using OnnxPredictors.Predictors;

namespace Client;

internal class Config : IDisposable
{
    private static Config _instance;

    private IPredictor _predictor;

    private Config()
    {
    }

    public static Config Instance => _instance ??= new Config();

    public static IPredictor Predictor
    {
        get => Instance._predictor;
        set => Instance._predictor = value;
    }

    public void Dispose()
    {
        Predictor?.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Load()
    {
        if (string.IsNullOrEmpty(Settings.Default.ModelPath)) return;

        try
        {
            _predictor = YoloPredictor.Create(
                Settings.Default.ModelPath,
                Settings.Default.AiRunner,
                Settings.Default.EnableLogging
            );
        }
        catch
        {
            // ignored
        }
    }
}