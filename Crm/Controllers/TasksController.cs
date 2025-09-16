using Microsoft.AspNetCore.Mvc;

public class TasksController : Controller
{
    public IActionResult Index()
    {
        // Логика для получения задач
        var tasks = new List<TaskViewModel>
        {
            new TaskViewModel { Project = "Разработка CRM", Task = "Создать интерфейс задач", Status = "В работе", Deadline = "25.05.2023" },
            new TaskViewModel { Project = "Маркетинг", Task = "Подготовить кампанию", Status = "Запланировано", Deadline = "30.05.2023" }
        };

        return View(tasks);
    }
}

public class TaskViewModel
{
    public string Project { get; set; }
    public string Task { get; set; }
    public string Status { get; set; }
    public string Deadline { get; set; }
}