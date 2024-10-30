using HeyChat2.Models;
using Microsoft.EntityFrameworkCore;
using PusherServer;

var builder = WebApplication.CreateBuilder(args);

// Получение строки подключения
var connectionString = builder.Configuration.GetConnectionString("ChatConnection");

// Добавление DbContext
builder.Services.AddDbContext<ChatContext>(options =>
    options.UseSqlServer(connectionString));

// Регистрация Pusher как сервиса
builder.Services.AddSingleton<Pusher>(sp =>
{
    var options = new PusherOptions { Cluster = "eu" };
    return new Pusher("1887209", "c38879c0eb230cbf0252", "ca21d54b92aea3d5b31f", options);
});

// Добавление контроллеров с представлениями
builder.Services.AddControllersWithViews();

// Настройка сессий
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Установите время жизни сессии
    options.Cookie.HttpOnly = true; // Предотвращение доступа к куки через JavaScript
});

// Построение приложения
var app = builder.Build();

// Настройка HTTP-запросов и других middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession(); // Включение сессий
app.UseAuthorization();

// Настройка маршрутов
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "Login",
    pattern: "login",
    defaults: new { controller = "Auth", action = "Login" }
);

app.MapControllerRoute(
    name: "ChatRoom",
    pattern: "chat",
    defaults: new { controller = "Chat", action = "Index" }
);

app.MapControllerRoute(
    name: "SendMessage",
    pattern: "send_message",
    defaults: new { controller = "Chat", action = "SendMessage" }
);

app.MapControllerRoute(
    name: "PusherAuth",
    pattern: "pusher/auth",
    defaults: new { controller = "Auth", action = "AuthForChannel" }
);

// Запуск приложения
app.Run();
