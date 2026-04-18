using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;

namespace Crm.Entity.Services
{
    public class InvitationRepository: IInvitationRepository
    {
        public InvitationRepository() { }

        public async Task<bool> Add(Invitation invitation )
        {
            try
            {
                using (var db = new CrmContext())
                {
                    db.Invitations.Add(invitation);
                    await db.SaveChangesAsync();
                }
            }
            catch { return false; }
           
            return true;
        }

        public async Task<bool> Update(Invitation invitation)
        {
            try
            {
                using (var db = new CrmContext())
                {
                    // Проверяем, что запись существует
                    var existingInvitation = await db.Invitations
                        .FirstOrDefaultAsync(i => i.Id == invitation.Id);

                    if (existingInvitation == null)
                    {
                        return false; // Запись не найдена
                    }

                    // Обновляем только нужные поля
                    existingInvitation.Status = invitation.Status;
                    existingInvitation.AcceptedAt = invitation.AcceptedAt;
                    existingInvitation.ExpiresAt = invitation.ExpiresAt;
                    existingInvitation.Email = invitation.Email;
                    existingInvitation.Phone = invitation.Phone;
                    existingInvitation.Message = invitation.Message;

                    await db.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {                
                return false;
            }
        }
        public async Task<bool> AddRange(List<Invitation> invitations)
        {
            try
            {
                using (var db = new CrmContext())
                {
                    db.Invitations.AddRange(invitations);
                    await db.SaveChangesAsync();
                }
            }
            catch { return false; }

            return true;
        }

        public async Task<Invitation> GetInvitationToCode(string code)
        {
            try
            {
                using (var db = new CrmContext())
                {
                   var result = await db.Invitations
                    .FirstOrDefaultAsync(i => i.Code == code && i.Status == InvitationStatus.Pending);
                    return result;
                }
            }
            catch { return null; }
            
        }
    }
}
