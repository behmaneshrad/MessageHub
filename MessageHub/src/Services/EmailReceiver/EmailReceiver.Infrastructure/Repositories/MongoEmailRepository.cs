using EmailReceiver.Domain.Entities;
using EmailReceiver.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;

namespace EmailReceiver.Infrastructure.Repositories
{
	public class MongoEmailRepository : IEmailRepository
	{
		private readonly IMongoCollection<EmailMessage> _emailCollection;

		public MongoEmailRepository(IConfiguration configuration)
		{
			var client = new MongoClient(configuration.GetConnectionString("MongoDb"));
			var database = client.GetDatabase("MessageHub");
			_emailCollection = database.GetCollection<EmailMessage>("Emails");
		}

		public async Task<string> SaveEmailAsync(EmailMessage email)
		{
			email.Id = Guid.NewGuid().ToString();
			email.CreatedDate = DateTime.UtcNow;
			email.IsSent = false;

			await _emailCollection.InsertOneAsync(email);
			return email.Id;
		}

		public async Task<EmailMessage> GetEmailByIdAsync(string id)
		{
			return await _emailCollection.Find(e => e.Id == id).FirstOrDefaultAsync();
		}

		public async Task<IEnumerable<EmailMessage>> GetPendingEmailsAsync()
		{
			return await _emailCollection.Find(e => !e.IsSent).ToListAsync();
		}

		public async Task UpdateEmailStatusAsync(string id, bool isSent)
		{
			var update = Builders<EmailMessage>.Update.Set(e => e.IsSent, isSent);
			await _emailCollection.UpdateOneAsync(e => e.Id == id, update);
		}
	}
}
