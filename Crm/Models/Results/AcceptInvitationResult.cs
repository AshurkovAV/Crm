namespace Crm.Models.Results
{
    /// <summary>
    /// Результат принятия приглашения
    /// </summary>
    public class AcceptInvitationResult
    {
        /// <summary>
        /// Успешность операции
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Сообщение (успех или ошибка)
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// ID компании, в которую добавлен пользователь
        /// </summary>
        public int? CompanyId { get; set; }

        /// <summary>
        /// Название компании
        /// </summary>
        public string CompanyName { get; set; }

        /// <summary>
        /// ID пользователя (нового или существующего)
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Email пользователя
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Роль, назначенная в компании
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Должность в компании
        /// </summary>
        public string Position { get; set; }

        /// <summary>
        /// Была ли это первая компания пользователя
        /// </summary>
        public bool IsFirstCompany { get; set; }

        /// <summary>
        /// Список ошибок (если есть)
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Успешный результат для нового пользователя
        /// </summary>
        public static AcceptInvitationResult SuccessForNewUser(int userId, string email, int companyId, string companyName, string role, string position)
        {
            return new AcceptInvitationResult
            {
                Success = true,
                Message = $"Вы успешно зарегистрировались и присоединились к компании {companyName}",
                UserId = userId,
                Email = email,
                CompanyId = companyId,
                CompanyName = companyName,
                Role = role,
                Position = position,
                IsFirstCompany = true
            };
        }

        /// <summary>
        /// Успешный результат для существующего пользователя
        /// </summary>
        public static AcceptInvitationResult SuccessForExistingUser(int userId, string email, int companyId, string companyName, string role, string position, bool isFirstCompany)
        {
            return new AcceptInvitationResult
            {
                Success = true,
                Message = $"Вы успешно присоединились к компании {companyName}",
                UserId = userId,
                Email = email,
                CompanyId = companyId,
                CompanyName = companyName,
                Role = role,
                Position = position,
                IsFirstCompany = isFirstCompany
            };
        }

        /// <summary>
        /// Результат с ошибкой
        /// </summary>
        public static AcceptInvitationResult Error(string errorMessage)
        {
            return new AcceptInvitationResult
            {
                Success = false,
                Message = errorMessage,
                Errors = new List<string> { errorMessage }
            };
        }

        /// <summary>
        /// Результат с несколькими ошибками
        /// </summary>
        public static AcceptInvitationResult Error(List<string> errorMessages)
        {
            return new AcceptInvitationResult
            {
                Success = false,
                Message = string.Join("; ", errorMessages),
                Errors = errorMessages
            };
        }
    }
}
