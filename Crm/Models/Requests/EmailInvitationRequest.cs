namespace Crm.Models.Requests
{
    public class EmailInvitationRequest
    {
        public string[] Emails { get; set; }
        public int? DepartmentId { get; set; }
        public string Message { get; set; }
        /// <summary>Срок действия ссылки в днях (1–30)</summary>
        public int? ExpiryDays { get; set; }
    }
}
