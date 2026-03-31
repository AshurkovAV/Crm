using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

/// <summary>
/// Таблица задач проекта. Содержит все задачи, назначенные на сотрудников, с указанием сроков и статусов.
/// </summary>
public partial class TaskCrm
{
    /// <summary>
    /// Уникальный идентификатор задачи. Автоинкрементное поле (IDENTITY). Первичный ключ таблицы.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Название задачи. Обязательное поле. Отображается в списке задач как основное описание.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Дата и время последней активности по задаче. Обновляется при любом изменении: комментарий, смена статуса, редактирование. Формат: YYYY-MM-DD HH:MI:SS
    /// </summary>
    public DateTime? Activity { get; set; }

    /// <summary>
    /// Крайний срок выполнения задачи. Используется для контроля просрочек и планирования. При превышении текущей даты задача считается просроченной.
    /// </summary>
    public DateTime? Deadline { get; set; }

    /// <summary>
    /// Исполнитель задачи. ФИО сотрудника, ответственного за выполнение. Отображается в столбце &quot;Исполнитель&quot; интерфейса.
    /// </summary>
    public int? Assignee { get; set; }

    /// <summary>
    /// Постановщик задачи. ФИО сотрудника, создавшего задачу. Отображается в столбце &quot;Постановщик&quot; интерфейса.
    /// </summary>
    public int? Author { get; set; }

    /// <summary>
    /// Внешний ключ на таблицу Project. Определяет принадлежность задачи к конкретному проекту. Может быть NULL, если задача не привязана к проекту. При удалении проекта значение становится NULL (ON DELETE SET NULL).
    /// </summary>
    public int? ProjectId { get; set; }

    /// <summary>
    /// Теги задачи. Хранятся в виде строки с разделителями (например: &quot;срочно,важно,клиент&quot;). Используются для категоризации и фильтрации задач. Максимальная длина - 500 символов.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Статус выполнения задачи. Возможные значения: &quot;Новая&quot;, &quot;В работе&quot;, &quot;Просрочена&quot;, &quot;Завершена&quot;, &quot;Отменена&quot;. По умолчанию - &quot;Новая&quot;.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// Приоритет задачи. Возможные значения: &quot;Высокий&quot;, &quot;Средний&quot;, &quot;Низкий&quot;. Используется для сортировки задач по важности.
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// Флаг просрочки задачи. Вычисляемое поле: TRUE, если Deadline &lt; текущей даты и статус не &quot;Завершена&quot;. Используется для быстрой фильтрации просроченных задач.
    /// </summary>
    public bool? IsOverdue { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// Комментарии к задаче. Текстовое поле неограниченной длины (MAX). Хранит обсуждения, уточнения и историю выполнения задачи.
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// Дата и время создания задачи. Автоматически устанавливается при вставке записи. Значение по умолчанию - GETDATE().
    /// </summary>
    public DateTime? CreatedDate { get; set; }

    /// <summary>
    /// Дата и время последнего изменения задачи. Автоматически обновляется при изменении записи. Значение по умолчанию - GETDATE().
    /// </summary>
    public DateTime? ModifiedDate { get; set; }

    public virtual Project? Project { get; set; }
}
