using EmailReceiver.Domain.Interfaces;
using EmailReceiver.Infrastructure.Repositories;
using MessageBroker.Common.Interfaces;
using Microsoft.OpenApi.Models;
using System.Reflection;
using MessageBroker.Kafka.MassTransit;
using MessageBroker.Kafka.CAP;
using MessageBroker.RabbitMQ.MassTransit;
using MessageBroker.RabbitMQ.CAP;
using RabbitMQ.Client;
using MassTransit;
using MassTransit.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "EmailReceiver.API", Version = "v1" });
});

// Register MongoDB Repository
builder.Services.AddSingleton<IEmailRepository, MongoEmailRepository>();

// Register Message Broker based on configuration
var messageBrokerType = builder.Configuration["MessageBroker:Type"]; // "RabbitMQ" or "Kafka"
var messageBrokerLibrary = builder.Configuration["MessageBroker:Library"]; // "MassTransit" or "CAP"

if (messageBrokerType == "RabbitMQ")
{
    if (messageBrokerLibrary == "MassTransit")
    {
        builder.Services.AddMassTransitWithRabbitMq(builder.Configuration, cfg =>
        {
            cfg.AddDelayedMessageScheduler();           
        });
        
    }
    else if (messageBrokerLibrary == "CAP")
    {
        builder.Services.AddCAPWithRabbitMq(builder.Configuration);
    }
}
else if (messageBrokerType == "Kafka")
{
    if (messageBrokerLibrary == "MassTransit")
    {
        builder.Services.AddMassTransitWithKafka(builder.Configuration);
    }
    else if (messageBrokerLibrary == "CAP")
    {
        builder.Services.AddCAPWithKafka(builder.Configuration);
    }
}

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "EmailReceiver.API v1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();