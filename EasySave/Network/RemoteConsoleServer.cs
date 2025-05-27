using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EasySave.Model;

namespace EasySave.Network
{
    public class RemoteConsoleServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly List<TcpClient> _clients = new();
        private readonly BackupManager _backupManager;
        private CancellationTokenSource _cts;
        private readonly int _port;
        private HttpListener _wsListener; // For WebSocket
        private Task _wsAcceptTask;
        private readonly int _wsPort = 9001; // WebSocket port

        public event Action<string, string> RemoteCommandReceived; // (command, jobName)

        public RemoteConsoleServer(BackupManager backupManager, int port = 9000)
        {
            _backupManager = backupManager;
            _port = port;
            _listener = new TcpListener(IPAddress.Any, _port);
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _listener.Start();
            Task.Run(() => AcceptClientsAsync(_cts.Token));

            // Start WebSocket server
            StartWebSocketServer();
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener.Stop();
            lock (_clients)
            {
                foreach (var client in _clients)
                    client.Close();
                _clients.Clear();
            }
            // Stop WebSocket server
            if (_wsListener != null && _wsListener.IsListening)
            {
                _wsListener.Stop();
                _wsListener.Close();
            }
        }

        private async Task AcceptClientsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(token);
                    lock (_clients) { _clients.Add(client); }
                    _ = Task.Run(() => HandleClientAsync(client, token));
                    // Send initial job status
                    await SendJobStatusAsync(client);
                }
                catch { break; }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            var stream = client.GetStream();
            var buffer = new byte[1024];
            while (!token.IsCancellationRequested && client.Connected)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead == 0) break;
                    var msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    // Expecting JSON: { "command": "pause/resume/stop", "jobName": "Job1" }
                    var cmd = JsonSerializer.Deserialize<RemoteCommand>(msg);
                    RemoteCommandReceived?.Invoke(cmd.Command, cmd.JobName);
                }
                catch { break; }
            }
            lock (_clients) { _clients.Remove(client); }
            client.Close();
        }

        private void StartWebSocketServer()
        {
            _wsListener = new HttpListener();
            string prefix = $"http://+:{_wsPort}/ws/";
            _wsListener.Prefixes.Add(prefix);

            try
            {
                _wsListener.Start();

                // Retrieve and display IP and port for WebSocket
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        Console.WriteLine($"[WebSocket] Listening on ws://{ip}:{_wsPort}/ws/");
                    }
                }
                Console.WriteLine("[WebSocket] Waiting for WebSocket connections...");

                _wsAcceptTask = Task.Run(() => AcceptWebSocketClientsAsync(_cts.Token));
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine("[WebSocket] ERROR: Unable to start WebSocket server (HttpListener).");
                Console.WriteLine($"[WebSocket] Reason: {ex.Message}");
                Console.WriteLine("[WebSocket] You may need to run this application as administrator, or reserve the URL with:");
                Console.WriteLine($@"    netsh http add urlacl url={prefix} user=Everyone");
                Console.WriteLine("[WebSocket] WebSocket server will not be available.");
                // Optionally, you can rethrow or just return
                return;
            }
        }

        private async Task AcceptWebSocketClientsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var context = await _wsListener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        var wsContext = await context.AcceptWebSocketAsync(null);
                        _ = Task.Run(() => HandleWebSocketClientAsync(wsContext.WebSocket, token));
                        // Optionally, send initial job status here
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
                catch (Exception ex)
                {
                    if (!_wsListener.IsListening) break;
                    Console.WriteLine($"[WebSocket] Exception: {ex.Message}");
                }
            }
        }

        private async Task HandleWebSocketClientAsync(WebSocket ws, CancellationToken token)
        {
            var buffer = new byte[1024];
            while (ws.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                try
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", token);
                        break;
                    }
                    var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var cmd = JsonSerializer.Deserialize<RemoteCommand>(msg);
                    RemoteCommandReceived?.Invoke(cmd.Command, cmd.JobName);
                }
                catch
                {
                    break;
                }
            }
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", token);
        }

        public async Task BroadcastJobStatusAsync()
        {
            var jobs = _backupManager.GetAllJobs();
            var status = JsonSerializer.Serialize(jobs.Select(j => new
            {
                j.Name,
                j.Type,
                j.Progress,
                j.LastRunTime,
                State = j.GetType().GetProperty("State")?.GetValue(j)
            }));
            var data = Encoding.UTF8.GetBytes(status);
            List<TcpClient> clientsCopy;
            lock (_clients) { clientsCopy = _clients.ToList(); }
            foreach (var client in clientsCopy)
            {
                try
                {
                    if (client.Connected)
                        await client.GetStream().WriteAsync(data, 0, data.Length);
                }
                catch { }
            }
        }

        private async Task SendJobStatusAsync(TcpClient client)
        {
            var jobs = _backupManager.GetAllJobs();
            var status = JsonSerializer.Serialize(jobs.Select(j => new
            {
                j.Name,
                j.Type,
                j.Progress,
                j.LastRunTime,
                State = j.GetType().GetProperty("State")?.GetValue(j)
            }));
            var data = Encoding.UTF8.GetBytes(status);
            try
            {
                await client.GetStream().WriteAsync(data, 0, data.Length);
            }
            catch { }
        }

        public void Dispose()
        {
            Stop();
        }

        private class RemoteCommand
        {
            public string Command { get; set; }
            public string JobName { get; set; }
        }
    }
}
