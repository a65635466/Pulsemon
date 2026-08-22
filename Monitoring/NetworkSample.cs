namespace PulseMon.Monitoring;

internal sealed record NetworkSample(long ReceivedBytes, long SentBytes, DateTime SampledAt);
