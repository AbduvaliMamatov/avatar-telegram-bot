using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Polling;
using TelegramBot;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

var builder = Host.CreateApplicationBuilder(args);


builder.Configuration.AddUserSecrets<Program>();

builder.Services.Configure<BotSettings>(builder.Configuration.GetSection("Bot"));

var botToken = builder.Configuration["Bot:Token"]
    ?? throw new ArgumentException("Telegram Bot Token is not configured.");

builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

builder.Services.AddHttpClient<AvatarService>();

builder.Services.AddSingleton<IUpdateHandler, BotUpdateHandler>();

builder.Services.AddHostedService<BotHostedService>();

await builder.Build().RunAsync();