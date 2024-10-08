﻿using Microsoft.ML.OnnxRuntime;
using OnnxPredictors.Inputs;
using OnnxPredictors.Labels;
using OnnxPredictors.Parsers;
using OnnxPredictors.Results;

namespace OnnxPredictors.Predictors;

public abstract class BasePredictor : IPredictor
{
    protected BasePredictor()
    {
    }

    protected BasePredictor(string modelPath, ModelRunner runner, bool debug = false)
    {
        Runner = runner;
        ModelPath = modelPath;
        Debug = debug;
        Session = GetSession();
    }

    protected string ModelPath { get; init; }

    protected bool Debug { get; init; }

    protected InferenceSession Session { get; init; }

    public ILabel[] Labels { get; protected init; } = [];

    public ModelRunner Runner { get; protected init; }

    public virtual IPredictionResult[] Predict(IPredictionInput predictionInput, IPredictionParser predictionParser = null)
    {
        throw new NotImplementedException();
    }

    public virtual object Clone()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        Session?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected InferenceSession GetSession()
    {
        return new InferenceSession(ModelPath, GetSessionOptions(Runner, Debug));
    }

    private SessionOptions GetSessionOptions(ModelRunner runner, bool debug = true)
    {
        var sessionOptions = runner switch
        {
            ModelRunner.Cpu => new SessionOptions(),
            ModelRunner.Cuda => SessionOptions.MakeSessionOptionWithCudaProvider(),
            ModelRunner.Tensorrt => SessionOptions.MakeSessionOptionWithTensorrtProvider(),
            ModelRunner.Rocm => SessionOptions.MakeSessionOptionWithRocmProvider(),
            ModelRunner.Tvm => SessionOptions.MakeSessionOptionWithTvmProvider(),
            _ => throw new ArgumentOutOfRangeException(nameof(runner), runner, null)
        };

        sessionOptions.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;
        sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;

        if (!debug) return sessionOptions;

        // sessionOptions.EnableProfiling = true;
        sessionOptions.LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_WARNING;

        return sessionOptions;
    }
}