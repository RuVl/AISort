using Client.Properties;
using Microsoft.ML.OnnxRuntime;
using NLog;

namespace Client;

internal class Logging : IDisposable
{
    private static Logging _instance;

    private Logging()
    {
        LogManager.Setup().LoadConfigurationFromAssemblyResource(typeof(App).Assembly);

        AppLogger = LogManager.GetLogger("Client");
        AiLogger = LogManager.GetLogger("OnnxRuntime");
    }

    public Logger AppLogger { get; }
    public Logger AiLogger { get; }

    public static Logging Instance => _instance ??= new Logging();

    public static Logger DefaultLogger => _instance.AppLogger;

    public void Dispose()
    {
        AppLogger.Info("App logging disabled");
        AiLogger.Info("AI logging disabled");

        LogManager.Shutdown();
        GC.SuppressFinalize(this);
    }

    public void Load()
    {
        // Tracking global exceptions
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        AppLogger.Info("App logging enabled");

        var env = new EnvironmentCreationOptions
        {
            logLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_INFO,
            loggingFunction = OnnxLogger
        };
        OrtEnv.CreateInstanceWithOptions(ref env);

        AiLogger.Info("AI logging enabled");
    }

    private void OnnxLogger(IntPtr _, OrtLoggingLevel severity, string category, string logId, string codeLocation, string message)
    {
        if (!Settings.Default.EnableLogging) return;

        var logMessage = $"[{category}] [{logId}] {message} at {codeLocation}";
        var logLevel = LogLevel.FromOrdinal((int)severity + 1);

        AiLogger.Log(logLevel, logMessage);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex) AppLogger.Fatal(ex);
    }
}