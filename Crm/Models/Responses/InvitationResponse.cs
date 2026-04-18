namespace Crm.Models.Responses
{
    public class InvitationResponse
    {
        public bool Success { get; set; }
        public int Count { get; set; }
        public string Message { get; set; }
        public int UserId { get; set; }
    }
}
