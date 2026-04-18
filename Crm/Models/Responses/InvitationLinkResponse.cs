namespace Crm.Models.Responses
{
    public class InvitationLinkResponse
    {
        public string Link { get; set; }
        public string Code { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
