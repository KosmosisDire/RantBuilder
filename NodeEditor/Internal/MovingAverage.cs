using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;

namespace NodeBuilder.Internal;

public class MovingAverage
{
    Queue<double> samples;
    Queue<DateTime> sampleTimes;
    double[] sampleArray;
    DateTime[] sampleTimesArray;

    int maxSamples;
    double average;

    public double Value
    {
        get { return average; }
    }

    public int SamplesTaken
    {
        get { return samples.Count; }
    }

    public int MaxSamples
    {
        get { return maxSamples; }
    }

    public bool IsFull
    {
        get { return samples.Count == maxSamples; }
    }

    public TimeSpan WindowTimeSpan
    {
        get { return sampleTimes.Last() - sampleTimes.First(); }
    }

    public TimeSpan AverageDeltaTime
    {
        get { return WindowTimeSpan / SamplesTaken; }
    }


    public MovingAverage(int maxSamples)
    {
        Reset(maxSamples);
    }

    public double AddSample(double sample)
    {
        if(!IsFull)
        {
            sampleArray[SamplesTaken] = sample;
            sampleTimesArray[SamplesTaken] = DateTime.UtcNow;
            samples.Enqueue(sample);
            sampleTimes.Enqueue(DateTime.UtcNow);

            average = sampleArray.Sum() / SamplesTaken;
        }
        else
        {
            average -= samples.Dequeue() / maxSamples;
            samples.Enqueue(sample);
            sampleTimes.Dequeue();
            sampleTimes.Enqueue(DateTime.UtcNow);
            average += sample / maxSamples;
        }

        return average;
    }

    public void Clear()
    {
        samples.Clear();
        sampleArray = new double[maxSamples];
        sampleTimesArray = new DateTime[maxSamples];
        average = 0;
    }

    public void Reset(int sampleCount)
    {
        samples = new Queue<double>(sampleCount);
        sampleTimes = new Queue<DateTime>(sampleCount);
        sampleArray = new double[sampleCount];
        sampleTimesArray = new DateTime[sampleCount];
        maxSamples = sampleCount;
        average = 0;
    }
}


public class MovingAverageVector
{
    MovingAverage X;
    MovingAverage Y;

    public Vector Value
    {
        get { return new Vector(X.Value, Y.Value); }
    }

    public double ValueX
    {
        get { return X.Value; }
    }

    public double ValueY
    {
        get { return Y.Value; }
    }

    public int SamplesTaken
    {
        get { return X.SamplesTaken; }
    }

    public int MaxSamples
    {
        get { return X.MaxSamples; }
    }

    public bool IsFull
    {
        get { return X.IsFull; }
    }

    public TimeSpan WindowTimeSpan => X.WindowTimeSpan;

    public TimeSpan AverageDeltaTime => X.AverageDeltaTime;

    public MovingAverageVector(int maxSamples)
    {
        X = new MovingAverage(maxSamples);
        Y = new MovingAverage(maxSamples);
    }

    public Vector AddSample(Vector sample)
    {
        X.AddSample(sample.X);
        Y.AddSample(sample.Y);

        return Value;
    }

    public void Clear()
    {
        X.Clear();
        Y.Clear();
    }

    public void Reset(int sampleCount)
    {
        X.Reset(sampleCount);
        Y.Reset(sampleCount);
    }

    // implicit conversion from MovingAverageVector to Vector
    public static implicit operator Vector(MovingAverageVector v)
    {
        return v.Value;
    }

}
