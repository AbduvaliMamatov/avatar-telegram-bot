using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Polling;
using TelegramBot;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.Configuration.UserSecrets; // ❌ Endi kerak emas

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
// builder.Configuration.AddUserSecrets<Program>(); // ❌ Endi kerak emas

// ⬇️ Tokenni .env yoki launchSettings.json dan olib o'qish
var botToken = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN")
    ?? throw new ArgumentException("Telegram Bot Token is not configured.");

// Konfiguratsiyadagi boshqa "Bot" bo'limini o'qish
builder.Services.Configure<BotSettings>(builder.Configuration.GetSection("Bot"));

builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
builder.Services.AddHttpClient<AvatarService>();
builder.Services.AddSingleton<IUpdateHandler, BotUpdateHandler>();
builder.Services.AddHostedService<BotHostedService>();

await builder.Build().RunAsync();
