namespace Crm.Models
{
    /// <summary>
    /// Состояния пользователя при обработке приглашения
    /// </summary>
    public enum UserState
    {
        /// <summary>
        /// Пользователь не зарегистрирован в системе
        /// </summary>
        NotRegistered = 0,

        /// <summary>
        /// Пользователь зарегистрирован, но НЕ авторизован (не вошёл в систему)
        /// </summary>
        RegisteredNotAuthenticated = 1,

        /// <summary>
        /// Пользователь зарегистрирован И авторизован (вошёл в систему)
        /// </summary>
        RegisteredAndAuthenticated = 2,

        /// <summary>
        /// Пользователь авторизован, но под другим аккаунтом (email не совпадает с приглашением)
        /// </summary>
        AuthenticatedMismatch = 3
    }
}
