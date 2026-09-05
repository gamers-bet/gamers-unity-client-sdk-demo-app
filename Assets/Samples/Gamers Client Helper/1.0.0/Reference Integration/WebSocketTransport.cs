using System;
using System.Text;
using NativeWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Gamers.Client;

public class WebSocketTransport : MonoBehaviour, IGamersTransport
{
    // Replace this with your own game-server WebSocket URL.
    [SerializeField] private string _url = "wss://reference-server-dev-internal-testing.up.railway.app/ws/gamers";
    [SerializeField] private string _playerIdHeader;

    public event Action<GamersServerReply> ReplyReceived;

    private WebSocket _ws;

    private async void Start()
    {
        await ConnectAsync();
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        _ws?.DispatchMessageQueue();
#endif
    }

    public async System.Threading.Tasks.Task ConnectAsync()
    {
        if (_ws != null && (_ws.State == WebSocketState.Open || _ws.State == WebSocketState.Connecting))
            return;

        string connectionUrl = _url;
        if (!string.IsNullOrEmpty(_playerIdHeader))
        {
            string separator = connectionUrl.Contains("?") ? "&" : "?";
            connectionUrl += $"{separator}playerId={Uri.EscapeDataString(_playerIdHeader)}";
        }

        Debug.Log($"[WebSocketTransport] Attempting connection to: {connectionUrl}");
        _ws = new WebSocket(connectionUrl);

        _ws.OnOpen += () =>
        {
            Debug.Log("[WebSocketTransport] Connected successfully!");
        };

        _ws.OnError += (e) =>
        {
            Debug.LogError($"[WebSocketTransport] Connection Error: {e}");
        };

        _ws.OnClose += (e) =>
        {
            Debug.LogWarning($"[WebSocketTransport] Connection closed with code: {e}");
        };

        _ws.OnMessage += (bytes) =>
        {
            var json = Encoding.UTF8.GetString(bytes);
            var reply = DeserializeReply(json);
            ReplyReceived?.Invoke(reply);
        };

        try
        {
            await _ws.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WebSocketTransport] Connect exception: {ex.GetType().Name} - {ex.Message}");
        }
    }

    public async System.Threading.Tasks.Task SendAsync(GamersClientMessage message, System.Threading.CancellationToken ct = default)
    {
        if (_ws == null || _ws.State != WebSocketState.Open)
        {
            Debug.LogError("[WebSocketTransport] Cannot send: Not connected");
            return;
        }

        var json = JsonConvert.SerializeObject(message);
        await _ws.SendText(json);
    }

    private GamersServerReply DeserializeReply(string json)
    {
        try
        {
            var jo = JObject.Parse(json);
            var type = jo["type"]?.ToString();
            return type switch
            {
                "auth:code-requested" => JsonConvert.DeserializeObject<AuthCodeRequestedReply>(json),
                "auth:result" => JsonConvert.DeserializeObject<AuthResultReply>(json),
                "event:join-result" => JsonConvert.DeserializeObject<EventJoinResultReply>(json),
                "tournament:join-result" => JsonConvert.DeserializeObject<TournamentJoinResultReply>(json),
                "tournament:leaderboard" => JsonConvert.DeserializeObject<LeaderboardSnapshotReply>(json),
                "error" => JsonConvert.DeserializeObject<ErrorReply>(json),
                _ => new ErrorReply { Code = "UNKNOWN_MESSAGE", Message = $"Unknown type: {type}", Retryable = false }
            };
        }
        catch (Exception ex)
        {
            return new ErrorReply { Code = "PARSE_ERROR", Message = ex.Message, Retryable = false };
        }
    }

    private async void OnApplicationQuit()
    {
        if (_ws != null)
        {
            await _ws.Close();
        }
    }
}