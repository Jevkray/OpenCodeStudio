using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OpenCodeStudio.Services
{
    /// <summary>
    /// Collects usage statistics from the OpenCode server and aggregates them
    /// into a compact JSON document consumed by the statistics dashboard.
    /// </summary>
    public class StatsService
    {
        private readonly IOpenCodeServerService _server;

        public StatsService(IOpenCodeServerService server)
        {
            _server = server;
        }

        public async Task<JObject> BuildAsync(string currentSessionId)
        {
            var client = _server?.GetClient();
            if (client == null)
                return null;

            var sessionsJson = await client.GetStringAsync("/session");
            var sessions = JArray.Parse(sessionsJson);

            long totalInput = 0, totalOutput = 0, totalReasoning = 0, totalCacheRead = 0, totalCacheWrite = 0;
            double totalCost = 0;

            var byModel = new Dictionary<string, Agg>();
            var byAgent = new Dictionary<string, Agg>();
            var byDay = new Dictionary<string, Agg>();
            var sessionList = new JArray();

            foreach (var s in sessions)
            {
                var id = (string)s["id"];
                var title = (string)s["title"];
                var dir = (string)s["directory"];
                var agent = (string)s["agent"] ?? "?";
                var model = (string)s["model"]?["id"] ?? "?";
                var provider = (string)s["model"]?["providerID"] ?? "?";
                double cost = (double?)s["cost"] ?? 0;

                var tok = s["tokens"];
                long inp = (long?)tok?["input"] ?? 0;
                long outp = (long?)tok?["output"] ?? 0;
                long reas = (long?)tok?["reasoning"] ?? 0;
                long cacheRead = (long?)tok?["cache"]?["read"] ?? 0;
                long cacheWrite = (long?)tok?["cache"]?["write"] ?? 0;
                long sessionTokens = inp + outp + reas + cacheRead + cacheWrite;

                long created = (long?)s["time"]?["created"] ?? 0;
                long updated = (long?)s["time"]?["updated"] ?? created;

                totalInput += inp;
                totalOutput += outp;
                totalReasoning += reas;
                totalCacheRead += cacheRead;
                totalCacheWrite += cacheWrite;
                totalCost += cost;

                Accumulate(byModel, provider + "/" + model, cost, sessionTokens);
                Accumulate(byAgent, agent, cost, sessionTokens);

                if (created > 0)
                {
                    var day = DateTimeOffset.FromUnixTimeMilliseconds(created).ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    Accumulate(byDay, day, cost, sessionTokens);
                }

                sessionList.Add(new JObject
                {
                    ["id"] = id,
                    ["title"] = title,
                    ["directory"] = dir,
                    ["agent"] = agent,
                    ["model"] = model,
                    ["provider"] = provider,
                    ["cost"] = cost,
                    ["tokens"] = sessionTokens,
                    ["updated"] = updated
                });
            }

            // Current session detail: request count + per-request series
            var current = new JObject();
            if (!string.IsNullOrEmpty(currentSessionId))
            {
                try
                {
                    var msgJson = await client.GetStringAsync($"/session/{currentSessionId}/message");
                    var msgs = JArray.Parse(msgJson);

                    int requests = 0;
                    double sessionCost = 0;
                    long sessionTokens = 0;
                    var series = new JArray();

                    foreach (var m in msgs)
                    {
                        var info = m["info"];
                        if (info == null || (string)info["role"] != "assistant")
                            continue;

                        requests++;
                        double cost = (double?)info["cost"] ?? 0;
                        long tokens = (long?)info["tokens"]?["total"] ?? 0;
                        long t = (long?)info["time"]?["completed"] ?? (long?)info["time"]?["created"] ?? 0;

                        sessionCost += cost;
                        sessionTokens += tokens;

                        series.Add(new JObject
                        {
                            ["t"] = t,
                            ["cost"] = cost,
                            ["tokens"] = tokens,
                            ["model"] = (string)info["modelID"] ?? ""
                        });
                    }

                    var cur = sessions.FirstOrDefault(x => (string)x["id"] == currentSessionId);
                    current = new JObject
                    {
                        ["id"] = currentSessionId,
                        ["title"] = (string)cur?["title"] ?? "",
                        ["model"] = (string)cur?["model"]?["id"] ?? "",
                        ["agent"] = (string)cur?["agent"] ?? "",
                        ["cost"] = sessionCost,
                        ["tokens"] = sessionTokens,
                        ["requests"] = requests,
                        ["series"] = series
                    };
                }
                catch
                {
                    // ignore - current session stats are optional
                }
            }

            var result = new JObject
            {
                ["generatedAt"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ["totals"] = new JObject
                {
                    ["sessions"] = sessions.Count,
                    ["cost"] = totalCost,
                    ["tokens"] = new JObject
                    {
                        ["input"] = totalInput,
                        ["output"] = totalOutput,
                        ["reasoning"] = totalReasoning,
                        ["cacheRead"] = totalCacheRead,
                        ["cacheWrite"] = totalCacheWrite,
                        ["total"] = totalInput + totalOutput + totalReasoning + totalCacheRead + totalCacheWrite
                    }
                },
                ["byModel"] = ToArray(byModel),
                ["byAgent"] = ToArray(byAgent),
                ["byDay"] = ToDayArray(byDay),
                ["sessions"] = sessionList,
                ["current"] = current
            };

            return result;
        }

        private static void Accumulate(Dictionary<string, Agg> map, string key, double cost, long tokens)
        {
            if (!map.TryGetValue(key, out var agg))
            {
                agg = new Agg();
                map[key] = agg;
            }
            agg.Cost += cost;
            agg.Tokens += tokens;
            agg.Count++;
        }

        private static JArray ToArray(Dictionary<string, Agg> map)
        {
            var arr = new JArray();
            foreach (var kv in map.OrderByDescending(k => k.Value.Cost).ThenByDescending(k => k.Value.Tokens))
            {
                arr.Add(new JObject
                {
                    ["name"] = kv.Key,
                    ["cost"] = kv.Value.Cost,
                    ["tokens"] = kv.Value.Tokens,
                    ["count"] = kv.Value.Count
                });
            }
            return arr;
        }

        private static JArray ToDayArray(Dictionary<string, Agg> map)
        {
            var arr = new JArray();
            foreach (var kv in map.OrderBy(k => k.Key))
            {
                arr.Add(new JObject
                {
                    ["date"] = kv.Key,
                    ["cost"] = kv.Value.Cost,
                    ["tokens"] = kv.Value.Tokens,
                    ["count"] = kv.Value.Count
                });
            }
            return arr;
        }

        private sealed class Agg
        {
            public double Cost;
            public long Tokens;
            public int Count;
        }
    }
}
