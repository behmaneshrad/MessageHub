namespace MessageBroker.Common.Outbox
{
	using MessageBroker.Common.Interfaces;
	using MessageBroker.Common.Models;
	using Microsoft.Extensions.DependencyInjection;
	using Microsoft.Extensions.Hosting;
	using Microsoft.Extensions.Logging;
	using System;
	using System.Text.Json;
	using System.Threading;
	using System.Threading.Tasks;

	public class OutboxProcessor : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<OutboxProcessor> _logger;
		private readonly TimeSpan _processInterval = TimeSpan.FromSeconds(10);

		public OutboxProcessor(
			IServiceProvider serviceProvider,
			ILogger<OutboxProcessor> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Outbox processor started");

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					await ProcessOutboxMessagesAsync();
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error processing outbox messages");
				}

				await Task.Delay(_processInterval, stoppingToken);
			}

			_logger.LogInformation("Outbox processor stopped");
		}

		private async Task ProcessOutboxMessagesAsync()
		{
			using var scope = _serviceProvider.CreateScope();
			var outboxStore = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
			var messageBroker = scope.ServiceProvider.GetRequiredService<IMessageBroker>();

			var pendingMessages = await outboxStore.GetPendingMessagesAsync();

			foreach (var message in pendingMessages)
			{
				try
				{
					// We need to get the type from the MessageType string
					Type messageType = Type.GetType(message.MessageType);
					if (messageType == null)
					{
						_logger.LogError("Could not find type {MessageType}", message.MessageType);
						await outboxStore.MarkMessageAsFailedAsync(message.Id, $"Unknown message type: {message.MessageType}");
						continue;
					}

					// Deserialize the JSON to the correct type
					var deserializedMessage = JsonSerializer.Deserialize(message.MessageJson, messageType);

					// Use reflection to call PublishAsync with the correct type
					var publishMethod = typeof(IMessageBroker).GetMethod("PublishAsync");
					var genericPublishMethod = publishMethod.MakeGenericMethod(messageType);

					// Invoke the publish method
					await (Task)genericPublishMethod.Invoke(messageBroker, new[] { deserializedMessage, message.Topic });

					// Mark as processed
					await outboxStore.MarkMessageAsProcessedAsync(message.Id);
					_logger.LogInformation("Successfully processed outbox message: {MessageId}", message.Id);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
					await outboxStore.MarkMessageAsFailedAsync(message.Id, ex.Message);
				}
			}
		}
	}
}