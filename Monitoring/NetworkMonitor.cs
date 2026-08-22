using System.Net.NetworkInformation;

namespace PulseMon.Monitoring;

public sealed class NetworkMonitor
{
    private const double BitsPerMegabit = 1_000_000d;

    private NetworkSample? _previousSample;

    public NetworkSpeedSnapshot GetSpeedSnapshot()
    {
        var currentSample = ReadCurrentSample();

        if (_previousSample is null)
        {
            _previousSample = currentSample;
            return new NetworkSpeedSnapshot(0, 0);
        }

        var elapsedSeconds = (currentSample.SampledAt - _previousSample.SampledAt).TotalSeconds;
        var receivedBytesDelta = currentSample.ReceivedBytes - _previousSample.ReceivedBytes;
        var sentBytesDelta = currentSample.SentBytes - _previousSample.SentBytes;

        _previousSample = currentSample;

        if (elapsedSeconds <= 0)
        {
            return new NetworkSpeedSnapshot(0, 0);
        }

        var downloadMbps = BytesDeltaToMbps(receivedBytesDelta, elapsedSeconds);
        var uploadMbps = BytesDeltaToMbps(sentBytesDelta, elapsedSeconds);

        return new NetworkSpeedSnapshot(downloadMbps, uploadMbps);
    }

    private static NetworkSample ReadCurrentSample()
    {
        long receivedBytes = 0;
        long sentBytes = 0;

        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (!ShouldInclude(networkInterface))
            {
                continue;
            }

            var statistics = networkInterface.GetIPv4Statistics();
            receivedBytes += Math.Max(0, statistics.BytesReceived);
            sentBytes += Math.Max(0, statistics.BytesSent);
        }

        return new NetworkSample(receivedBytes, sentBytes, DateTime.UtcNow);
    }

    private static bool ShouldInclude(NetworkInterface networkInterface)
    {
        return networkInterface.OperationalStatus == OperationalStatus.Up
            && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback
            && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Tunnel
            && networkInterface.Supports(NetworkInterfaceComponent.IPv4);
    }

    private static double BytesDeltaToMbps(long bytesDelta, double elapsedSeconds)
    {
        if (bytesDelta <= 0)
        {
            return 0;
        }

        return Math.Max(0, (bytesDelta * 8d) / elapsedSeconds / BitsPerMegabit);
    }
}
