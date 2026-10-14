using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Crm.Services.Notifications;

namespace Crm.Services.Notifications.Max;

/// <summary>Кнопка-ссылка под сообщением (inline_keyboard, type = link).</summary>
public sealed record MaxLinkButton(string Text, string Url);

/// <summary>Итог вызова Bot API. <see cref="Permanent"/> — повтор не поможет (400/403/404: бот заблокирован, нет пользователя).</summary>
public sealed record MaxCallResult(bool Ok, int StatusCode, string? ErrorCode = null, string? Error = null, TimeSpan? RetryAfter = null)
{
    public bool Permanent => !Ok && StatusCode is 400 or 401 or 403 or 404;
}

public sealed record MaxBotInfo(long UserId, string? Name, string? Username);

/// <summary>
/// Клиент MAX Bot API (https://dev.max.ru/docs-api), перенесён из модуля уведомлений Bakesoft:
/// токен — заголовок <c>Authorization: &lt;token&gt;</c> (без «Bearer»);
/// <c>GET /me</c>, <c>POST /messages?user_id=…</c> {text, format, attachments:[inline_keyboard]},
/// <c>GET/POST/DELETE /subscriptions</c> {url, update_types, secret}. Токен в лог не пишется.
/// </summary>
public sealed class MaxBotClient
{
    public const string HttpClientName = "max-bot-api";

    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // Кириллица — как есть (UTF-8), без \uXXXX: короче и читаемо в логах прокси.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly HttpClient _http;
    private readonly MaxOptions _options;
    private readonly ILogger<MaxBotClient> _log;

    public MaxBotClient(HttpClient http, IOptions<NotificationOptions> options, ILogger<MaxBotClient> log)
    {
        _http = http;
        _options = options.Value.Max;
        _log = log;
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<MaxBotInfo?> GetMeAsync(CancellationToken ct = default)
    {
        using var res = await SendAsync(HttpMethod.Get, "me", null, null, ct);
        if (res is null || !res.IsSuccessStatusCode) return null;
        var me = await res.Content.ReadFromJsonAsync<MaxUser>(Json, ct);
        return me is null ? null : new MaxBotInfo(me.UserId, me.DisplayName, me.Username);
    }

    /// <summary>Сообщение пользователю в личный диалог с ботом.</summary>
    public async Task<MaxCallResult> SendMessageAsync(long userId, string text, IReadOnlyList<MaxLinkButton>? buttons = null, CancellationToken ct = default)
    {
        var body = new SendMessageBody
        {
            Text = text.Length > MaxTextLimit ? text[..(MaxTextLimit - 1)] + "…" : text,
            Format = "markdown",
            Attachments = buttons is { Count: > 0 }
                ? new object[]
                {
                    new
                    {
                        type = "inline_keyboard",
                        payload = new { buttons = buttons.Select(b => new object[] { new { type = "link", text = b.Text, url = b.Url } }).ToArray() },
                    },
                }
                : null,
        };
        return await CallAsync(HttpMethod.Post, "messages", new() { ["user_id"] = userId.ToString(System.Globalization.CultureInfo.InvariantCulture) }, body, ct);
    }

    /// <summary>Подписать бота на вебхук (POST /subscriptions). MAX будет присылать секрет в заголовке X-Max-Bot-Api-Secret.</summary>
    public Task<MaxCallResult> SubscribeAsync(string url, IReadOnlyList<string> updateTypes, string? secret, CancellationToken ct = default) =>
        CallAsync(HttpMethod.Post, "subscriptions", null,
            new { url, update_types = updateTypes, secret = string.IsNullOrEmpty(secret) ? null : secret }, ct);

    public Task<MaxCallResult> UnsubscribeAsync(string url, CancellationToken ct = default) =>
        CallAsync(HttpMethod.Delete, "subscriptions", new() { ["url"] = url }, null, ct);

    /// <summary>Лимит длины текста сообщения в MAX (предположение: 4000 символов).</summary>
    public const int MaxTextLimit = 4000;

    private async Task<MaxCallResult> CallAsync(HttpMethod method, string path, Dictionary<string, string>? query, object? body, CancellationToken ct)
    {
        if (!_options.IsConfigured) return new MaxCallResult(false, 0, "not.configured", "Бот MAX не настроен");
        try
        {
            using var res = await SendAsync(method, path, query, body, ct);
            if (res is null) return new MaxCallResult(false, 0, "not.configured", "Бот MAX не настроен");
            if (res.IsSuccessStatusCode) return new MaxCallResult(true, (int)res.StatusCode);

            MaxError? err = null;
            try { err = await res.Content.ReadFromJsonAsync<MaxError>(Json, ct); } catch (JsonException) { } catch (NotSupportedException) { }
            TimeSpan? retryAfter = res.Headers.RetryAfter?.Delta;
            var status = (int)res.StatusCode;
            _log.LogWarning("MAX {Method} /{Path}: {Status} {Code}", method.Method, path, status, err?.Code);
            return new MaxCallResult(false, status, err?.Code, Trim(err?.Message ?? res.ReasonPhrase), retryAfter);
        }
        catch (HttpRequestException ex)
        {
            _log.LogWarning("MAX {Method} /{Path}: сеть: {Error}", method.Method, path, ex.Message);
            return new MaxCallResult(false, 0, "network", Trim(ex.Message));
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new MaxCallResult(false, 0, "timeout", "Таймаут MAX Bot API");
        }
    }

    private async Task<HttpResponseMessage?> SendAsync(HttpMethod method, string path, Dictionary<string, string>? query, object? body, CancellationToken ct)
    {
        if (!_options.IsConfigured) return null;
        var q = new Dictionary<string, string>(query ?? new());
        var url = _options.ApiBaseUrl.TrimEnd('/') + "/" + path;
        if (q.Count > 0) url += "?" + string.Join("&", q.Select(kv => Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value)));

        using var req = new HttpRequestMessage(method, url);
        req.Headers.TryAddWithoutValidation("Authorization", _options.BotToken);
        // Буферизованное тело с Content-Length (JsonContent шлёт chunked — не все прокси/API это любят).
        if (body is not null) req.Content = new StringContent(JsonSerializer.Serialize(body, body.GetType(), Json), System.Text.Encoding.UTF8, "application/json");
        return await _http.SendAsync(req, ct);
    }

