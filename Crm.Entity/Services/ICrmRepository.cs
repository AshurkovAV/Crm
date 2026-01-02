using Crm.Core.Infrastructure;
using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services
{
    public interface ICrmRepository
    {
        TransactionResult<User> GetUser(string email);
        IEnumerable<User> GetUsers();
        List<Project> GetProjects(int userId);
        TransactionResult InsertProjectUser(ProjectUser project);
        TransactionResult<Project> GetProject(int id);
        TransactionResult InsertProject(Project project);
        TransactionResult UpdataProject(Project project);
        TransactionResult DeleteProject(int id);
    }
}