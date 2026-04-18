namespace Crm.Entity.Entities
{
    public class Company: ModelsCrm.Company
    {
        private Company(
                    string name, int ownerId
                    )
        {
            Name = name;
            OwnerId = ownerId;
            CreatedDate = DateTime.UtcNow;
            ModifiedDate = DateTime.UtcNow;
            IsActive = true;
            InviteCode = Guid.NewGuid().ToString();
        }

        public static Company Create(
            string name,
            int ownerId)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Наименование компании пусто", nameof(name));
            if (ownerId == 0)
                throw new ArgumentException("Владелец компании пусто", nameof(ownerId));


            // Создание и возврат объекта
            return new Company(
               name.ToLower(), ownerId // Нормализация email
            );
        }
    }
}
