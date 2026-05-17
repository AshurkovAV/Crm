namespace Crm.Models.Profil
{
    public class EditProfileModel
    {
        // Основная информация
        public string DisplayName { get; set; }
        public string RealName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string Sex { get; set; }
        public string Birthday { get; set; }
        public string BirthPlace { get; set; }

        // Контакты
        public string DefaultEmail { get; set; }
        public string AlternativeEmail { get; set; }
        public string DefaultPhone { get; set; }
        public string AlternativePhone { get; set; }
        public string WorkPhone { get; set; }

        // Мессенджеры
        public string Telegram { get; set; }
        public string WhatsApp { get; set; }

        // Работа
        public string Position { get; set; }
        public string Department { get; set; }

        // Личное
        public string Address { get; set; }
        public string Bio { get; set; }
        public string Website { get; set; }
        public string LinkedIn { get; set; }
    }
}
