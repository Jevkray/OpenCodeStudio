using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenCodeStudio.Models;

namespace OpenCodeStudio.Services
{
    /// <summary>
    /// Manages OpenCode server lifecycle independently of any tool window.
    /// Supports ownership transfer between windows with mutual exclusion.
    /// </summary>
    public class ServerController : IDisposable
    {
        private readonly IOpenCodeServerService _serverService;
        private IConnectionMonitor _connectionMonitor;
        private IOpenCodeSessionService _sessionService;

        private string _currentProjectRoot;
        private string _currentSessionId;
        private string _currentSessionDirectory;

        private CancellationTokenSource _shutdownCts;
        private CancellationTokenSource _workspaceCheckCts;
        private const int ShutdownIdleMinutes = 5;
        private const int AgentCheckIntervalMs = 10000;
        private const int WorkspaceCheckIntervalMs = 5000;
        private bool _isDisposed;

        public IOpenCodeServerService ServerService => _serverService;
        public IOpenCodeSessionService SessionService => _sessionService;
        public ConnectionState State => _serverService.State;
        public string CurrentSessionId => _currentSessionId;
        public string CurrentProjectRoot => _currentProjectRoot;

        public void UpdateProjectRoot(string newRoot)
        {
            if (!string.Equals(newRoot, _currentProjectRoot, StringComparison.OrdinalIgnoreCase))
                _currentProjectRoot = newRoot;
        }

        public event Action<ConnectionState> ServerStateChanged;
        public event Action ConnectionLost;
        public event Action ConnectionRestored;
        public event Action WorkspaceMismatch;

        public ServerController()
        {
            _serverService = new OpenCodeServerService();
            _serverService.StateChanged += s => ServerStateChanged?.Invoke(s);
        }

