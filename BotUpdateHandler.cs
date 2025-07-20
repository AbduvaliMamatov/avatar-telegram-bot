using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot;

public class BotUpdateHandler : IUpdateHandler
{
    private readonly ILogger<BotUpdateHandler> logger;
    private readonly AvatarService avatarService;
    private readonly Dictionary<string, string> _supportedCommands;
    private readonly Dictionary<long, UserSession> _sessions = new();
    private readonly HashSet<string> _cssColors = new(StringComparer.OrdinalIgnoreCase)
    {
        // ... barcha ranglar
        "red","green","blue","black","white","gray","yellow","purple","orange","pink","brown","cyan","magenta"
        // va boshqalar
    };

    public BotUpdateHandler(
        ILogger<BotUpdateHandler> logger,
        AvatarService avatarService,
        IOptions<BotSettings> options)
    {
        this.logger = logger;
        this.avatarService = avatarService;
        this._supportedCommands = options.Value.SupportedCommands;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var chatId = update.CallbackQuery?.Message?.Chat?.Id ?? update.Message?.Chat?.Id ?? 0;
        if (chatId == 0) return;

        if (!_sessions.ContainsKey(chatId))
            _sessions[chatId] = new UserSession();

        var session = _sessions[chatId];

        // Callback tugmalar uchun ishlov
        if (update.CallbackQuery?.Data is string data)
        {
            if (_supportedCommands.ContainsKey(data))
            {
                session.Command = data;
                session.Style = _supportedCommands[data];

                await RemoveInlineKeyboardMessageAsync(botClient, update.CallbackQuery, cancellationToken);

                var buttons = new[]
                {
                    new[] {
                        InlineKeyboardButton.WithCallbackData("🖼 PNG", "format|png"),
                        InlineKeyboardButton.WithCallbackData("📄 SVG", "format|svg")
                    }
                };
                await botClient.SendMessage(chatId, $"✅ {session.Style} formatini tanlang:", replyMarkup: new InlineKeyboardMarkup(buttons));
                return;
            }

            if (data.StartsWith("format|"))
            {
                session.Format = data.Split('|')[1];

                await RemoveInlineKeyboardMessageAsync(botClient, update.CallbackQuery, cancellationToken);

                var buttons = new[]
                {
                    new[] {
                        InlineKeyboardButton.WithCallbackData("🔳 Transparent", "bg|transparent"),
                        InlineKeyboardButton.WithCallbackData("🟥 Solid", "bg|solid")
                    }
                };
                await botClient.SendMessage(chatId, "Fon qanday bo‘lsin?", replyMarkup: new InlineKeyboardMarkup(buttons));
                return;
            }

            if (data.StartsWith("bg|"))
            {
                session.Background = data.Split('|')[1];

                await RemoveInlineKeyboardMessageAsync(botClient, update.CallbackQuery, cancellationToken);

                if (session.Background == "transparent")
                {
                    session.Stage = Stage.Seed;
                    await botClient.SendMessage(chatId, "Seed kiriting:");
                }
                else
                {
                    session.Stage = Stage.Color;
                    await botClient.SendMessage(chatId, "Rang kiriting (masalan red, blue):");
                }
                return;
            }
        }

        // Faqat text mavjud bo‘lsa davom etamiz
        if (!string.IsNullOrWhiteSpace(update.Message?.Text))
        {
            var text = update.Message.Text;

            if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
            {
                await botClient.SendMessage(chatId, "👋 Salom! /help orqali boshlang.");
                return;
            }

            if (text.Equals("/help", StringComparison.OrdinalIgnoreCase))
            {
                var keyboardButtons = _supportedCommands.Select(kvp =>
                    new[] { InlineKeyboardButton.WithCallbackData($"{kvp.Key} — {kvp.Value}", kvp.Key) });
                await botClient.SendMessage(chatId, "Buyruqni tanlang:", replyMarkup: new InlineKeyboardMarkup(keyboardButtons));
                return;
            }

            // Foydalanuvchi Color bosqichidami
            if (session.Stage == Stage.Color)
            {
                if (!_cssColors.Contains(text))
                {
                    await botClient.SendMessage(chatId, "Noto‘g‘ri rang, qayta kiriting:");
                    return;
                }

                session.Color = text;
                session.Stage = Stage.Seed;
                await botClient.SendMessage(chatId, "Seed kiriting:");
                return;
            }

            // Foydalanuvchi Seed bosqichidami
            if (session.Stage == Stage.Seed)
            {
                session.Seed = text;

                if (session.Background == "solid" && !string.IsNullOrWhiteSpace(session.Color))
                {
                    session.Color = ConvertColorNameToHex(session.Color);
                }

                logger.LogInformation("Avatar so‘rovi: style={Style}, seed={Seed}, format={Format}, background={Bg}, color={Color}",
                    session.Style, session.Seed, session.Format, session.Background, session.Color);

                if (string.IsNullOrWhiteSpace(session.Style) ||
                    string.IsNullOrWhiteSpace(session.Format) ||
                    string.IsNullOrWhiteSpace(session.Seed))
                {
                    await botClient.SendMessage(chatId, "❌ Ma'lumotlar yetarli emas. Iltimos, /start dan qayta boshlang.");
                    _sessions.Remove(chatId);
                    return;
                }

                try
                {
                    using var originalStream = await avatarService.GetAvatarAsync(
                        session.Style,
                        session.Seed,
                        session.Format,
                        session.Background?.Equals("solid", StringComparison.OrdinalIgnoreCase) == true
                            ? session.Color
                            : null,
                        cancellationToken);

                    using var memoryStream = new MemoryStream();
                    await originalStream.CopyToAsync(memoryStream, cancellationToken);
                    memoryStream.Position = 0;

                    if (session.Format == "svg")
                    {
                        var inputFile = new InputFileStream(memoryStream, $"{session.Seed}.svg");
                        await botClient.SendDocument(chatId, inputFile, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        var inputFile = new InputFileStream(memoryStream, $"{session.Seed}.{session.Format}");
                        await botClient.SendPhoto(chatId, inputFile, cancellationToken: cancellationToken);
                    }

                    var keyboardButtons = _supportedCommands.Select(kvp =>
                    new[] { InlineKeyboardButton.WithCallbackData($"{kvp.Key} — {kvp.Value}", kvp.Key) });

                    await botClient.SendMessage(chatId, "Yana buyruq tanlang:", replyMarkup: new InlineKeyboardMarkup(keyboardButtons));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Xatolik: {msg}", ex.Message);
                    await botClient.SendMessage(chatId, "❌ Xatolik yuz berdi", cancellationToken: cancellationToken);
                }

                _sessions.Remove(chatId);
            }
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Bot xatosi: {Message}", exception.Message);
        return Task.CompletedTask;
    }

    private static async Task RemoveInlineKeyboardMessageAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        if (callbackQuery.Message is { } message)
        {
            await botClient.EditMessageReplyMarkup(
                chatId: message.Chat.Id,
                messageId: message.MessageId,
                replyMarkup: null,
                cancellationToken: cancellationToken);

            await botClient.DeleteMessage(
                chatId: message.Chat.Id,
                messageId: message.MessageId,
                cancellationToken: cancellationToken);
        }
    }

    private class UserSession
    {
        public string? Command { get; set; }
        public string? Style { get; set; }
        public string? Format { get; set; }
        public string? Background { get; set; }
        public string? Color { get; set; }
        public string? Seed { get; set; }
        public Stage Stage { get; set; } = Stage.None;
    }

    private enum Stage { None, Color, Seed }

    private static string? ConvertColorNameToHex(string? colorName)
    {
        return colorName?.ToLower() switch
        {
            "red" => "FF0000",
            "green" => "00FF00",
            "blue" => "0000FF",
            "black" => "000000",
            "white" => "FFFFFF",
            "gray" => "808080",
            "yellow" => "FFFF00",
            "purple" => "800080",
            "orange" => "FFA500",
            "pink" => "FFC0CB",
            "brown" => "A52A2A",
            "cyan" => "00FFFF",
            "magenta" => "FF00FF",
            _ => null
        };
    }

}
