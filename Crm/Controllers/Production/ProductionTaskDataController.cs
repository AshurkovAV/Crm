using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using Crm.Models.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Crm.Controllers;

/// <summary>
/// API производственного конвейера (Модуль Б ТЗ): канбан-доска цеха, смена статусов этапов,
/// генерация дефолтных стадий изделия, ручное добавление/удаление стадий, загрузка фото ОТК.
/// </summary>
[Authorize]
[Route("api/v1/production")]
public class ProductionTaskDataController : Controller
{
    private static readonly string[] AllowedImageContentTypes =
    {
        "image/jpeg", "image/png", "image/webp", "image/gif", "image/heic", "image/heif"
    };

    private const long MaxPhotoSizeBytes = 15 * 1024 * 1024; // 15 МБ — с запасом под фото со смартфона.

    private readonly IProductionTaskRepository _productionTaskRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProductionTaskDataController(IProductionTaskRepository productionTaskRepository, IWebHostEnvironment webHostEnvironment)
    {
        _productionTaskRepository = productionTaskRepository;
        _webHostEnvironment = webHostEnvironment;
    }

    /// <summary>
    /// GET api/v1/production/tasks/kanban?stageName=&status= — плоский список активных этапов
    /// компании для канбан-доски цеха (группировку по колонкам делает фронт).
    /// </summary>
    [HttpGet("tasks/kanban")]
    public async Task<IActionResult> GetKanban(string? stageName = null, string? status = null)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var tasks = await _productionTaskRepository.GetByCompanyKanbanAsync(userId.Value, stageName, status);
        var maxOrderByOrderItem = tasks
            .GroupBy(t => t.OrderItemId)
            .ToDictionary(g => g.Key, g => g.Max(t => t.StageOrder));
        return Json(tasks.Select(task => ToCardDto(task, maxOrderByOrderItem[task.OrderItemId])));
    }

    /// <summary>
    /// GET api/v1/production/tasks/by-order-item/{orderItemId} — этапы конкретного изделия.
    /// </summary>
    [HttpGet("tasks/by-order-item/{orderItemId:int}")]
    public async Task<IActionResult> GetByOrderItem(int orderItemId)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var tasks = await _productionTaskRepository.GetByOrderItemAsync(orderItemId, userId.Value);
        var maxOrder = tasks.Count > 0 ? tasks.Max(t => t.StageOrder) : 0;
        return Json(tasks.Select(task => ToCardDto(task, maxOrder)));
    }

    /// <summary>
    /// GET api/v1/production/order-items — изделия компании (для выбора при запуске производства).
    /// Читаем напрямую через CrmContext: отдельного репозитория OrderItem нет, а плодить его
    /// только под один лёгкий lookup для канбана избыточно (ведёт этим модуль калькулятора/склада).
    /// </summary>
    [HttpGet("order-items")]
    public async Task<IActionResult> GetOrderItems()
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        await using var db = new CrmContext();
        var companyId = await db.Users
            .Where(u => u.Id == userId.Value)
            .Select(u => u.CurrentCompanyId)
            .FirstOrDefaultAsync();
        if (!companyId.HasValue)
            return Json(Array.Empty<object>());

        var items = await db.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Deal)
                .ThenInclude(deal => deal.Client)
            .Where(oi => oi.Deal.CompanyId == companyId.Value)
            .OrderByDescending(oi => oi.CreatedDate)
            .Select(oi => new
            {
                oi.OrderItemId,
                oi.Name,
                oi.DealId,
                DealTitle = oi.Deal.Title,
                ClientName = oi.Deal.Client != null ? oi.Deal.Client.Name : oi.Deal.ClientName,
                HasStages = db.ProductionTasks.Any(t => t.OrderItemId == oi.OrderItemId)
            })
            .ToListAsync();

        return Json(items);
    }

    /// <summary>
    /// POST api/v1/production/tasks/generate-stages — создать дефолтный набор этапов
    /// (Резка/Обработка/Сборка/ОТК) для изделия, если этапов ещё нет.
    /// </summary>
    [HttpPost("tasks/generate-stages")]
    public async Task<IActionResult> GenerateStages([FromBody] GenerateStagesRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (request.OrderItemId <= 0)
            return BadRequest(new { message = "Не указано изделие." });

        var tasks = await _productionTaskRepository.GenerateDefaultStagesAsync(request.OrderItemId, userId.Value);
        if (tasks == null)
            return NotFound(new { message = "Изделие не найдено или недоступно." });

        var maxOrder = tasks.Count > 0 ? tasks.Max(t => t.StageOrder) : 0;
        return Json(tasks.Select(task => ToCardDto(task, maxOrder)));
    }

    /// <summary>
    /// PATCH api/v1/production/tasks/{id}/status — смена статуса производственного этапа
    /// (с логированием времени начала/окончания). На финальном этапе (ОТК) требует фото.
    /// </summary>
    [HttpPatch("tasks/{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] ProductionTaskStatusRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(new { message = "Не указан статус." });

        var result = await _productionTaskRepository.UpdateStatusAsync(id, userId.Value, request.Status.Trim(), request.PhotoUrl);
        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Json(ToCardDtoBasic(result.Task!));
    }

    /// <summary>
    /// POST api/v1/production/tasks/{id}/assign — назначить внутреннего исполнителя на этап.
    /// </summary>
    [HttpPost("tasks/{id:int}/assign")]
    public async Task<IActionResult> AssignUser(int id, [FromBody] AssignUserRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var ok = await _productionTaskRepository.AssignUserAsync(id, userId.Value, request.AssignedUserId);
        return ok ? Ok() : NotFound(new { message = "Этап не найден." });
    }

    /// <summary>
    /// POST api/v1/production/tasks/stages — добавить произвольный этап изделию вручную.
    /// </summary>
    [HttpPost("tasks/stages")]
    public async Task<IActionResult> AddStage([FromBody] AddStageRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (request.OrderItemId <= 0 || string.IsNullOrWhiteSpace(request.StageName))
            return BadRequest(new { message = "Укажите изделие и название этапа." });

        var task = await _productionTaskRepository.AddStageAsync(
            request.OrderItemId, userId.Value, request.StageName, request.ExecutorType, request.AssignedUserId, request.ContractorId);
        if (task == null)
            return NotFound(new { message = "Изделие не найдено или недоступно." });

        return Json(ToCardDtoBasic(task));
    }

    /// <summary>
    /// DELETE api/v1/production/tasks/{id} — удалить произвольный этап (нельзя удалить завершённый).
    /// </summary>
    [HttpDelete("tasks/{id:int}")]
    public async Task<IActionResult> DeleteStage(int id)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var ok = await _productionTaskRepository.DeleteStageAsync(id, userId.Value);
        return ok ? Ok() : BadRequest(new { message = "Этап не найден или уже завершён — удаление запрещено." });
    }

    /// <summary>
    /// POST api/v1/production/tasks/{id}/photo — фотофиксация этапа (ОТК): приём файла с камеры/галереи
    /// планшета, сохранение в wwwroot/uploads/production/{id}/, возвращает относительный URL для PATCH status.
    /// </summary>
    [HttpPost("tasks/{id:int}/photo")]
    [RequestSizeLimit(MaxPhotoSizeBytes)]
    public async Task<IActionResult> UploadPhoto(int id, IFormFile? file)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Файл не передан." });

        if (file.Length > MaxPhotoSizeBytes)
            return BadRequest(new { message = "Файл слишком большой (максимум 15 МБ)." });

        if (!AllowedImageContentTypes.Contains(file.ContentType?.ToLowerInvariant()))
            return BadRequest(new { message = "Допускаются только файлы изображений." });

        var belongsToCompany = await TaskBelongsToUserCompanyAsync(id, userId.Value);
        if (!belongsToCompany)
            return NotFound(new { message = "Этап не найден." });

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || extension.Length > 10)
        {
            extension = file.ContentType?.ToLowerInvariant() switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                "image/heic" => ".heic",
                "image/heif" => ".heif",
                _ => ".jpg"
            };
        }

        var webRoot = _webHostEnvironment.WebRootPath;
        var folderRelative = Path.Combine("uploads", "production", id.ToString());
        var folderAbsolute = Path.Combine(webRoot, folderRelative);
        Directory.CreateDirectory(folderAbsolute);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folderAbsolute, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = "/" + Path.Combine(folderRelative, fileName).Replace('\\', '/');
        return Json(new { url = relativeUrl });
    }

    private async Task<bool> TaskBelongsToUserCompanyAsync(int taskId, int userId)
    {
        await using var db = new CrmContext();
        var companyId = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.CurrentCompanyId)
            .FirstOrDefaultAsync();
        if (!companyId.HasValue)
            return false;

        return await db.ProductionTasks
            .Where(t => t.ProductionTaskId == taskId && t.OrderItem.Deal.CompanyId == companyId.Value)
            .AnyAsync();
    }

    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    private static ProductionTaskCardDto ToCardDto(ProductionTask task, int maxOrderInGroup)
    {
        return new ProductionTaskCardDto
        {
            ProductionTaskId = task.ProductionTaskId,
            OrderItemId = task.OrderItemId,
            StageName = task.StageName,
            StageOrder = task.StageOrder,
            IsFinalStage = string.Equals(task.StageName, "ОТК", StringComparison.OrdinalIgnoreCase)
                           || task.StageOrder >= maxOrderInGroup,
            ExecutorType = task.ExecutorType,
            AssignedUserId = task.AssignedUserId,
            AssignedUserName = task.AssignedUser?.DisplayName ?? task.AssignedUser?.RealName,
            ContractorId = task.ContractorId,
            ContractorName = task.Contractor?.Name,
            Status = task.Status,
            StartedAt = task.StartedAt,
            CompletedAt = task.CompletedAt,
            PhotoUrl = task.PhotoUrl,
            Notes = task.Notes,
            OrderItemName = task.OrderItem?.Name ?? string.Empty,
            DealId = task.OrderItem?.DealId ?? 0,
            DealTitle = task.OrderItem?.Deal?.Title ?? string.Empty,
            ClientName = task.OrderItem?.Deal?.Client?.Name ?? task.OrderItem?.Deal?.ClientName
        };
    }

    private static ProductionTaskCardDto ToCardDtoBasic(ProductionTask task) => new()
    {
        ProductionTaskId = task.ProductionTaskId,
        OrderItemId = task.OrderItemId,
        StageName = task.StageName,
        StageOrder = task.StageOrder,
        IsFinalStage = string.Equals(task.StageName, "ОТК", StringComparison.OrdinalIgnoreCase),
        ExecutorType = task.ExecutorType,
        AssignedUserId = task.AssignedUserId,
        ContractorId = task.ContractorId,
        Status = task.Status,
        StartedAt = task.StartedAt,
        CompletedAt = task.CompletedAt,
        PhotoUrl = task.PhotoUrl,
        Notes = task.Notes
    };
}
