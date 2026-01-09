using Crm.Entity.ModelsCrm;
using Crm.Core.Infrastructure;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;

namespace Crm.Entity.Services
{
    public class CrmRepository : ICrmRepository
    {
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
