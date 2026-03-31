using Crm.Core.Infrastructure;
using Crm.Entity.DTO;
using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services
{
    public interface ICrmRepository
    {
        List<TaskDTO> GetTasksWithAccess(int userId);
        List<ProjectDTO> GetProjectsWithParticipantsDTO(int userId);
        TransactionResult<User> GetUser(string email);
        IEnumerable<User> GetUsers();
        List<Project> GetProjects(int userId);
        List<ProjectUser> GetProjectUsers(int projectId);
        void AddProjectUser(ProjectUser projectUser);
        void UpdateProjectUser(ProjectUser projectUser);
        TransactionResult InsertProjectUser(ProjectUser project);
        TransactionResult<Project> GetProject(int id);
        TransactionResult<TaskCrm> GetTaskCrm(int id);
        TransactionResult InsertTaskCrm(TaskCrm taskcrm);
        TransactionResult InsertProject(Project project);
        TransactionResult UpdataProject(Project project);
        TransactionResult UpdataTaskCrm(TaskCrm taskcrm);
        TransactionResult DeleteProject(int id);
    }
}