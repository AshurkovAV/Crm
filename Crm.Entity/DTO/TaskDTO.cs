using System.ComponentModel.DataAnnotations.Schema;

namespace Crm.Entity.DTO
{
    public class TaskDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? Activity { get; set; }
        public DateTime? Deadline { get; set; }
        public int? Author { get; set; }
        public int? Assignee { get; set; }
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Tags { get; set; }
        // Если нужно для удобства в коде, добавьте вычисляемое свойство
        [NotMapped]
        public string[] TagsList
        {
            get => Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
            set => Tags = value != null ? string.Join(",", value) : null;
        }
        public int? Status { get; set; }
        public string Priority { get; set; }
        public bool? IsOverdue { get; set; }
        public string? Comments { get; set; }
        public decimal? EstimatedHours { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        // Для отображения участников проекта
        public List<ParticipantDto> ProjectParticipants { get; set; }
        public string ProjectParticipantsDisplay { get; set; }
    }
}
