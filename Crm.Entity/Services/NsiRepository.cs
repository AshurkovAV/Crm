using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services
{
    public class NsiRepository : INsiRepository
    {
        public List<RefStatus> GetRefStatuses()
        {
            var result = new List<RefStatus>(); 
            using (var db = new CrmContext())
            {
                result = db.RefStatuses           
                .ToList();
            }
            return result;
        }
    }
}
