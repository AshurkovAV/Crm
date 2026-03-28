using Crm.Core.Infrastructure;
using Crm.Entity.DTO;
using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services
{
    public interface ICrmRepository
    {
        List<ProjectDTO> GetProjectsWithParticipantsDTO(int userId);
        TransactionResult<User> GetUser(string email);
        IEnumerable<User> GetUsers();
        List<Project> GetProjects(int userId);
        List<ProjectUser> GetProjectUsers(int projectId);
        void AddProjectUser(ProjectUser projectUser);
        void UpdateProjectUser(ProjectUser projectUser);
        TransactionResult InsertProjectUser(ProjectUser project);
        TransactionResult<Project> GetProject(int id);
        TransactionResult InsertProject(Project project);
        TransactionResult UpdataProject(Project project);
        TransactionResult DeleteProject(int id);
    }
}