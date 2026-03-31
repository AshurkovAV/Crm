using Crm.Entity.ModelsCrm;
using Crm.Core.Infrastructure;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Crm.Entity.DTO;

namespace Crm.Entity.Services
{
    public class CrmRepository : ICrmRepository
    {
        public List<TaskDTO> GetTasksWithAccess(int userId)
        {
            using (var db = new CrmContext())
            {
                var tasksData = db.TaskCrms                    
                    .Where(t =>
                        // Доступ через проект
                        db.ProjectUsers.Any(pu => pu.UserId == userId && pu.IsActive && pu.ProjectId == t.ProjectId)
                        // ИЛИ пользователь назначен исполнителем
                        || t.Assignee == userId
                        // ИЛИ пользователь автор задачи
                        || t.Author == userId
                    )
                    .Select(t => new
                    {
                        t.Id,
                        t.Name,
                        t.Activity,
                        t.Deadline,
                        t.Author,
                        t.Assignee,
                        t.ProjectId,
                        t.Tags,
                        t.Status,
                        t.Priority,
                        t.Comments,
                        t.Description,
                        t.CreatedDate,
                        t.ModifiedDate,
                        t.IsOverdue,
                        ProjectName = t.Project != null ? t.Project.Name : null,
                        ProjectParticipants = t.Project.ProjectUsers
                            .Where(pu => pu.IsActive)
                            .Select(pu => new ParticipantDto
                            {
                                Id = pu.UserId,
                                DisplayName = pu.User.DisplayName ?? pu.User.DefaultEmail ?? "Участник"
                            })
                            .ToList()
                    })
                    .ToList();

                return tasksData.Select(t => new TaskDTO
                {
                    Id = t.Id,
                    Name = t.Name,
                    Activity = t.Activity,
                    Deadline = t.Deadline,
                    Author = t.Author,
                    Assignee = t.Assignee,
                    ProjectId = t.ProjectId,
                    ProjectName = t.ProjectName,
                    Tags = t.Tags,
                    Status = t.Status,
                    Priority = t.Priority,
                    Comments = t.Comments,
                    Description = t.Description,
                    CreatedDate = t.CreatedDate,
                    ModifiedDate = t.ModifiedDate,
                    IsOverdue = t.IsOverdue,
                    ProjectParticipants = t.ProjectParticipants,
                    ProjectParticipantsDisplay = string.Join(", ", t.ProjectParticipants.Select(p => p.DisplayName))
                }).ToList();
            }
        }
        public List<ProjectDTO> GetProjectsWithParticipantsDTO(int userId)
        {
            using (var db = new CrmContext())
            {            
                // Сначала получаем данные из базы без string.Join
                var projectsData = db.ProjectUsers
                    .Where(pu => pu.UserId == userId && pu.IsActive)
                    .Select(pu => pu.Project)
                    .Distinct()
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Activity,
                        p.Status,
                        p.CreatedDate,
                        p.ModifiedDate,
                        Participants = p.ProjectUsers
                            .Where(pu => pu.IsActive)
                            .Select(pu => pu.UserId)
                            .ToList(),
                        ParticipantDetails = p.ProjectUsers
                            .Where(pu => pu.IsActive)
                            .Select(pu => new ParticipantDto
                            {
                                Id = pu.UserId,
                                DisplayName = pu.User.DisplayName ?? pu.User.DefaultEmail ?? "Участник"
                            })
                            .ToList()
                                })
                    .ToList(); // Материализуем здесь

