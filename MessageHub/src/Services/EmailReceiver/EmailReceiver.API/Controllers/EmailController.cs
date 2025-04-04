using EmailReceiver.Application.Commands;
using EmailReceiver.Application.IntegrationEvents;
using EmailReceiver.Domain.Entities;
using EmailReceiver.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EmailReceiver.API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class EmailController : ControllerBase
	{
		private readonly IEmailRepository _emailRepository;
		private readonly IMessageBroker _messageBroker;
		private readonly ILogger<EmailController> _logger;

		public EmailController(
			IEmailRepository emailRepository,
			IMessageBroker messageBroker,
			ILogger<EmailController> logger)
		{
			_emailRepository = emailRepository;
			_messageBroker = messageBroker;
			_logger = logger;
		}

		[HttpPost]
		public async Task<IActionResult> CreateEmail([FromBody] CreateEmailCommand command)
		{
			if (string.IsNullOrEmpty(command.RecipientEmail))
				return BadRequest("Recipient email is required");

			try
			{
				var email = new EmailMessage
				{
					RecipientEmail = command.RecipientEmail,
					Subject = command.Subject,
					Body = command.Body
				};

				// Save to database
				var emailId = await _emailRepository.SaveEmailAsync(email);

				// Create integration event
				var emailEvent = new EmailMessageCreatedEvent
				{
					Id = emailId,
					RecipientEmail = email.RecipientEmail,
					Subject = email.Subject,
					Body = email.Body,
					CreatedDate = email.CreatedDate
				};

				// Publish event to message broker
				await _messageBroker.PublishAsync(emailEvent, "email-created");

				_logger.LogInformation($"Email with ID {emailId} created and published to message broker");

				return Ok(new { EmailId = emailId });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating email");
				return StatusCode(500, "An error occurred while processing your request");
			}
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetEmail(string id)
		{
			var email = await _emailRepository.GetEmailByIdAsync(id);
			if (email == null)
				return NotFound();

			return Ok(email);
		}
	}
}
