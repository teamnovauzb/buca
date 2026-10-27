using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
#if NEWTONSOFT_JSON && LUXODD_INTEGRATION
using Luxodd.Game.Scripts.Network;
using Luxodd.Game.Scripts.Network.CommandHandler;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using Newtonsoft.Json.Linq;
#endif

/// <summary>Exercises the real SDK request, response dispatch and game mapping without a live session.</summary>
public static class LeaderboardIntegrationValidation
{
    [MenuItem("BUCA/Validation/Leaderboard Integration")]
    public static void RunFromMenu() => Debug.Log(Run());

    public static string Run()
    {
#if NEWTONSOFT_JSON && LUXODD_INTEGRATION
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Run in edit mode.");
        var preview = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Leaderboard validation (no network)");
        root.SetActive(false);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
        var loggerField = typeof(LoggerHelper).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
        var previousLogger = loggerField.GetValue(null);
        try
        {
            // SDK response handlers require their normal logger singleton, even with logging disabled.
            loggerField.SetValue(null, root.AddComponent<LoggerHelper>());
            var socket = root.AddComponent<WebSocketService>();
            var commands = root.AddComponent<WebSocketCommandHandler>();
            var bridge = root.AddComponent<LuxoddGameBridge>();
            Set(commands, "_webSocketService", socket);
            Call(commands, "PrepareCommands");
            Set(bridge, "_commandHandler", commands);
            Set(bridge, "_connected", true);

            var rows = new JArray(Enumerable.Range(1, 12).Select(rank => new JObject
            {
                ["rank"] = rank, ["game_handle"] = "PLAYER " + rank, ["score_total"] = 2000 - rank
            }));
            var ranked = Fetch(bridge, socket, 200, new JObject
            {
                ["current_user"] = new JObject { ["rank"] = 21, ["game_handle"] = "TEST PLAYER", ["score_total"] = 932 },
                ["leaderboard"] = rows
            });
            Check(ranked.connected && ranked.entries.Length == 10, "Top 10 mapping");
            Check(ranked.name == "TEST PLAYER" && ranked.rank == 21 && ranked.score == 932, "Current player outside Top 10");
            Check(ranked.entries[0].score == 1999 && ranked.entries[9].rank == 10, "Rank and score field mapping");

            var unranked = Fetch(bridge, socket, 200, new JObject { ["current_user"] = null, ["leaderboard"] = rows });
            Check(unranked.connected && unranked.name == "YOU" && unranked.rank == 0, "Missing current user");
            var empty = Fetch(bridge, socket, 200, new JObject { ["leaderboard"] = new JArray() });
            Check(empty.connected && empty.entries.Length == 0, "Empty leaderboard");
            var error = Fetch(bridge, socket, 503, new JObject { ["message"] = "Temporarily unavailable" });
            Check(!error.connected && error.entries == null, "Server error fallback");
            var nullPayload = Fetch(bridge, socket, 200, null);
            Check(!nullPayload.connected, "Null payload fallback");
            var malformed = Fetch(bridge, socket, 200, new JObject { ["leaderboard"] = "invalid" });
            Check(!malformed.connected, "Malformed payload fallback");
            var recovery = Fetch(bridge, socket, 200, new JObject { ["leaderboard"] = rows });
            Check(recovery.connected && recovery.entries.Length == 10, "Recovery after failed request");

            Set(bridge, "_connected", false);
            int offlineCalls = 0;
            bridge.FetchResultsLeaderboard((entries, name, rank, score, connected) =>
            {
                offlineCalls++;
                Check(!connected && entries == null && name == "YOU", "Offline fallback");
            });
            Check(offlineCalls == 1 && Queue(socket).Count == 0, "Offline callback exactly once without queued request");
            return "PASS: SDK request serialization and response dispatch, Top 10, current rank/score, unranked player, empty results, server error, null/malformed payload, recovery and offline fallback. Synthetic transport; no live backend contacted.";
        }
        finally
        {
            loggerField.SetValue(null, previousLogger);
            UnityEngine.Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(preview);
        }
#else
        throw new InvalidOperationException("Select the WebGL target with NEWTONSOFT_JSON and LUXODD_INTEGRATION enabled.");
#endif
    }

#if NEWTONSOFT_JSON && LUXODD_INTEGRATION
    sealed class Result
    {
        public LeaderboardPanel.LeaderboardData[] entries;
        public string name;
        public int rank, score;
        public bool connected;
    }

    static Result Fetch(LuxoddGameBridge bridge, WebSocketService socket, int status, JToken payload)
    {
        int callbacks = 0;
        Result result = null;
        bridge.FetchResultsLeaderboard((entries, name, rank, score, connected) =>
        {
            callbacks++;
            result = new Result { entries = entries, name = name, rank = rank, score = score, connected = connected };
        });
        Check(Queue(socket).Count == 1, "Exactly one outbound request");
        var request = Queue(socket).Dequeue();
        var json = JObject.Parse(request.CommandRequestJson);
        Check(request.CommandRequestType == CommandRequestType.LeaderboardRequest && (string)json["type"] == "leaderboard_request", "Request protocol");
        // Register the queued callback, then deliver JSON through the same dispatcher used by WebSocket messages.
        // The transport remains disconnected, so no credentials, scores or fake players reach a server.
        Call(socket, "AddCommandRequestHandler", request.CommandRequestType, request.OnCommandResponseSuccessHandler);
        Call(socket, "HandleMessageReceived", new JObject
        {
            ["msgver"] = "1.0", ["type"] = "leaderboard_response", ["status"] = status, ["payload"] = payload
        }.ToString());
        Check(callbacks == 1, "Response callback exactly once");
        return result;
    }

    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    static Queue<WebSocketService.SendCommandData> Queue(WebSocketService socket) =>
        (Queue<WebSocketService.SendCommandData>)typeof(WebSocketService).GetField("_sendCommandDataQueue", PrivateInstance).GetValue(socket);
    static void Set(object instance, string name, object value) => instance.GetType().GetField(name, PrivateInstance).SetValue(instance, value);
    static void Call(object instance, string name, params object[] values)
    {
        try { instance.GetType().GetMethod(name, PrivateInstance).Invoke(instance, values); }
        catch (TargetInvocationException e) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw(); }
    }
    static void Check(bool condition, string reason)
    {
        if (!condition) throw new Exception("Leaderboard validation failed: " + reason);
    }
#endif
}
