using MassTransit;
using EmailSender.Domain.Events;
using EmailSender.Application.IntegrationEvents;

namespace EmailSender.API.Consumers
{
    public class EmailMessageCreatedConsumer : IConsumer<EmailMessageCreated>
    {
        public async Task Consume(ConsumeContext<EmailMessageCreated> context)
        {
            var message = context.Message;
            // Add your email processing logic here
            await Task.CompletedTask;
        }

        internal async Task ConsumeEmailCreatedEvent(EmailMessageCreatedEvent emailEvent)
        {
            throw new NotImplementedException();
        }
    }
}