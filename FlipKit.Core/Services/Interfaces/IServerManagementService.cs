namespace FlipKit.Core.Services
{
    public interface IServerManagementService
    {
        /// <summary>
        /// Starts the Web server on the specified port.
        /// </summary>
        Task<ServerStartResult> StartWebServerAsync(int port);

        /// <summary>
        /// Starts the API server on the specified port.
        /// </summary>
        Task<ServerStartResult> StartApiServerAsync(int port);

        /// <summary>
        /// Stops the Web server gracefully.
        /// </summary>
        Task StopWebServerAsync();

        /// <summary>
        /// Stops the API server gracefully.
        /// </summary>
        Task StopApiServerAsync();

        /// <summary>
        /// Gets the current status of both servers.
        /// </summary>
        ServerStatus GetServerStatus();

        /// <summary>
        /// Gets recent log lines from the Web server.
        /// </summary>
        string[] GetWebServerLogs();

        /// <summary>
        /// Gets recent log lines from the API server.
        /// </summary>
        string[] GetApiServerLogs();

        /// <summary>
        /// Clears the Web server logs.
        /// </summary>
        void ClearWebServerLogs();

        /// <summary>
        /// Clears the API server logs.
        /// </summary>
        void ClearApiServerLogs();
    }

    public class ServerStartResult
    {
        public bool Success { get; set; }
        public int ActualPort { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class ServerStatus
    {
        public bool IsWebRunning { get; set; }
        public bool IsApiRunning { get; set; }
        public int WebPort { get; set; }
        public int ApiPort { get; set; }
        public DateTime? WebStartTime { get; set; }
        public DateTime? ApiStartTime { get; set; }
    }
}
