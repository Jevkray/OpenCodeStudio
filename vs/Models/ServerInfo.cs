namespace OpenCodeStudio.Models
{
    /// <summary>
    /// Server connection info.
    /// </summary>
    public class ServerInfo
    {
        public string Host { get; set; }

        public int Port { get; set; }

        /// <summary>
        /// Full base URL (e.g., http://localhost:4096).
        /// </summary>
        public string BaseUrl => $"http://{Host}:{Port}";

        public ServerInfo()
        {
            Host = "127.0.0.1";
            Port = 4096;
        }

        public ServerInfo(string host, int port)
        {
            Host = host;
            Port = port;
        }
    }
}
