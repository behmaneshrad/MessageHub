namespace MessageBroker.Infrastructure
{
	using MessageBroker.Common.Interfaces;
	using MessageBroker.Common.Models;
	using MessageBroker.Common.Models.MessageBroker.Common.Models;
	using MessageBroker.Common.Outbox;
	using System;
	using System.Text.Json;
	using System.Threading.Tasks;

	public class OutboxMessageBroker : IMessageBroker
	{
		private readonly IOutboxStore _outboxStore;

		public OutboxMessageBroker(IOutboxStore outboxStore)
		{
			_outboxStore = outboxStore;
		}

		public async Task PublishAsync<T>(T message, string topic = null) where T : class
		{
			var outboxMessage = new OutboxMessage
			{
				Id = Guid.NewGuid().ToString(),
				Topic = topic ?? typeof(T).Name,
				MessageType = typeof(T).AssemblyQualifiedName,
				MessageJson = JsonSerializer.Serialize(message),
				CreatedDate = DateTime.UtcNow,
				Status = "Pending"
			};

			await _outboxStore.SaveMessageAsync(outboxMessage);
		}
	}
}