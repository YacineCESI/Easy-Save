using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace EasySaveV2.Model
{
    public class NetworkMonitor : IDisposable
    {
        private readonly int _threshold;
        private readonly Timer _monitorTimer;
        private int _currentLoad;
        private long _lastBytesSent;
        private long _lastBytesReceived;
        private DateTime _lastCheck;

        public event EventHandler<int>? NetworkLoadChanged;

        public NetworkMonitor(int threshold)
        {
            _threshold = threshold;
            _lastCheck = DateTime.Now;
            _monitorTimer = new Timer(CheckNetworkLoad, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        private void CheckNetworkLoad(object? state)
        {
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                                ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                long totalBytesSent = 0;
                long totalBytesReceived = 0;

                foreach (var ni in interfaces)
                {
                    var stats = ni.GetIPStatistics();
                    totalBytesSent += stats.BytesSent;
                    totalBytesReceived += stats.BytesReceived;
                }

                var now = DateTime.Now;
                var timeDiff = (now - _lastCheck).TotalSeconds;
                
                if (timeDiff > 0)
                {
                    var bytesSentPerSecond = (totalBytesSent - _lastBytesSent) / timeDiff;
                    var bytesReceivedPerSecond = (totalBytesReceived - _lastBytesReceived) / timeDiff;
                    
                    // Calculate network load as a percentage of theoretical maximum bandwidth
                    // Assuming 100 MB/s as maximum bandwidth
                    const long maxBandwidth = 100 * 1024 * 1024; // 100 MB/s
                    int load = (int)(((bytesSentPerSecond + bytesReceivedPerSecond) * 100) / maxBandwidth);
                    
                    if (load != _currentLoad)
                    {
                        _currentLoad = load;
                        NetworkLoadChanged?.Invoke(this, load);
                    }
                }

                _lastBytesSent = totalBytesSent;
                _lastBytesReceived = totalBytesReceived;
                _lastCheck = now;
            }
            catch (Exception)
            {
                // Log error or handle it appropriately
            }
        }

        public void Dispose()
        {
            _monitorTimer.Dispose();
        }
    }
} 