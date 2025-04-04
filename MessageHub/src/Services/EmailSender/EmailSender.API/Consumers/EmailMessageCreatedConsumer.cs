using EmailSender.Application.IntegrationEvents;
using EmailSender.Domain.Entities;
using EmailSender.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace EmailSender.API.Consumers
{
	public class EmailMessageCreatedConsumer
	{
		private readonly IEmailRepository _emailRepository;
		private readonly IEmailService _emailService;
		private readonly ILogger<EmailMessageCreatedConsumer> _logger;

		public EmailMessageCreatedConsumer(
			IEmailRepository emailRepository,
			IEmailService emailService,
			ILogger<EmailMessageCreatedConsumer> logger)
		{
			_emailRepository = emailRepository;
			_emailService = emailService;
			_logger = logger;
		}

		public async Task ConsumeEmailCreatedEvent(EmailMessageCreatedEvent emailEvent)
		{
			try
			{
				_logger.LogInformation($"Received email created event with ID: {emailEvent.Id}");

				// Save email to database
				var email = new EmailMessage
				{
					Id = emailEvent.Id,
					RecipientEmail = emailEvent.RecipientEmail,
					Subject = emailEvent.Subject,
					Body = emailEvent.Body,
					CreatedDate = emailEvent.CreatedDate,
					IsSent = false
				};

				await _emailRepository.SaveEmailAsync(email);

				// Send the email
				try
				{
					await _emailService.SendEmailAsync(email.RecipientEmail, email.Subject, email.Body);
					await _emailRepository.UpdateEmailStatusAsync(email.Id, true);
					_logger.LogInformation($"Email with ID {email.Id} sent successfully");
				}
				catch (Exception ex)
				{
					await _emailRepository.UpdateEmailStatusAsync(email.Id, false, ex.Message);
					_logger.LogError(ex, $"Failed to send email with ID {email.Id}");
					throw;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing email created event");
				throw;
			}
		}
	}
}