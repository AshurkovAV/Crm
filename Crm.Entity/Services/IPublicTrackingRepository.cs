using System;
using System.Threading.Tasks;
using Crm.Entity.DTO;

namespace Crm.Entity.Services;

/// <summary>
/// Публичный (анонимный) доступ к статусу заказа для клиентского трекера
/// (Модуль Г ТЗ). Поиск идёт по Deal.PublicToken — числовой Deal.Id в публичном
/// API намеренно не используется, чтобы исключить перебор чужих заказов.
/// </summary>
public interface IPublicTrackingRepository
{
    /// <summary>
    /// Возвращает публичные данные по сделке с данным PublicToken, либо null,
    /// если токен не найден — в этом случае вызывающий код должен вернуть
    /// общее сообщение ("Заказ не найден"), не раскрывая деталей.
    /// </summary>
    Task<PublicTrackingDto?> GetByPublicTokenAsync(Guid token);
}
