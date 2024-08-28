using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using MuPDF.NET;
using OnnxPredictors.Inputs;
using OnnxPredictors.Predictors;
using OnnxPredictors.Results;
using SixLabors.ImageSharp.PixelFormats;

namespace Client.Models;

public enum ProcessStatus
{
    None,
    Processing,
    Found,
    NotFound,
    Error
}

public enum FileType
{
    Unknown,
    Pdf,
    Image
}

public class StatusFile : INotifyPropertyChanged, IEquatable<StatusFile>
{
    public static readonly string[] AllowedExtensions = ["*.pdf", "*.jpg", "*.jpeg", "*.png", "*.bmp"];

    private ObservableCollection<IPredictionResult> _predictionResults;

    private ProcessStatus _status;

    public StatusFile(string filePath)
    {
        if (!Path.Exists(filePath)) throw new ArgumentException($"Path {filePath} does not exist");

        FilePath = filePath;
        Type = Path.GetExtension(filePath) switch
        {
            ".pdf" => FileType.Pdf,
            ".jpg" or ".jpeg" or ".png" or ".bmp" => FileType.Image,
            _ => FileType.Unknown
        };
    }

    public string FilePath { get; init; }

    public ObservableCollection<IPredictionResult> PredictionResults
    {
        get => _predictionResults;
        set => SetField(ref _predictionResults, value);
    }

    public FileType Type { get; init; }

    public ProcessStatus Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public bool Equals(StatusFile other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return FilePath == other.FilePath;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((StatusFile)obj);
    }

    public override int GetHashCode()
    {
        return FilePath != null ? FilePath.GetHashCode() : 0;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public void Process(IPredictor predictor)
    {
        if (Status != ProcessStatus.None) return;
        Status = ProcessStatus.Processing;
        PredictionResults = [];

        try
        {
            foreach (var input in GetPredictionInput())
            {
                var results = predictor.Predict(input, null);
                foreach (var result in results) PredictionResults.Add(result);
            }

            Status = PredictionResults.Count > 0 ? ProcessStatus.Found : ProcessStatus.NotFound;
        }
        catch (Exception ex)
        {
            Logging.DefaultLogger.Error(ex);
            Status = ProcessStatus.Error;
        }
    }

    private IEnumerable<IPredictionInput> GetPredictionInput()
    {
        switch (Type)
        {
            case FileType.Pdf:
            {
                var document = new Document(FilePath);
                for (var i = 0; i < document.PageCount; i++)
                {
                    var page = document.LoadPage(i);
                    var pixmap = page.GetPixmap();

                    yield return pixmap.Alpha == 0
                        ? OneImageInput.FromBytes(pixmap.SAMPLES_MV.Span, pixmap.W, pixmap.H)
                        : OneImageInput.FromBytes<Rgba32>(pixmap.SAMPLES_MV.Span, pixmap.W, pixmap.H);
                }

                break;
            }
            case FileType.Image:
                yield return OneImageInput.FromFilepath(FilePath);
                break;
            case FileType.Unknown:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}