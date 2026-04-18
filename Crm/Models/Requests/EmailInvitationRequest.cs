namespace Crm.Models.Requests
{
    public class EmailInvitationRequest
    {
        public string[] Emails { get; set; }
        public int? DepartmentId { get; set; }
        public string Message { get; set; }
    }
}
