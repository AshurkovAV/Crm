
using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services
{
    public interface IInvitationRepository
    {
        Task<bool> Add(Invitation invitation);
        Task<bool> Update(Invitation invitation);
        Task<bool> AddRange(List<Invitation> invitations);
        Task<Invitation> GetInvitationToCode(string code);
    }
}
