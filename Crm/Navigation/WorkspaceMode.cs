namespace Crm.Navigation
{
    /// <summary>
    /// Режим работы компании (учреждения). Хранится у компании, у каждой свой.
    /// Full  — полная CRM (продажи, склад, производство, задачи, команда).
    /// Tasks — только постановка задач: задачи, проекты, команда и пароли.
    /// </summary>
    public static class WorkspaceMode
    {
        public const string Full = "Full";
        public const string Tasks = "Tasks";

        public static bool IsValid(string? mode) => mode == Full || mode == Tasks;

        public static string Normalize(string? mode) => mode == Tasks ? Tasks : Full;
    }
}
