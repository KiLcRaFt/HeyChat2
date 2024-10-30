using Microsoft.AspNetCore.Mvc;
using HeyChat2.Models;
using PusherServer;

namespace HeyChat2.Controllers
{
    public class ChatController : Controller
    {
        private readonly ChatContext _context; // Храните контекст базы данных
        private readonly Pusher _pusher;

        // Конструктор класса с Dependency Injection
        public ChatController(ChatContext context)
        {
            _context = context;
            var options = new PusherOptions { Cluster = "eu" };
            _pusher = new Pusher("1887209", "c38879c0eb230cbf0252", "ca21d54b92aea3d5b31f", options);
        }

        public ActionResult Index()
        {
            // Получаем Id пользователя из сессии
            var userId = HttpContext.Session.GetInt32("userId");

            if (!userId.HasValue)
            {
                return Redirect("/");
            }

            var currentUser = _context.Users.Find(userId.Value); // Находим пользователя по Id

            ViewBag.allUsers = _context.Users
                .Where(u => u.name != currentUser.name)
                .ToList();

            ViewBag.currentUser = currentUser;

            return View();
        }

        public JsonResult ConversationWithContact(int contact)
        {
            var userId = HttpContext.Session.GetInt32("userId");

            if (!userId.HasValue)
            {
                return Json(new { status = "error", message = "User is not logged in" });
            }

            List<Conversation> conversations = _context.Conversations
                .Where(c => (c.receiver_id == userId.Value && c.sender_id == contact) ||
                            (c.receiver_id == contact && c.sender_id == userId.Value))
                .OrderBy(c => c.created_at)
                .ToList();

            return Json(new { status = "success", data = conversations });
        }

        [HttpPost]
        public JsonResult SendMessage()
        {
            var userId = HttpContext.Session.GetInt32("userId");

            if (!userId.HasValue)
            {
                return Json(new { status = "error", message = "User is not logged in" });
            }

            string socket_id = Request.Form["socket_id"];
            int contactId = Convert.ToInt32(Request.Form["contact"]);

            var convo = new Conversation
            {
                sender_id = userId.Value,
                message = Request.Form["message"],
                receiver_id = contactId
            };

            _context.Conversations.Add(convo);
            _context.SaveChanges();

            var conversationChannel = GetConvoChannel(userId.Value, contactId);

            // Отправка сообщения через Pusher
            _pusher.TriggerAsync(
                conversationChannel,
                "new_message",
                convo,
                new TriggerOptions { SocketId = socket_id }
            );

            return Json(convo);
        }

        private string GetConvoChannel(int user_id, int contact_id)
        {
            return user_id > contact_id
                ? $"private-chat-{contact_id}-{user_id}"
                : $"private-chat-{user_id}-{contact_id}";
        }
    }
}
