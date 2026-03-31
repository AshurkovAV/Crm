
using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services
{
    public interface INsiRepository
    {
        public List<RefStatus> GetRefStatuses();
    }
}
