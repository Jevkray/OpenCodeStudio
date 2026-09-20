using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenCodeStudio.Models;

namespace OpenCodeStudio.Services
{
    public class OpenCodeServerService : IOpenCodeServerService
    {
        private const int ConnectTimeoutMs = 30000;
        private const int HealthCheckIntervalMs = 500;

        private System.Diagnostics.Process _process;
        private HttpClient _httpClient;
        private ServerInfo _serverInfo;
        private ConnectionState _state = ConnectionState.Disconnected;
        private bool _ownsProcess;

        public ServerInfo ServerInfo => _serverInfo;
        public ConnectionState State => _state;
        public event Action<ConnectionState> StateChanged;

        public HttpClient GetClient()
        {
            return _httpClient;
        }

        public async Task<bool> StartAsync(string projectRoot)
        {
            Stop();
            SetState(ConnectionState.Connecting);

            // Reuse an OpenCode server that is already running - for example one
            // started by another Visual Studio window, or the OpenCode desktop
            // app. Sharing a single server keeps every client on the same live
            // session/event stream, so chats stay in sync everywhere.
            if (await TryAdoptSharedServerAsync())
            {
                Log.Info($"Reusing existing OpenCode server at {_serverInfo.BaseUrl}");
                SetState(ConnectionState.Connected);
                return true;
            }

            Log.Info($"Starting OpenCode server (cwd={projectRoot})");

            try
            {
                var opencodePath = ResolveOpenCodePath();
                if (opencodePath == null)
                {
                    Log.Error("Could not locate the opencode executable on PATH or common locations");
                    SetState(ConnectionState.Error);
                    return false;
                }
                Log.Info($"Using opencode executable: {opencodePath}");

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    WorkingDirectory = projectRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // A stray OPENCODE_SERVER_PASSWORD in the inherited environment
                // would switch the server to HTTP auth mode, making the embedded
                // WebView2 (and our HttpClient) get 401 responses. The embedded
                // client always talks to a loopback-only server, so we drop it.
                try { psi.EnvironmentVariables.Remove("OPENCODE_SERVER_PASSWORD"); } catch { }

                // NOTE: we deliberately do NOT override XDG_* here. Using the
                // user's default OpenCode environment keeps history, settings and
                // credentials in sync with the OpenCode CLI and Desktop app.

                // .cmd/.bat shims cannot be started directly with UseShellExecute=false,
                // so launch them through cmd.exe.
                if (opencodePath.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
                    opencodePath.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
                {
                    psi.FileName = "cmd.exe";
                    psi.Arguments = "/c \"" + opencodePath + "\" serve";
                }
                else
                {
                    psi.FileName = opencodePath;
                    psi.Arguments = "serve";
                }

                _process = System.Diagnostics.Process.Start(psi);
                if (_process == null)
                {
                    SetState(ConnectionState.Error);
                    return false;
                }

                _ownsProcess = true;
                ProcessBinding.BindToCurrentProcess(_process);

                var resolvedInfo = await ResolveServerUrlAsync(_process, ConnectTimeoutMs);
                if (resolvedInfo == null)
                {
                    Log.Error("Failed to detect server URL from process output");
                    SetState(ConnectionState.Error);
                    return false;
                }

                _serverInfo = resolvedInfo;
                Log.Info($"Server listening on {resolvedInfo.BaseUrl}");
                WriteSharedRegistry(resolvedInfo);

                var healthy = await WaitForHealthAsync(ConnectTimeoutMs);
                if (healthy)
                {
                    await InitializeHttpClientAsync();
                    SetState(ConnectionState.Connected);
                    Log.Info("Server healthy and connected");
                    return true;
                }

                Log.Error("Server started but health check never passed");
                SetState(ConnectionState.Error);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to start server", ex);
                SetState(ConnectionState.Error);
                return false;
            }
        }

        public async Task<bool> CheckHealthAsync()
        {
            if (_httpClient == null) return false;
            try
            {
                var response = await _httpClient.GetAsync("/global/health");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var health = JsonConvert.DeserializeObject<HealthInfo>(json);
                    return health?.Healthy == true;
                }
                return false;
            }
            catch { return false; }
        }

        public void Stop()
        {
            _httpClient?.Dispose();
            _httpClient = null;
            _serverInfo = null;

            // Only kill the server if this instance started it. When we merely
            // adopted a server started elsewhere, leave it running.
            if (_ownsProcess && _process != null)
            {
                Log.Info("Stopping OpenCode server");
                if (!_process.HasExited)
                {
                    try { _process.Kill(); _process.WaitForExit(5000); } catch { }
                }
                _process.Dispose();
                RemoveSharedRegistry();
            }
            _process = null;
            _ownsProcess = false;
            SetState(ConnectionState.Disconnected);
        }

        /// <summary>
        /// Tries to reuse a server recorded by another OpenCode Studio instance
        /// (other Visual Studio windows). Returns true when a healthy server was
        /// found and adopted.
        /// </summary>
        private async Task<bool> TryAdoptSharedServerAsync()
        {
            var info = ReadSharedRegistry();
            if (info == null) return false;

            try
            {
                using (var client = CreateHttpClient(info))
                {
                    client.Timeout = TimeSpan.FromSeconds(3);
                    var response = await client.GetAsync("/global/health");
                    if (!response.IsSuccessStatusCode) return false;
                    var json = await response.Content.ReadAsStringAsync();
                    var health = JsonConvert.DeserializeObject<HealthInfo>(json);
                    if (health?.Healthy != true) return false;
                }
            }
            catch { return false; }

            _serverInfo = info;
            _process = null;
            _ownsProcess = false;
            await InitializeHttpClientAsync();
            return true;
        }

