using PulseMon.Models;

namespace PulseMon.Monitoring;

public sealed class StatusHistory
{
    private const int MaxSamples = 60;
    private readonly Queue<SystemStatus> _samples = new();

    public void Add(SystemStatus status)
    {
        _samples.Enqueue(status);

        while (_samples.Count > MaxSamples)
        {
            _samples.Dequeue();
        }
    }

    public IReadOnlyList<SystemStatus> GetSamples()
    {
        return _samples.ToArray();
    }
}
