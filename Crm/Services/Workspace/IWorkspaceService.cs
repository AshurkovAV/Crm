namespace Crm.Services.Workspace
{
    /// <summary>Режим работы текущей компании пользователя (см. Crm.Navigation.WorkspaceMode).</summary>
    public interface IWorkspaceService
    {
        /// <summary>Режим текущей компании пользователя; Full, если компании нет или не авторизован.</summary>
        Task<string> GetCurrentModeAsync();

        /// <summary>Может ли текущий пользователь менять режим текущей компании (владелец или Admin).</summary>
        Task<bool> CanChangeModeAsync();

        /// <summary>Сменить режим текущей компании. false — нет прав или компании.</summary>
        Task<bool> SetCurrentModeAsync(string mode);
    }
}
