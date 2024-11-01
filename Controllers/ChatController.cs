using Microsoft.AspNetCore.Mvc;
using HeyChat2.Models;
using PusherServer;

namespace HeyChat2.Controllers
{
    public class ChatController : Controller
    {
        private readonly ChatContext _context;
        private readonly Pusher _pusher;

        // Class constructor with Dependency Injection
        public ChatController(ChatContext context)
        {
            _context = context;
            var options = new PusherOptions { Cluster = "eu" };
            _pusher = new Pusher("1887209", "c38879c0eb230cbf0252", "ca21d54b92aea3d5b31f", options);
        }

        public ActionResult Index()
        {
            // Get user Id from session
            var userId = HttpContext.Session.GetInt32("userId");

            if (!userId.HasValue)
            {
                return Redirect("/");
            }

            var currentUser = _context.Users.Find(userId.Value); // Find user by Id

            ViewBag.allUsers = _context.Users
                .Where(u => u.name != currentUser.name)
                .ToList();

            ViewBag.currentUser = currentUser;

            return View();
        }

        [HttpGet("chat/ConversationWithContact")]
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
        public async Task<JsonResult> SendMessage(string message, int contact, string socket_id)
        {
            var userId = HttpContext.Session.GetInt32("userId");

            if (!userId.HasValue)
            {
                return Json(new { status = "error", message = "User is not logged in" });
            }

            // Creating a new message
            var newConversation = new Conversation
            {
                sender_id = userId.Value,
                receiver_id = contact,
                message = message,
                created_at = DateTime.Now,
                status = Conversation.messageStatus.Sent 
            };

            try
            {
                _context.Conversations.Add(newConversation);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving message: {ex.Message}");
                return Json(new { status = "error", message = "Failed to send message." });
            }

            var options = new PusherOptions
            {
                Cluster = "eu"
            };

            var pusher = new Pusher(
                "1887209",
                "c38879c0eb230cbf0252",
                "ca21d54b92aea3d5b31f",
                options
            );

            var eventData = new
            {
                id = newConversation.id,
                sender_id = newConversation.sender_id,
                receiver_id = newConversation.receiver_id,
                message = newConversation.message,
                created_at = newConversation.created_at.ToString("yyyy-MM-dd HH:mm:ss"),
                status = newConversation.status.ToString()
            };

            try
            {
                // Sending an event via Pusher
                await pusher.TriggerAsync(
                    $"private-chat-{Math.Min(userId.Value, contact)}-{Math.Max(userId.Value, contact)}",
                    "new_message",
                    eventData,
                    new TriggerOptions { SocketId = socket_id }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error triggering Pusher event: {ex.Message}");
                return Json(new { status = "error", message = "Failed to notify recipient." });
            }

            return Json(new { status = "success", data = eventData });
        }


        private string GetConvoChannel(int user_id, int contact_id)
        {
            return user_id > contact_id
                ? $"private-chat-{contact_id}-{user_id}"
                : $"private-chat-{user_id}-{contact_id}";
        }
    }
}