        private static string RegistryPath => Path.Combine(
            OpenCodeEnvironment.StudioDir, "server.json");

        private static void WriteSharedRegistry(ServerInfo info)
        {
            try
            {
                Directory.CreateDirectory(OpenCodeEnvironment.StudioDir);
                var record = new SharedServerRecord
                {
                    Host = info.Host,
                    Port = info.Port,
                    Pid = System.Diagnostics.Process.GetCurrentProcess().Id
                };
                File.WriteAllText(RegistryPath, JsonConvert.SerializeObject(record));
            }
            catch { }
        }

        private static ServerInfo ReadSharedRegistry()
        {
            try
            {
                if (!File.Exists(RegistryPath)) return null;
                var record = JsonConvert.DeserializeObject<SharedServerRecord>(
                    File.ReadAllText(RegistryPath));
                if (record == null || record.Port <= 0) return null;
                return new ServerInfo(string.IsNullOrEmpty(record.Host) ? "127.0.0.1" : record.Host, record.Port);
            }
            catch { return null; }
        }

        private static void RemoveSharedRegistry()
        {
            try { if (File.Exists(RegistryPath)) File.Delete(RegistryPath); } catch { }
        }

        public void UpdateConnectionState(bool connected)
        {
            if (connected)
                SetState(ConnectionState.Connected);
            else
                SetState(ConnectionState.Disconnected);
        }

        private static async Task<ServerInfo> ResolveServerUrlAsync(
            System.Diagnostics.Process process, int timeoutMs)
        {
            var tcs = new TaskCompletionSource<ServerInfo>();
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

            async Task ReadStreamAsync(System.IO.StreamReader reader, string label)
            {
                try
                {
                    // Keep the same pending read across timeout ticks. Calling
                    // ReadLineAsync() again while a previous read is still in
                    // flight throws InvalidOperationException, which would abort
                    // detection entirely.
                    Task<string> lineTask = null;
                    while (DateTime.UtcNow < deadline)
                    {
                        if (lineTask == null)
                            lineTask = reader.ReadLineAsync();
                        var delayTask = Task.Delay(1000);
                        var completed = await Task.WhenAny(lineTask, delayTask);
                        if (completed == delayTask) continue;
                        var line = await lineTask;
                        lineTask = null;
                        if (line == null) break;
                        System.Diagnostics.Debug.WriteLine($"OpenCode {label}: {line}");
                        if (TryParseListenLine(line, out string host, out int port))
                        {
                            tcs.TrySetResult(new ServerInfo(host, port));
                            return;
                        }
                    }
                }
                catch { }
            }

            var stdoutTask = ReadStreamAsync(process.StandardOutput, "stdout");
            var stderrTask = ReadStreamAsync(process.StandardError, "stderr");
            var timeoutTask = Task.Delay(timeoutMs);

            await Task.WhenAny(tcs.Task, timeoutTask);
            return tcs.Task.IsCompleted ? await tcs.Task : null;
        }

        private static bool TryParseListenLine(string line, out string host, out int port)
        {
            host = null;
            port = 0;
            if (string.IsNullOrEmpty(line)) return false;
            const string prefix = "opencode server listening on http://";
            var idx = line.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return false;
            var url = line.Substring(idx + prefix.Length).Trim();
            var colonIdx = url.LastIndexOf(':');
            if (colonIdx < 0) return false;
            host = url.Substring(0, colonIdx);
            return int.TryParse(url.Substring(colonIdx + 1), out port);
        }

        private async Task<bool> WaitForHealthAsync(int timeoutMs)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                if (await TryConnectAsync(_serverInfo))
                    return true;
                await Task.Delay(HealthCheckIntervalMs);
            }
            return false;
        }

        private async Task<bool> TryConnectAsync(ServerInfo info)
        {
            try
            {
                using (var client = CreateHttpClient(info))
                {
                    client.Timeout = TimeSpan.FromSeconds(3);
                    var response = await client.GetAsync("/global/health");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var health = JsonConvert.DeserializeObject<HealthInfo>(json);
                        return health?.Healthy == true;
                    }
                }
            }
            catch { }
            return false;
        }

        private async Task InitializeHttpClientAsync()
        {
            _httpClient?.Dispose();
            _httpClient = CreateHttpClient(_serverInfo);
            await Task.CompletedTask;
        }

        private static HttpClient CreateHttpClient(ServerInfo info)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            return new HttpClient(handler)
            {
                BaseAddress = new Uri(info.BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        private void SetState(ConnectionState newState)
        {
            if (_state != newState)
            {
                _state = newState;
                StateChanged?.Invoke(newState);
            }
        }

        private static string ResolveOpenCodePath()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c where opencode 2>nul",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var proc = System.Diagnostics.Process.Start(psi))
                {
                    var output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(3000);
                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    string cmdFallback = null;
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            return trimmed;
                        if (cmdFallback == null && trimmed.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase))
                            cmdFallback = trimmed;
                    }
                    if (cmdFallback != null)
                        return cmdFallback;
                }
            }
            catch { }

            var commonPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm", "opencode.cmd"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "nvmw", "nodejs", "opencode.cmd"),
                Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles") ?? "", "nodejs", "opencode.cmd"),
            };
            foreach (var path in commonPaths)
                if (File.Exists(path)) return path;

            return null;
        }
    }

    /// <summary>Record of a running OpenCode server shared between clients.</summary>
    internal sealed class SharedServerRecord
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public int Pid { get; set; }
    }
}
