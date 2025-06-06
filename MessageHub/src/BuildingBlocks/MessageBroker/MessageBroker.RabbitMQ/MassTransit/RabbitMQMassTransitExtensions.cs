﻿using MassTransit;
using MessageBroker.Common.Interfaces;
using MessageBroker.Common.Models;
using MessageBroker.Common.Models.MessageBroker.Common.Models;
using MessageBroker.Common.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace MessageBroker.RabbitMQ.MassTransit
{
    public static class RabbitMQMassTransitExtensions
    {
        public static IServiceCollection AddMassTransitWithRabbitMq(this IServiceCollection services, IConfiguration configuration)
        {
            // Register MassTransit with RabbitMQ
            services.AddMassTransit(x =>
            {
                // Add consumers here
                x.AddConsumers(AppDomain.CurrentDomain.GetAssemblies());

                x.UsingRabbitMq((context, cfg) =>
                {
                    var rabbitMqConfig = configuration.GetSection("MessageBroker:RabbitMQ");
                    var host = rabbitMqConfig["Host"];
                    var username = rabbitMqConfig["Username"];
                    var password = rabbitMqConfig["Password"];
                    var virtualHost = rabbitMqConfig["VirtualHost"];

                    cfg.Host(host, virtualHost, h =>
                    {
                        h.Username(username);
                        h.Password(password);
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });

            // Register MongoDB Outbox Store
            services.AddSingleton<IOutboxStore>(provider =>
            {
                var mongoConnectionString = configuration.GetConnectionString("MongoDb");
                if (string.IsNullOrEmpty(mongoConnectionString))
                {
                    throw new InvalidOperationException("MongoDB connection string is missing in configuration.");
                }

                var mongoClient = new MongoClient(mongoConnectionString);
                var dbName = configuration["MessageBroker:MongoDbName"] ?? "OutboxDb";
                var collectionName = configuration["MessageBroker:OutboxCollection"] ?? "Outbox";
                return new MongoOutboxStore(mongoClient, dbName, collectionName);
            });

            // Register Message Broker with Outbox Pattern
            services.AddSingleton<IMessageBroker, RabbitMQMassTransitMessageBroker>();

            return services;
        }
    }

    public class RabbitMQMassTransitMessageBroker : IMessageBroker
    {
        private readonly IBus _bus;
        private readonly IOutboxStore _outboxStore;

        public RabbitMQMassTransitMessageBroker(IBus bus, IOutboxStore outboxStore)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _outboxStore = outboxStore ?? throw new ArgumentNullException(nameof(outboxStore));
        }

        public async Task PublishAsync<T>(T message, string topic = null) where T : class
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            // Save message to outbox
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
                // Publish the message to RabbitMQ
                if (string.IsNullOrEmpty(topic))
                {
                    await _bus.Publish(message);
                }
                else
                {
                    var endpoint = await _bus.GetSendEndpoint(new Uri($"queue:{topic}"));
                    await endpoint.Send(message);
                }

                // Mark message as processed in outbox
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

        public MongoOutboxStore(MongoClient mongoClient, string databaseName, string collectionName)
        {
            if (mongoClient == null)
                throw new ArgumentNullException(nameof(mongoClient));
            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentNullException(nameof(databaseName));
            if (string.IsNullOrEmpty(collectionName))
                throw new ArgumentNullException(nameof(collectionName));

            var database = mongoClient.GetDatabase(databaseName);
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
}