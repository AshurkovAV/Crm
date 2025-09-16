using Microsoft.AspNetCore.Mvc;

namespace Crm.ViewComponents.Chat
{
    public class ChatViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var chats = new List<ChatModel>
        {
            new ChatModel { Id = 1, Name = "Иван Петров", LastMessage = "Привет! Как дела?", Time = "12:30", Unread = 3, IsOnline = true },
            new ChatModel { Id = 2, Name = "Мария Сидорова", LastMessage = "Отправила документы", Time = "11:45", Unread = 0, IsOnline = false },
            new ChatModel { Id = 3, Name = "ООО Ромашка", LastMessage = "Обсудить проект", Time = "10:20", Unread = 1, IsOnline = true },
            new ChatModel { Id = 4, Name = "Техподдержка", LastMessage = "Ваша заявка обработана", Time = "09:15", Unread = 0, IsOnline = false }
        };

            return View(chats);
        }
    }

    public class ChatModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LastMessage { get; set; }
        public string Time { get; set; }
        public int Unread { get; set; }
        public bool IsOnline { get; set; }
    }
}
