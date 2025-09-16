using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace Crm.Controllers
{
    [Authorize]
    [Route("crmsetup")]
    public class CrmSetupController : Controller
    {
        private static readonly ConcurrentDictionary<string, SetupProcess> _processes =
            new ConcurrentDictionary<string, SetupProcess>();

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("start")]
        public IActionResult StartProcess()
        {
            var processId = Guid.NewGuid().ToString();
            var process = new SetupProcess(processId);

            _processes[processId] = process;

            // Запускаем процесс в фоновом режиме
            Task.Run(() => process.ExecuteAsync());

            return Ok(new { processId });
        }

        [HttpGet("status/{processId}")]
        public IActionResult GetStatus(string processId)
        {
            if (!_processes.TryGetValue(processId, out var process))
            {
                return NotFound(new { error = "Процесс не найден" });
            }

            return Ok(new
            {
                completed = process.IsCompleted,
                failed = process.IsFailed,
                error = process.Error
            });
        }
    }

    public class SetupProcess
    {
        public string ProcessId { get; }
        public bool IsCompleted { get; private set; }
        public bool IsFailed { get; private set; }
        public string Error { get; private set; }

        public SetupProcess(string processId)
        {
            ProcessId = processId;
        }

        public async Task ExecuteAsync()
        {
            try
            {
                // Имитация длительного процесса установки (15-25 секунд)
                var random = new Random();
                var duration = TimeSpan.FromSeconds(random.Next(15, 25));

                await Task.Delay(duration);

                IsCompleted = true;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                IsFailed = true;
            }
        }
    }
}
