using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBot;

public class BotUpdateHandler : IUpdateHandler
{
    private readonly ILogger<BotUpdateHandler> logger;
    private readonly AvatarService avatarService;

    private readonly Dictionary<string, string> supported = new()
    {
        ["/fun-emoji"] = "fun-emoji",
        ["/avataaars"] = "avataaars",
        ["/bottts"] = "bottts",
        ["/pixel-art"] = "pixel-art"
    };

    public BotUpdateHandler(ILogger<BotUpdateHandler> logger, AvatarService avatarService)
    {
        this.logger = logger;
        this.avatarService = avatarService;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message?.Text is not { } text)
            return;

        var chatId = update.Message.Chat.Id;

        if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            await botClient.SendMessage(
                chatId,
                "👋 Salom! Men Dicebear avatar botman.\n/help orqali buyruqlarni ko‘rib chiqing.",
                cancellationToken: cancellationToken);
            return;
        }

        if (text.Equals("/help", StringComparison.OrdinalIgnoreCase))
        {
            var msg = "**Buyruqlar:**\n" +
                      string.Join("\n", supported.Select(kvp => $"{kvp.Key} <ism> — `{kvp.Value}`"));
            await botClient.SendMessage(chatId, msg, cancellationToken: cancellationToken);
            return;
        }

        var cmd = supported.Keys.FirstOrDefault(text.StartsWith);
        if (cmd is null) return;

        var parts = text.Split(' ', 2);
        if (parts.Length < 2)
        {
            await botClient.SendMessage(chatId, $"❗️Buyruqni to'liq kiriting! Masalan: `{cmd} John`", cancellationToken: cancellationToken);
            return;
        }

        var style = supported[cmd];
        var seed = parts[1];

        try
        {
            using var st = await avatarService.GetAvatarAsync(style, seed, cancellationToken);
            var inputFile = new InputFileStream(st, $"{seed}.png");
            await botClient.SendPhoto(chatId, inputFile, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Xatolik: {msg}", ex.Message);
            await botClient.SendMessage(chatId, "❌ Xatolik yuz berdi", cancellationToken: cancellationToken);
        }
    }

    public Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Botda xatolik: {Message}", exception.Message);
        return Task.CompletedTask;
    }
}
