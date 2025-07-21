using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Polling;
using TelegramBot;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

var builder = Host.CreateApplicationBuilder(args);

// ✅ appsettings.json optional qilib qo‘yildi
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// ✅ Railway tokenni logga chiqarish (debug uchun)
Console.WriteLine($"DEBUG >> TELEGRAM_BOT_TOKEN = {Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN")}");

// ✅ Environment orqali tokenni olish
var botToken = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN")
    ?? throw new ArgumentException("Telegram Bot Token is not configured.");

// ✅ appsettings.json ichidagi "Bot" bo‘limini DI orqali uzatish
builder.Services.Configure<BotSettings>(builder.Configuration.GetSection("Bot"));

builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
builder.Services.AddHttpClient<AvatarService>();
builder.Services.AddSingleton<IUpdateHandler, BotUpdateHandler>();
builder.Services.AddHostedService<BotHostedService>();

await builder.Build().RunAsync();
