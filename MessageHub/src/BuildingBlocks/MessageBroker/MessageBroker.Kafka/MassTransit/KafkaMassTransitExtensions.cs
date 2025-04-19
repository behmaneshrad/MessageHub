using MassTransit;
using MessageBroker.Common.Interfaces;
using MessageBroker.Common.Models;
using MessageBroker.Common.Models.MessageBroker.Common.Models;
using MessageBroker.Common.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.Kafka.MassTransit
{
	public static class KafkaMassTransitExtensions
	{
		public static IServiceCollection AddMassTransitWithKafka(this IServiceCollection services, IConfiguration configuration)
		{
			services.AddMassTransit(x =>
			{
				x.AddConsumers(AppDomain.CurrentDomain.GetAssemblies());
				x.SetKebabCaseEndpointNameFormatter();

                x.UsingInMemory((context, cfg) =>
				{
					cfg.UseConcurrencyLimit(10);
					cfg.ConfigureEndpoints(context);
				});

				x.AddRider(rider =>
				{
					var kafkaConfig = configuration.GetSection("MessageBroker:Kafka");
					var bootstrapServers = kafkaConfig["BootstrapServers"];

					rider.AddProducer<BaseMessage>("default-topic");

					rider.UsingKafka((context, k) =>
					{
						k.Host(bootstrapServers);
					});
				});
			});

			// Register MongoDB Outbox Store
			services.AddSingleton<IOutboxStore>(provider =>
			{
				var mongoClient = new MongoClient(configuration.GetConnectionString("MongoDb"));
				return new MongoOutboxStore(mongoClient, configuration["MessageBroker:OutboxCollection"] ?? "Outbox");
			});

			services.AddSingleton<IMessageBroker, KafkaMassTransitMessageBroker>();

			return services;
		}
	}

	public class KafkaMassTransitMessageBroker : IMessageBroker
	{
		private readonly ITopicProducer<BaseMessage> _producer;
		private readonly IOutboxStore _outboxStore;

		public KafkaMassTransitMessageBroker(ITopicProducer<BaseMessage> producer, IOutboxStore outboxStore)
		{
			_producer = producer;
			_outboxStore = outboxStore;
		}

		public async Task PublishAsync<T>(T message, string topic = null) where T : class
		{
			var outboxMessage = new OutboxMessage
			{
				Id = Guid.NewGuid().ToString(),
				Topic = topic ?? typeof(T).Name,
				MessageType = typeof(T).FullName,
				MessageJson = JsonSerializer.Serialize(message),
				CreatedDate = DateTime.UtcNow,
				Status = "Pending"
			};

			await _outboxStore.SaveMessageAsync(outboxMessage);

			try
			{
				await _producer.Produce(new
				{
					Topic = outboxMessage.Topic,
					MessageType = outboxMessage.MessageType,
					Payload = outboxMessage.MessageJson
				});

				await _outboxStore.MarkMessageAsProcessedAsync(outboxMessage.Id);
			}
			catch (Exception ex)
			{
				await _outboxStore.MarkMessageAsFailedAsync(outboxMessage.Id, ex.Message);
				throw;
			}
		}
	}

	public class MongoOutboxStore : IOutboxStore
	{
		private readonly IMongoCollection<OutboxMessage> _outboxCollection;

		public MongoOutboxStore(MongoClient mongoClient, string collectionName)
		{
			var database = mongoClient.GetDatabase("OutboxDb");
			_outboxCollection = database.GetCollection<OutboxMessage>(collectionName);
		}

		public async Task SaveMessageAsync(OutboxMessage message)
		{
			await _outboxCollection.InsertOneAsync(message);
		}

		public async Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync()
		{
			return await _outboxCollection.Find(m => m.Status == "Pending").ToListAsync();
		}

		public async Task MarkMessageAsProcessedAsync(string id)
		{
			var update = Builders<OutboxMessage>.Update
				.Set(m => m.Status, "Processed")
				.Set(m => m.ProcessedDate, DateTime.UtcNow);

			await _outboxCollection.UpdateOneAsync(m => m.Id == id, update);
		}

		public async Task MarkMessageAsFailedAsync(string id, string errorMessage)
		{
			var update = Builders<OutboxMessage>.Update
				.Set(m => m.Status, "Failed")
				.Set(m => m.ErrorMessage, errorMessage);

			await _outboxCollection.UpdateOneAsync(m => m.Id == id, update);
		}
	}

	public record BaseMessage
	{
		public string Topic { get; init; }
		public string MessageType { get; init; }
		public string Payload { get; init; }
	}
}