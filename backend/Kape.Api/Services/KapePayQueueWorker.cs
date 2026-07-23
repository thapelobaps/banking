using Kape.Api.Services.Interfaces;

namespace Kape.Api.Services;

public sealed class KapePayQueueWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<KapePayQueueWorker> logger) : BackgroundService
{
    private const string QueueName = "kape-pay";
    private readonly string _workerId = $"{Environment.MachineName}-kape-pay-{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var queue = scope.ServiceProvider.GetRequiredService<IWalletQueue>();
                var service = scope.ServiceProvider.GetRequiredService<IKapePayService>();
                var message = await queue.DequeueAsync(QueueName, _workerId, stoppingToken);
                if (message is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }

                try
                {
                    await service.ProcessQueueMessageAsync(message, stoppingToken);
                    await queue.CompleteAsync(message.Id, stoppingToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Kape Pay queue message {MessageId} failed on attempt {Attempt}",
                        message.Id,
                        message.Attempts);
                    var retryDelay = TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, message.Attempts)));
                    await queue.FailAsync(message.Id, exception.Message, retryDelay, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Kape Pay queue worker loop failed");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