    private static string? Trim(string? s) => s is null ? null : s.Length > 300 ? s[..300] : s;

    private sealed class SendMessageBody
    {
        [JsonPropertyName("text")] public string? Text { get; set; }
        [JsonPropertyName("format")] public string? Format { get; set; }
        [JsonPropertyName("attachments")] public object[]? Attachments { get; set; }
    }

    private sealed class MaxError
    {
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
    }
}

// ---------------- входящие обновления (вебхук) ----------------

public sealed class MaxUpdate
{
    [JsonPropertyName("update_type")] public string? UpdateType { get; set; }
    /// <summary>Unix-время, мс.</summary>
    [JsonPropertyName("timestamp")] public long Timestamp { get; set; }
    [JsonPropertyName("chat_id")] public long? ChatId { get; set; }
    /// <summary>bot_started / bot_stopped: кто нажал «Начать».</summary>
    [JsonPropertyName("user")] public MaxUser? User { get; set; }
    /// <summary>bot_started: параметр start из ссылки https://max.ru/&lt;bot&gt;?start=&lt;payload&gt;.</summary>
    [JsonPropertyName("payload")] public string? Payload { get; set; }
    [JsonPropertyName("message")] public MaxMessage? Message { get; set; }
}

public sealed class MaxUser
{
    [JsonPropertyName("user_id")] public long UserId { get; set; }
    [JsonPropertyName("first_name")] public string? FirstName { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("username")] public string? Username { get; set; }
    [JsonPropertyName("is_bot")] public bool IsBot { get; set; }

    public string? DisplayName => FirstName ?? Name;
}

public sealed class MaxMessage
{
    [JsonPropertyName("sender")] public MaxUser? Sender { get; set; }
    [JsonPropertyName("recipient")] public MaxRecipient? Recipient { get; set; }
    [JsonPropertyName("timestamp")] public long Timestamp { get; set; }
    [JsonPropertyName("body")] public MaxMessageBody? Body { get; set; }
}

public sealed class MaxRecipient
{
    [JsonPropertyName("chat_id")] public long? ChatId { get; set; }
    [JsonPropertyName("chat_type")] public string? ChatType { get; set; }
    [JsonPropertyName("user_id")] public long? UserId { get; set; }
}

public sealed class MaxMessageBody
{
    [JsonPropertyName("mid")] public string? Mid { get; set; }
    [JsonPropertyName("text")] public string? Text { get; set; }
}
