using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Client
{
    public class RemoteConsoleClient : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        public event Action<List<JobStatus>> JobStatusReceived;

        public async Task ConnectAsync(string host, int port)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(host, port);
            _stream = _client.GetStream();
            _ = Task.Run(ReceiveLoop);
        }

        public async Task RunJobAsync(string jobName)
        {
            await SendCommandAsync("run", jobName);
        }

        public async Task RunAllJobsAsync()
        {
            var obj = new { Command = "runall", JobName = "" };
            var json = JsonSerializer.Serialize(obj);
            var data = Encoding.UTF8.GetBytes(json);
            await _stream.WriteAsync(data, 0, data.Length);
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[4096];
            while (_client.Connected)
            {
                try
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;
                    var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var jobs = JsonSerializer.Deserialize<List<JobStatus>>(json);
                    JobStatusReceived?.Invoke(jobs);
                }
                catch { break; }
            }
        }

        public async Task SendCommandAsync(string command, string jobName)
        {
            if (_stream == null) return;
            var obj = new { Command = command, JobName = jobName };
            var json = JsonSerializer.Serialize(obj);
            var data = Encoding.UTF8.GetBytes(json);
            await _stream.WriteAsync(data, 0, data.Length);
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _client?.Close();
        }

        public class JobStatus
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public float Progress { get; set; }
            public DateTime LastRunTime { get; set; }
            public string State { get; set; }
        }
    }
}
