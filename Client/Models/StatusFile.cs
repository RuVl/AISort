using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using OnnxPredictors.Results;

namespace Client.Models;

public enum ProcessStatus
{
    None,
    Processing,
    Found,
    NotFound
}

public class StatusFile : INotifyPropertyChanged, IEquatable<StatusFile>
{
    private readonly string _filename;
    
    private ProcessStatus _status;

    private IPredictionResult[] _predictionResults;

    public string Filename
    {
        get => _filename;
        init
        {
            _filename = value;
            OnPropertyChanged();
        }
    }

    public ProcessStatus Status
    {
        get => _status;
        set
        {
            if (value == _status) return;
            _status = value;
            OnPropertyChanged();
        }
    }

    public IPredictionResult[] PredictionResults
    {
        get => _predictionResults;
        set
        {
            _predictionResults = value;
            OnPropertyChanged();
        }
    }

    public StatusFile(string filename)
    {
        Filename = filename;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public bool Equals(StatusFile other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _filename == other._filename;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((StatusFile)obj);
    }

    public override int GetHashCode()
    {
        return _filename != null ? _filename.GetHashCode() : 0;
    }
}
