using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
namespace TelegramBot;

public class BotHostedService(
    ILogger<BotHostedService> logger,
    ITelegramBotClient botClient,
    IUpdateHandler updateHandler
    ) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var me = await botClient.GetMe(cancellationToken);
        logger.LogInformation("🎉 {bot} has started successfully.", $"{me.FirstName} - {me.Username}");

        await botClient.ReceiveAsync(
            updateHandler,
            new ReceiverOptions
            {
                DropPendingUpdates = true,
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            },
            cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("{service} is exiting...", nameof(BotHostedService));
        return Task.CompletedTask;
    }
}