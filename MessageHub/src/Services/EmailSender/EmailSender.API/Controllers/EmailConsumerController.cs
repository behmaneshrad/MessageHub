using EmailSender.Application.IntegrationEvents;
using EmailSender.API.Consumers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace EmailSender.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailConsumerController : ControllerBase
    {
        private readonly EmailMessageCreatedConsumer _emailConsumer;
        private readonly ILogger<EmailConsumerController> _logger;

        public EmailConsumerController(
            EmailMessageCreatedConsumer emailConsumer,
            ILogger<EmailConsumerController> logger)
        {
            _emailConsumer = emailConsumer;
            _logger = logger;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessEmailEvent([FromBody] EmailMessageCreatedEvent emailEvent)
        {
            if (emailEvent == null)
                return BadRequest("Email event is required");

            try
            {
                await _emailConsumer.ConsumeEmailCreatedEvent(emailEvent);
                return Ok(new { Message = "Email event processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email event");
                return StatusCode(500, new { Error = "An error occurred while processing the email event" });
            }
        }

        [HttpGet("test/{email}")]
        public async Task<IActionResult> TestEmailConsumer(string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email is required");

            try
            {
                var testEvent = new EmailMessageCreatedEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    RecipientEmail = email,
                    Subject = "Test Email",
                    Body = "This is a test email from the consumer controller",
                    CreatedDate = DateTime.UtcNow
                };

                await _emailConsumer.ConsumeEmailCreatedEvent(testEvent);
                return Ok(new { Message = "Test email event processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing test email event");
                return StatusCode(500, new { Error = "An error occurred while processing the test email" });
            }
        }
    }
}
