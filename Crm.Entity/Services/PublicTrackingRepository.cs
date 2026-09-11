using System;
using System.Linq;
using System.Threading.Tasks;
using Crm.Entity.DTO;
using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;

namespace Crm.Entity.Services;

public class PublicTrackingRepository : IPublicTrackingRepository
{
    public async Task<PublicTrackingDto?> GetByPublicTokenAsync(Guid token)
    {
        await using var db = new CrmContext();

        var deal = await db.Deals
            .AsNoTracking()
            .Include(d => d.Company)
            .Include(d => d.OrderItems)
                .ThenInclude(oi => oi.ProductionTasks)
            .FirstOrDefaultAsync(d => d.PublicToken == token);

        if (deal == null)
            return null;

        var allTasks = deal.OrderItems.SelectMany(oi => oi.ProductionTasks).ToList();

        return new PublicTrackingDto
        {
            CompanyName = deal.Company?.Name ?? string.Empty,
            DealTitle = deal.Title,
            DealStatus = deal.Status,
            ExpectedCloseDate = deal.ExpectedCloseDate,
            OverallProgressPercent = CalculateOverallProgressPercent(deal, allTasks),
            Items = deal.OrderItems
                .OrderBy(oi => oi.OrderItemId)
                .Select(MapOrderItem)
                .ToList()
        };
    }

    /// <summary>
    /// Прогресс сделки = доля завершённых производственных этапов (ProductionTask)
    /// от их общего числа по всем изделиям (OrderItem) сделки.
    /// Завершённость этапа определяется по факту простановки CompletedAt, а не по
    /// текстовому значению Status — у Status пока нет фиксированного словаря значений,
    /// а дата завершения проставляется однозначно и не зависит от формулировок.
    /// Если этапы производства ещё не заведены (изделия не декомпозированы на стадии),
    /// используем упрощённую эвристику по Status самого изделия.
    /// </summary>
    private static int CalculateOverallProgressPercent(Deal deal, System.Collections.Generic.List<ProductionTask> allTasks)
    {
        if (allTasks.Count > 0)
        {
            var completed = allTasks.Count(t => t.CompletedAt.HasValue);
            return (int)Math.Round(completed * 100.0 / allTasks.Count);
        }

        if (deal.OrderItems.Count > 0)
        {
            var doneKeywords = new[] { "готов", "заверш", "выполнен", "done", "completed" };
            var doneCount = deal.OrderItems.Count(oi =>
                doneKeywords.Any(k => (oi.Status ?? string.Empty).ToLowerInvariant().Contains(k)));
            return (int)Math.Round(doneCount * 100.0 / deal.OrderItems.Count);
        }

        return 0;
    }

    private static PublicOrderItemDto MapOrderItem(OrderItem orderItem)
    {
        var stages = orderItem.ProductionTasks
            .OrderBy(t => t.StageOrder)
            .Select(t => new PublicStageDto
            {
                StageName = t.StageName,
                StageOrder = t.StageOrder,
                State = t.CompletedAt.HasValue ? "done" : (t.StartedAt.HasValue ? "current" : "pending")
            })
            .ToList();

        return new PublicOrderItemDto
        {
            Name = orderItem.Name,
            Quantity = orderItem.Quantity,
            Status = orderItem.Status,
            Stages = stages,
            PhotoUrl = FindOtkPhotoUrl(orderItem)
        };
    }

    /// <summary>
    /// Фото изделия с этапа ОТК: ищем среди этапов с загруженным фото тот, чьё название
    /// похоже на "ОТК" (без учёта регистра); если такого явно не нашлось — берём фото
    /// с последнего по порядку этапа (StageOrder), т.к. на практике это и есть финальная
    /// приёмка/проверка перед отгрузкой клиенту.
    /// </summary>
    private static string? FindOtkPhotoUrl(OrderItem orderItem)
    {
        var stagesWithPhoto = orderItem.ProductionTasks
            .Where(t => !string.IsNullOrWhiteSpace(t.PhotoUrl))
            .ToList();

        if (stagesWithPhoto.Count == 0)
            return null;

        var otkStage = stagesWithPhoto
            .Where(t => t.StageName.Contains("отк", StringComparison.OrdinalIgnoreCase)
                     || t.StageName.Contains("otk", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.StageOrder)
            .FirstOrDefault();

        var fallbackLastStage = stagesWithPhoto
            .OrderByDescending(t => t.StageOrder)
            .First();

        return (otkStage ?? fallbackLastStage).PhotoUrl;
    }
}
