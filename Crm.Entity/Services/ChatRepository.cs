using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;

namespace Crm.Entity.Services;

public class ChatRepository : IChatRepository
{
    public async Task<List<ChatMessage>> GetConversationAsync(int userId, int otherUserId, int take = 100)
    {
        await using var db = new CrmContext();
        var companyId = await GetSharedCompanyIdAsync(db, userId, otherUserId);

        if (!companyId.HasValue)
            return new List<ChatMessage>();

        var messages = await db.ChatMessages
            .AsNoTracking()
            .Where(message => message.CompanyId == companyId.Value &&
                ((message.SenderUserId == userId && message.RecipientUserId == otherUserId) ||
                 (message.SenderUserId == otherUserId && message.RecipientUserId == userId)))
            .OrderByDescending(message => message.SentAt)
            .Take(Math.Clamp(take, 1, 200))
            .OrderBy(message => message.SentAt)
            .ToListAsync();

        var unread = await db.ChatMessages
            .Where(message => message.CompanyId == companyId.Value &&
                message.SenderUserId == otherUserId &&
                message.RecipientUserId == userId &&
                !message.IsRead)
            .ToListAsync();

        if (unread.Count > 0)
        {
            unread.ForEach(message => message.IsRead = true);
            await db.SaveChangesAsync();
        }

        return messages;
    }

    public async Task<ChatMessage?> SendMessageAsync(int senderUserId, int recipientUserId, string text)
    {
        await using var db = new CrmContext();
        var companyId = await GetSharedCompanyIdAsync(db, senderUserId, recipientUserId);

        if (!companyId.HasValue)
            return null;

        var message = new ChatMessage
        {
            CompanyId = companyId.Value,
            SenderUserId = senderUserId,
            RecipientUserId = recipientUserId,
            Text = text.Trim(),
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        db.ChatMessages.Add(message);
        await db.SaveChangesAsync();
        return message;
    }

    private static async Task<int?> GetSharedCompanyIdAsync(CrmContext db, int firstUserId, int secondUserId)
    {
        var companyIds = await db.CompanyUsers
            .Where(companyUser => companyUser.UserId == firstUserId && companyUser.IsActive)
            .Select(companyUser => companyUser.CompanyId)
            .ToListAsync();

        return await db.CompanyUsers
            .Where(companyUser => companyIds.Contains(companyUser.CompanyId) &&
                companyUser.UserId == secondUserId &&
                companyUser.IsActive)
            .Select(companyUser => (int?)companyUser.CompanyId)
            .FirstOrDefaultAsync();
    }
}
