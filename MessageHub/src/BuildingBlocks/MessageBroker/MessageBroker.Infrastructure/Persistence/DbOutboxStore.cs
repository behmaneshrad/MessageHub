namespace MessageBroker.Infrastructure.Persistence
{
	using MessageBroker.Common.Models;
	using MessageBroker.Common.Models.MessageBroker.Common.Models;
	using MessageBroker.Common.Outbox;
	using Microsoft.EntityFrameworkCore;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;

	public class DbOutboxStore : IOutboxStore
	{
		private readonly DbContext _dbContext;
		private readonly DbSet<OutboxMessage> _messages;

		public DbOutboxStore(DbContext dbContext)
		{
			_dbContext = dbContext;
			_messages = dbContext.Set<OutboxMessage>();
		}

		public async Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync()
		{
			return await _messages
				.Where(m => m.Status == "Pending")
				.OrderBy(m => m.CreatedDate)
				.Take(50) // Process in batches
				.ToListAsync();
		}

		public async Task MarkMessageAsFailedAsync(string id, string errorMessage)
		{
			var message = await _messages.FindAsync(id);
			if (message != null)
			{
				message.Status = "Failed";
				message.ErrorMessage = errorMessage;
				await _dbContext.SaveChangesAsync();
			}
		}

		public async Task MarkMessageAsProcessedAsync(string id)
		{
			var message = await _messages.FindAsync(id);
			if (message != null)
			{
				message.Status = "Processed";
				message.ProcessedDate = DateTime.UtcNow;
				await _dbContext.SaveChangesAsync();
			}
		}

		public async Task SaveMessageAsync(OutboxMessage message)
		{
			await _messages.AddAsync(message);
			await _dbContext.SaveChangesAsync();
		}
	}
}