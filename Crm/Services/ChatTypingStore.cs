using System.Collections.Concurrent;

namespace Crm.Services;

public sealed class ChatTypingStore
{
    private static readonly TimeSpan TypingLifetime = TimeSpan.FromSeconds(2.5);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _typingUsers = new();

    public void SetTyping(int senderUserId, int recipientUserId, bool isTyping)
    {
        var key = GetKey(senderUserId, recipientUserId);

        if (isTyping)
            _typingUsers[key] = DateTimeOffset.UtcNow;
        else
            _typingUsers.TryRemove(key, out _);
    }

    public bool IsTyping(int senderUserId, int recipientUserId)
    {
        var key = GetKey(senderUserId, recipientUserId);

        if (!_typingUsers.TryGetValue(key, out var updatedAt))
            return false;

        if (DateTimeOffset.UtcNow - updatedAt > TypingLifetime)
        {
            _typingUsers.TryRemove(key, out _);
            return false;
        }

        return true;
    }

    private static string GetKey(int senderUserId, int recipientUserId)
        => $"{senderUserId}:{recipientUserId}";
}