                // Теперь формируем DTO с string.Join в памяти
                return projectsData.Select(p => new ProjectDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Activity = p.Activity,
                    Status = p.Status,
                    CreatedDate = p.CreatedDate,
                    ModifiedDate = p.ModifiedDate,
                    Participants = p.Participants,
                    ParticipantsDisplay = string.Join(", ", p.ParticipantDetails.Select(n => n.DisplayName)),
                    Participantss = p.ParticipantDetails // Заполняем новое свойство
                }).ToList();
            }
        }       

        public List<Project> GetProjects(int userId)
        {
            var result = new List<Project>();
            using (var db = new CrmContext())
            {
                result = db.ProjectUsers
                .Where(pu => pu.UserId == userId && pu.IsActive)
                .Select(pu => pu.Project)
                .ToList();
            }
            return result;
        }

        public List<ProjectUser> GetProjectUsers(int projectId)
        {
            using (var db = new CrmContext())
            {
                return db.ProjectUsers
                .Where(pu => pu.ProjectId == projectId)
                .ToList();
            }                
        }

        public void AddProjectUser(ProjectUser projectUser)
        {
            using (var db = new CrmContext())
            {
                db.ProjectUsers.Add(projectUser);
                db.SaveChanges();
            }                
        }

        public void UpdateProjectUser(ProjectUser projectUser)
        {
            using (var db = new CrmContext())
            {
                db.Entry(projectUser).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public TransactionResult DeleteProject(int id)
        {
            var result = new TransactionResult();

            using (var db = new CrmContext())
            {
                try
                {
                    var resultProject = db.Projects.FirstOrDefault(x => x.Id == id);
                    if (resultProject != null)
                    {
                        db.Projects.Remove(resultProject);
                        db.SaveChanges();
                    }
                    else
                    {
                        throw new Exception("Запись не найдена");
                    }
                }
                catch (Exception ex)
                {
                    result.AddError(ex.Message);
                }
            }
            return result;
        }

        public TransactionResult UpdataTaskCrm(TaskCrm taskcrm)
        {
            var result = new TransactionResult();
            try
            {
                using (var db = new CrmContext())
                {
                    // 3. Помечаем запись как измененную и сохраняем
                    db.Entry(taskcrm).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                result.AddError(ex.Message);

            }
            return result;
        }

        public TransactionResult UpdataProject(Project project)
        {
            var result = new TransactionResult();
            try
            {
                using (var db = new CrmContext())
                {
                    // 3. Помечаем запись как измененную и сохраняем
                    db.Entry(project).State = EntityState.Modified;
                    db.SaveChanges();                    
                }
            } 
            catch (Exception ex) 
            {
                result.AddError(ex.Message);
                
            } 
            return result;
        }
        public TransactionResult InsertProjectUser(ProjectUser project)
        {
            var result = new TransactionResult();
            try
            {
                using (var db = new CrmContext())
                {
                    project.IsActive = true;
                    var data = db.Add(project);                    
                    db.SaveChanges();
                    result.Id = project.Id;
                }
            }
            catch (Exception ex)
            {
                result.AddError("Ошибка добавления записи в таблицу Project");
            }
            return result;
        }

        public TransactionResult InsertTaskCrm(TaskCrm taskcrm)
        {
            var result = new TransactionResult();
            try
            {
                using (var db = new CrmContext())
                {
                    var data = db.Add(taskcrm);
                    db.SaveChanges();
                    result.Id = taskcrm.Id;
                }
            }
            catch (Exception ex)
            {
                result.AddError("Ошибка добавления записи в таблицу Задачи");
            }
            return result;
        }

        public TransactionResult InsertProject(Project project)
        {
            var result = new TransactionResult();
            try
            {
                using (var db = new CrmContext())
                {
                    var data = db.Add(project);
                    db.SaveChanges();
                    result.Id = project.Id;
                }
            }
            catch (Exception ex)
            {        
                result.AddError("Ошибка добавления записи в таблицу Project");                
            }
            return result;
        }

        public TransactionResult<TaskCrm> GetTaskCrm(int id)
        {
            var result = new TransactionResult<TaskCrm>();

            using (var db = new CrmContext())
            {
                try
                {
                    var resultProject = db.TaskCrms.FirstOrDefault(x => x.Id == id);
                    if (resultProject == null)
                    {
                        result.Data = new TaskCrm();
                        throw new Exception("Задача не найдена");
                    }
                    result.Data = resultProject;
                }
                catch (Exception ex)
                {
                    result.AddError(ex.Message);
                }
            }
            return result;
        }


        public TransactionResult<Project> GetProject(int id)
        {
            var result = new TransactionResult<Project>();

            using (var db = new CrmContext())
            {
                try
                {
                    var resultProject = db.Projects.FirstOrDefault(x => x.Id == id);
                    if (resultProject == null)
                    {                        
                        result.Data = new Project();
                        throw new Exception("Проект не найден");
                    }
                    result.Data = resultProject;
                } catch (Exception ex) 
                {
                    result.AddError(ex.Message);
                }
            }
            return result;
        }

        public IEnumerable<User> GetUsers()
        {
            var result = new List<User>();
            using (var db = new CrmContext())
            {
                result = db.Users.Where(x => x.IsActive == true).ToList();
            }
            return result;
        }

        public TransactionResult<User> GetUser(string email)
        {
            var result = new TransactionResult<User>();
            try
            {
                using (var db = new CrmContext())
                {
                    var user = db.Users.Where(x => x.DefaultEmail == email && x.IsActive == true).FirstOrDefault();
                    if (user == null)
                    {
                        throw new Exception("Пользователь не найден, либо не активен");
                    }
                    result.Data = user;
                }
            }
            catch (Exception ex)
            {
                result.AddError(ex);
            }

            return result;
        }
    }
}
