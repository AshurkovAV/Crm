namespace Crm.Services.Workspace
{
    // ВРЕМЕННАЯ ЗАГЛУШКА: всегда «Полная CRM». Реальная реализация — хранение режима у компании.
    public class WorkspaceService : IWorkspaceService
    {
        public Task<string> GetCurrentModeAsync() => Task.FromResult(Crm.Navigation.WorkspaceMode.Full);
        public Task<bool> CanChangeModeAsync() => Task.FromResult(false);
        public Task<bool> SetCurrentModeAsync(string mode) => Task.FromResult(false);
    }
}
