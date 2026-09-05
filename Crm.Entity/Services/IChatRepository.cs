using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services;

public interface IChatRepository
{
    Task<List<ChatMessage>> GetConversationAsync(int userId, int otherUserId, int take = 100);

    Task<ChatMessage?> SendMessageAsync(int senderUserId, int recipientUserId, string text);
}
