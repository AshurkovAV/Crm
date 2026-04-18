using Crm.Models.Company;
using Crm.Models.Requests;
using Crm.Models.Responses;

namespace Crm.Services
{
    public interface IInvitationService
    {
        Task<InvitationLinkResponse> GenerateInvitationLinkAsync(int? departmentId);
        Task<InvitationResponse> SendEmailInvitationsAsync(EmailInvitationRequest request, int userId);
        //Task<InvitationResponse> SendSmsInvitationsAsync(SmsInvitationRequest request);
        Task<InvitationCheckResponse> CheckInvitationAsync(string code);
        Task<InvitationResponse> AcceptInvitationAsync(string code, AcceptInvitationRequest request);
        Task<bool> UpdateStatusAsync(string code, string newStatus);
        //Task<InvitationStatsResponse> GetStatsAsync();
    }
}
