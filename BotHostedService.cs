using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace TelegramBot;

public class BotHostedService(
    ILogger<BotHostedService> logger,
    ITelegramBotClient botClient,
    IUpdateHandler updateHandler
    ) : IHostedService
{
    private CancellationTokenSource? _cts;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        Task.Run(async () =>
        {
            var me = await botClient.GetMe(_cts.Token);
            logger.LogInformation("🎉 {bot} has started successfully.", $"{me.FirstName} - {me.Username}");

            await botClient.ReceiveAsync(
                updateHandler,
                new ReceiverOptions
                {
                    DropPendingUpdates = true,
                    AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
                },
                cancellationToken: _cts.Token);
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("{service} is exiting...", nameof(BotHostedService));
        _cts?.Cancel();
        return Task.CompletedTask;
    }
}
