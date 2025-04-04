using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Common.Outbox
{
	using MessageBroker.Common.Models;
	using MessageBroker.Common.Models.MessageBroker.Common.Models;

	public interface IOutboxStore
	{
		Task SaveMessageAsync(OutboxMessage message);
		Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync();
		Task MarkMessageAsProcessedAsync(string id);
		Task MarkMessageAsFailedAsync(string id, string errorMessage);
	}
}
