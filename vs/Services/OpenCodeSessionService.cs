using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenCodeStudio.Models;
using OpenCodeStudio.Resources;

namespace OpenCodeStudio.Services
{
    /// <summary>
    /// Manages OpenCode sessions via the HTTP API.
    /// </summary>
    public class OpenCodeSessionService : IOpenCodeSessionService
    {
        private readonly IOpenCodeServerService _serverService;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public OpenCodeSessionService(IOpenCodeServerService serverService)
        {
            _serverService = serverService;
        }

        public async Task<List<Session>> ListSessionsAsync(string directory)
        {
            var client = _serverService.GetClient();
            if (client == null)
                throw new InvalidOperationException(StringsHelper.ErrorServerNotConnected);

            var encoded = Uri.EscapeDataString(directory);
            var response = await client.GetAsync($"/session?directory={encoded}");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to list sessions: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Session>>(json);
        }

        public async Task<Session> CreateSessionAsync(string directory, string title = null)
        {
            var client = _serverService.GetClient();
            if (client == null)
                throw new InvalidOperationException(StringsHelper.ErrorServerNotConnected);

            var body = new CreateSessionRequest
            {
                Title = title ?? "VS OpenCode"
            };

            var json = JsonConvert.SerializeObject(body, JsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var encoded = Uri.EscapeDataString(directory);
            var response = await client.PostAsync($"/session?directory={encoded}", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Failed to create session (HTTP {response.StatusCode}): {errorBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Session>(responseJson);
        }

        public async Task<PathInfo> GetPathAsync(string directory)
        {
            var client = _serverService.GetClient();
            if (client == null)
                throw new InvalidOperationException(StringsHelper.ErrorServerNotConnected);

            var encoded = Uri.EscapeDataString(directory);
            var response = await client.GetAsync($"/path?directory={encoded}");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to get path: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<PathInfo>(json);
        }

        public async Task<PathInfo> GetServerPathAsync()
        {
            var client = _serverService.GetClient();
            if (client == null)
                throw new InvalidOperationException(StringsHelper.ErrorServerNotConnected);

            var response = await client.GetAsync("/path");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to get server path: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<PathInfo>(json);
        }

        public async Task<List<ProjectInfo>> ListProjectsAsync(string directory)
        {
            var client = _serverService.GetClient();
            if (client == null)
                throw new InvalidOperationException(StringsHelper.ErrorServerNotConnected);

            var encoded = Uri.EscapeDataString(directory);
            var response = await client.GetAsync($"/project?directory={encoded}");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to list projects: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<ProjectInfo>>(json);
        }

        public async Task<ProjectInfo> GetCurrentProjectAsync(string directory)
        {
            var client = _serverService.GetClient();
            if (client == null)
                throw new InvalidOperationException(StringsHelper.ErrorServerNotConnected);

            var encoded = Uri.EscapeDataString(directory);
            var response = await client.GetAsync($"/project/current?directory={encoded}");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to get current project: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ProjectInfo>(json);
        }
    }
}
