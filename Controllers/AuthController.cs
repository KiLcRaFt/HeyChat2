using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using HeyChat2.Models;
using PusherServer;

namespace HeyChat2.Controllers
{
    public class AuthController : Controller
    {
        private readonly ChatContext _context;

        public AuthController(ChatContext context)
        {
            _context = context;
        }

        public ActionResult TestDatabaseConnection()
        {
            try
            {
                int userCount = _context.Users.Count();
                return Content($"Total users: {userCount}");
            }
            catch (Exception ex)
            {
                return Content("Error connecting to database: " + ex.Message);
            }
        }

        [HttpPost]
        public ActionResult Login()
        {
            string user_name = Request.Form["username"];

            if (string.IsNullOrWhiteSpace(user_name))
            {
                return Redirect("/");
            }

            // Using an already implemented database context
            User user = _context.Users.FirstOrDefault(u => u.name == user_name);

            if (user == null)
            {
                user = new User { name = user_name };
                _context.Users.Add(user);
                _context.SaveChanges();
            }

            // Save only the user Id in the session
            HttpContext.Session.SetInt32("userId", user.id);

            return Redirect("/chat");
        }

        public JsonResult AuthForChannel(string channel_name, string socket_id)
        {
            var userId = HttpContext.Session.GetInt32("userId");

            if (!userId.HasValue)
            {
                return Json(new { status = "error", message = "User is not logged in" });
            }

            var currentUser = _context.Users.Find(userId.Value);

            if (currentUser == null)
            {
                return Json(new { status = "error", message = "User not found" });
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

            if (!channel_name.Contains(currentUser.id.ToString()))
            {
                return Json(new { status = "error", message = "User cannot join channel" });
            }

            var auth = pusher.Authenticate(channel_name, socket_id);
            return Json(auth);
        }
    }
}
