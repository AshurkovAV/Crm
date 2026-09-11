using System;
using System.Collections.Generic;

namespace Crm.Entity.DTO
{
    /// <summary>
    /// Публичные данные для страницы отслеживания заказа (Модуль Г ТЗ, "эффект Додо").
    /// Отдаются анонимному клиенту по Deal.PublicToken — сюда НЕЛЬЗЯ добавлять
    /// ClientName/телефон/email клиента, CompanyId, внутренние Notes/Description,
    /// себестоимость (CostPrice) или маржу (MarginPercent) изделий.
    /// </summary>
    public class PublicTrackingDto
    {
        public string CompanyName { get; set; } = string.Empty;

        public string DealTitle { get; set; } = string.Empty;

        public string DealStatus { get; set; } = string.Empty;

        public DateTime? ExpectedCloseDate { get; set; }

        /// <summary>
        /// Общий процент готовности заказа (0-100), см. логику расчёта
        /// в PublicTrackingRepository.GetByPublicTokenAsync.
        /// </summary>
        public int OverallProgressPercent { get; set; }

        public List<PublicOrderItemDto> Items { get; set; } = new();
    }

    public class PublicOrderItemDto
    {
        public string Name { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string Status { get; set; } = string.Empty;

        public List<PublicStageDto> Stages { get; set; } = new();

        /// <summary>Фото изделия с этапа ОТК (если оно загружено рабочим), либо null.</summary>
        public string? PhotoUrl { get; set; }
    }

    public class PublicStageDto
    {
        public string StageName { get; set; } = string.Empty;

        public int StageOrder { get; set; }

        /// <summary>"done" | "current" | "pending" — состояние этапа для отрисовки прогресс-шагов.</summary>
        public string State { get; set; } = "pending";
    }
}