        /// <summary>
        /// Start server for the given project root, creating or finding a session.
        /// </summary>
        public async Task<bool> StartAsync(string projectRoot)
        {
            CancelShutdown();
            _currentProjectRoot = projectRoot;

            _sessionService = new OpenCodeSessionService(_serverService);

            var running = await _serverService.CheckHealthAsync();

            if (!running)
            {
                var started = await _serverService.StartAsync(projectRoot);
                if (!started || _serverService.State != ConnectionState.Connected)
                    return false;
            }

            // Find an existing session for this project. We deliberately do NOT
            // create one here: the OpenCode web UI does that on demand, and
            // auto-creating an empty session on every project change only adds
            // clutter. If nothing is found we simply open the web UI home.
            Models.Session curSession = null;
            try
            {
                var sessions = await _sessionService.ListSessionsAsync(projectRoot);
                foreach (var s in sessions)
                {
                    if (string.Equals(
                        ProjectRootResolver.NormalizePath(s.Directory ?? ""),
                        projectRoot,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        curSession = s;
                        break;
                    }
                }
            }
            catch { }

            _currentSessionId = curSession?.Id;
            // Keep the directory exactly as OpenCode reports it (Windows path
            // with backslashes). The web client encodes this value into the URL,
            // so a normalized (forward-slash) path would not match the project.
            _currentSessionDirectory = curSession?.Directory;

            // Start connection monitoring
            if (_connectionMonitor == null)
            {
                _connectionMonitor = new ConnectionMonitor(_serverService, 5000);
                _connectionMonitor.ConnectionLost += () => ConnectionLost?.Invoke();
                _connectionMonitor.ConnectionRestored += () => ConnectionRestored?.Invoke();
            }
            _connectionMonitor.Start();

            StartWorkspaceCheck();
            // NOTE: the idle shutdown countdown was intentionally removed.
            // Stopping the server after a few idle minutes caused the tool window
            // to lose its connection periodically. The server process is bound to
            // Visual Studio through a job object, so it is terminated automatically
            // when VS exits.

            return true;
        }


        /// <summary>
        /// Get the navigation URL for the current project/session.
        /// Points straight at the OpenCode server's own web UI
        /// (http://127.0.0.1:port/{encoded-dir}/session/{id}). The directory
        /// segment uses the same UTF-8 base64url encoding as the OpenCode web
        /// client (its <c>cn()</c> helper), so the app resolves the right
        /// project without any proxying.
        /// </summary>
        public string GetSessionUrl()
        {
            if (_serverService.ServerInfo == null || string.IsNullOrEmpty(_currentSessionId))
                return null;

            var baseUrl = _serverService.ServerInfo.BaseUrl;
            var dir = !string.IsNullOrEmpty(_currentSessionDirectory)
                ? _currentSessionDirectory
                : (_currentProjectRoot ?? "");
            var encoded = ToUrlSafeBase64(dir);
            return $"{baseUrl}/{encoded}/session/{_currentSessionId}";
        }

        /// <summary>
        /// URL of the OpenCode web UI home. Opening the home (instead of a
        /// project-scoped session) shows all projects and every chat, exactly
        /// like the OpenCode desktop app.
        /// </summary>
        public string GetHomeUrl()
        {
            if (_serverService.ServerInfo == null)
                return null;
            return _serverService.ServerInfo.BaseUrl + "/";
        }

        public void Stop()
        {
            CancelWorkspaceCheck();
            CancelShutdown();
            _connectionMonitor?.Dispose();
            _connectionMonitor = null;
            _serverService.Stop();
        }

        private void StartWorkspaceCheck()
        {
            _workspaceCheckCts?.Cancel();
            _workspaceCheckCts = new CancellationTokenSource();
            var ct = _workspaceCheckCts.Token;

            _ = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(WorkspaceCheckIntervalMs, ct);
                    if (ct.IsCancellationRequested) return;

                    if (_serverService.State != ConnectionState.Connected) continue;

                    try
                    {
                        var pathInfo = await _sessionService.GetServerPathAsync();
                        if (pathInfo == null) continue;

                        var dir = ProjectRootResolver.NormalizePath(pathInfo.Directory ?? "");
                        if (string.IsNullOrEmpty(dir)) continue;

                        if (!string.Equals(dir, _currentProjectRoot, StringComparison.OrdinalIgnoreCase))
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"Server directory changed: {_currentProjectRoot} -> {dir}");
                            _currentProjectRoot = dir;
                            WorkspaceMismatch?.Invoke();
                        }
                    }
                    catch { }
                }
            });
        }

        private void CancelWorkspaceCheck()
        {
            _workspaceCheckCts?.Cancel();
            _workspaceCheckCts?.Dispose();
            _workspaceCheckCts = null;
        }

        private void StartShutdownCountdown()
        {
            _shutdownCts?.Cancel();
            _shutdownCts = new CancellationTokenSource();
            var ct = _shutdownCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        var busy = await IsAgentBusyAsync(ct);
                        if (busy)
                        {
                            await Task.Delay(AgentCheckIntervalMs, ct);
                            continue;
                        }

                        await Task.Delay(ShutdownIdleMinutes * 60 * 1000, ct);

                        if (!ct.IsCancellationRequested)
                        {
                            var stillBusy = await IsAgentBusyAsync(ct);
                            if (stillBusy) continue;

                            System.Diagnostics.Debug.WriteLine("ServerController: idle timeout, stopping server");
                            Stop();
                            break;
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ServerController shutdown error: {ex}");
                }
            });
        }

        private void CancelShutdown()
        {
            if (_shutdownCts != null)
            {
                _shutdownCts.Cancel();
                _shutdownCts.Dispose();
                _shutdownCts = null;
            }
        }

        private async Task<bool> IsAgentBusyAsync(CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_currentSessionId)
                || _serverService.State != ConnectionState.Connected)
                return false;

            try
            {
                var client = _serverService.GetClient();
                if (client == null) return false;

                var response = await client.GetAsync(
                    $"/session/{_currentSessionId}/status", ct);
                if (!response.IsSuccessStatusCode) return false;

                var json = await response.Content.ReadAsStringAsync();
                return json.Contains("\"status\":\"busy\"")
                    || json.Contains("\"status\":\"working\"");
            }
            catch
            {
                return false;
            }
        }

        private static string ToUrlSafeBase64(string text)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            Stop();
        }
    }
}
