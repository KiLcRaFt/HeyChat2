using HeyChat2.Models;
using Microsoft.EntityFrameworkCore;
using PusherServer;

var builder = WebApplication.CreateBuilder(args);

// ��������� ������ �����������
var connectionString = builder.Configuration.GetConnectionString("ChatConnection");

// ���������� DbContext
builder.Services.AddDbContext<ChatContext>(options =>
    options.UseSqlServer(connectionString));

// ����������� Pusher ��� �������
builder.Services.AddSingleton<Pusher>(sp =>
{
    var options = new PusherOptions { Cluster = "eu" };
    return new Pusher("1887209", "c38879c0eb230cbf0252", "ca21d54b92aea3d5b31f", options);
});

// ���������� ������������ � ���������������
builder.Services.AddControllersWithViews();

// ��������� ������
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // ���������� ����� ����� ������
    options.Cookie.HttpOnly = true; // �������������� ������� � ���� ����� JavaScript
});

// ���������� ����������
var app = builder.Build();

// ��������� HTTP-�������� � ������ middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession(); // ��������� ������
app.UseAuthorization();

// ��������� ���������
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

// ������ ����������
app.Run();
