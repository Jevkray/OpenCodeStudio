namespace OpenCodeStudio.Services
{
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    public interface IOpenCodeServerService
    {
        Models.ServerInfo ServerInfo { get; }
        ConnectionState State { get; }
        System.Net.Http.HttpClient GetClient();
        System.Threading.Tasks.Task<bool> StartAsync(string projectRoot);
        System.Threading.Tasks.Task<bool> CheckHealthAsync();
        void Stop();
        event System.Action<ConnectionState> StateChanged;

        void UpdateConnectionState(bool connected);
    }
}
