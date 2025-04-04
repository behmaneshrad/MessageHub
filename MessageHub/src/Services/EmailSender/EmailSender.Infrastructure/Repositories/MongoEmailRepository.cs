using EmailSender.Domain.Entities;
using EmailSender.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace EmailSender.Infrastructure.Repositories
{
	public class MongoEmailRepository : IEmailRepository
	{
		private readonly IMongoCollection<EmailMessage> _emailCollection;

		public MongoEmailRepository(IConfiguration configuration)
		{
			var client = new MongoClient(configuration.GetConnectionString("MongoDb"));
			var database = client.GetDatabase("EmailSenderDb");
			_emailCollection = database.GetCollection<EmailMessage>("Emails");
		}

		public async Task SaveEmailAsync(EmailMessage email)
		{
			await _emailCollection.InsertOneAsync(email);
		}

		public async Task<EmailMessage> GetEmailByIdAsync(string id)
		{
			return await _emailCollection.Find(e => e.Id == id).FirstOrDefaultAsync();
		}

		public async Task UpdateEmailStatusAsync(string id, bool isSent, string errorMessage = null)
		{
			var update = Builders<EmailMessage>.Update
				.Set(e => e.IsSent, isSent)
				.Set(e => e.SentDate, isSent ? DateTime.UtcNow : (DateTime?)null)
				.Set(e => e.ErrorMessage, errorMessage);

			await _emailCollection.UpdateOneAsync(e => e.Id == id, update);
		}
	}
}