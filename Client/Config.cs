using System;
using Client.Properties;
using OnnxPredictors.Predictors;

namespace Client;

internal class Config : IDisposable
{
    private static Config _instance;

    public static Config Instance => _instance ??= new Config();
    
    public IPredictor Predictor { get; set; }
    
    private Config() { }

    public void Load()
    {
        try
        {
            Instance.Predictor = YoloPredictor.Create(
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

    public void Dispose()
    {
        Predictor?.Dispose();
        GC.SuppressFinalize(this);
    }
}
