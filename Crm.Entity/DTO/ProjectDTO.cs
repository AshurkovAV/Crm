using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Entity.DTO
{
    public class ProjectDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Activity { get; set; }
        public string Status { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public List<Participant> Participants { get; set; }
    }

    public class Participant
    {
        public int UserId { get; set; }
        public UserInfo User { get; set; }
    }

    public class UserInfo
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
    }
}
