using System.ComponentModel.DataAnnotations;

namespace Crm.Models.Compan
{
    // Models/Company/AcceptInvitationViewModel.cs
  
        public class AcceptInvitationViewModel
        {
            /// <summary>
            /// Код приглашения
            /// </summary>
            public string Code { get; set; }

            /// <summary>
            /// Email, на который было отправлено приглашение
            /// </summary>
            [EmailAddress(ErrorMessage = "Некорректный email адрес")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            /// <summary>
            /// Телефон, если был указан в приглашении
            /// </summary>
            [Phone(ErrorMessage = "Некорректный номер телефона")]
            [Display(Name = "Телефон")]
            public string Phone { get; set; }

            /// <summary>
            /// ID отдела, в который приглашен сотрудник
            /// </summary>
            public int? DepartmentId { get; set; }

            /// <summary>
            /// Название отдела
            /// </summary>
            public string DepartmentName { get; set; }

            /// <summary>
            /// Имя пользователя (для формы)
            /// </summary>
            [Required(ErrorMessage = "Имя обязательно")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "Имя должно быть от 2 до 100 символов")]
            [Display(Name = "Имя и фамилия")]
            public string DisplayName { get; set; }

            /// <summary>
            /// Пароль (для формы)
            /// </summary>
            [Required(ErrorMessage = "Пароль обязателен")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть не менее 6 символов")]
            [DataType(DataType.Password)]
            [Display(Name = "Пароль")]
            public string Password { get; set; }

            /// <summary>
            /// Подтверждение пароля (для формы)
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Подтверждение пароля")]
            [Compare("Password", ErrorMessage = "Пароли не совпадают")]
            public string ConfirmPassword { get; set; }

            /// <summary>
            /// Должность (необязательно)
            /// </summary>
            [StringLength(100, ErrorMessage = "Должность не должна превышать 100 символов")]
            [Display(Name = "Должность")]
            public string Position { get; set; }

            /// <summary>
            /// ID компании (обычно берется из приглашения или контекста)
            /// </summary>
            public int? CompanyId { get; set; }

            /// <summary>
            /// Название компании
            /// </summary>
            public string CompanyName { get; set; }

            /// <summary>
            /// Тип приглашения (Email, SMS, Link)
            /// </summary>
            public string InvitationType { get; set; }

            /// <summary>
            /// Оставшееся количество дней действия приглашения
            /// </summary>
            public int? DaysLeft { get; set; }

            /// <summary>
            /// Дата истечения срока действия
            /// </summary>
            public DateTime? ExpiresAt { get; set; }

            /// <summary>
            /// ID существующего пользователя (если email уже зарегистрирован)
            /// </summary>
            public int? ExistingUserId { get; set; }

            /// <summary>
            /// Признак существующего пользователя
            /// </summary>
            public bool IsExistingUser => ExistingUserId.HasValue;

            /// <summary>
            /// Дополнительное сообщение для отображения
            /// </summary>
            public string Message { get; set; }

            /// <summary>
            /// Список ошибок валидации
            /// </summary>
            public List<string> Errors { get; set; } = new List<string>();

            /// <summary>
            /// Признак успешной операции
            /// </summary>
            public bool Success { get; set; }

            /// <summary>
            /// Проверка: есть ли ошибки
            /// </summary>
            public bool HasErrors => Errors != null && Errors.Any();

            /// <summary>
            /// Проверка: просрочено ли приглашение
            /// </summary>
            public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

            /// <summary>
            /// Форматированная дата истечения срока
            /// </summary>
            public string ExpiresAtFormatted => ExpiresAt?.ToString("dd.MM.yyyy HH:mm");

            /// <summary>
            /// Сообщение о сроке действия
            /// </summary>
            public string ExpiryMessage
            {
                get
                {
                    if (!ExpiresAt.HasValue)
                        return "Срок действия не ограничен";

                    if (IsExpired)
                        return "Срок действия истек";

                    if (DaysLeft.HasValue)
                    {
                        return DaysLeft.Value switch
                        {
                            0 => "Истекает сегодня",
                            1 => "Остался 1 день",
                            _ => $"Осталось {DaysLeft} дней"
                        };
                    }

                    return $"Действительно до {ExpiresAtFormatted}";
                }
            }

            /// <summary>
            /// Заголовок страницы в зависимости от состояния
            /// </summary>
            public string PageTitle
            {
                get
                {
                    if (IsExistingUser)
                        return "Присоединение к компании";

                    if (IsExpired)
                        return "Приглашение просрочено";

                    return "Принять приглашение";
                }
            }

            /// <summary>
            /// Текст кнопки отправки формы
            /// </summary>
            public string SubmitButtonText
            {
                get
                {
                    if (IsExistingUser)
                        return "Присоединиться к компании";

                    return "Принять приглашение";
                }
            }

            /// <summary>
            /// Нужно ли показывать поля пароля
            /// </summary>
            public bool ShowPasswordFields => !IsExistingUser;

            /// <summary>
            /// Нужно ли показывать поле DisplayName
            /// </summary>
            public bool ShowDisplayNameField => !IsExistingUser || string.IsNullOrEmpty(DisplayName);

            /// <summary>
            /// Получить предполагаемое имя из email
            /// </summary>
            public string GetSuggestedDisplayName()
            {
                if (!string.IsNullOrEmpty(DisplayName))
                    return DisplayName;

                if (!string.IsNullOrEmpty(Email))
                {
                    var parts = Email.Split('@');
                    if (parts.Length > 0)
                    {
                        // Преобразуем "ivan.petrov" в "Иван Петров"
                        var name = parts[0].Replace(".", " ").Replace("_", " ");
                        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
                    }
                }

                return string.Empty;
            }
        }

}
