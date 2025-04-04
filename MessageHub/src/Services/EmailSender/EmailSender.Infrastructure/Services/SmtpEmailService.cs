using EmailSender.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace EmailSender.Infrastructure.Services
{
	public class SmtpEmailService : IEmailService
	{
		private readonly IConfiguration _configuration;
		private readonly ILogger<SmtpEmailService> _logger;

		public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
		{
			_configuration = configuration;
			_logger = logger;
		}

		public async Task SendEmailAsync(string to, string subject, string body)
		{
			try
			{
				var smtpSettings = _configuration.GetSection("SmtpSettings");
				var fromEmail = smtpSettings["FromEmail"];
				var fromName = smtpSettings["FromName"];
				var host = smtpSettings["Host"];
				var port = int.Parse(smtpSettings["Port"]);
				var username = smtpSettings["Username"];
				var password = smtpSettings["Password"];
				var enableSsl = bool.Parse(smtpSettings["EnableSsl"]);

				using var client = new SmtpClient(host, port)
				{
					Credentials = new NetworkCredential(username, password),
					EnableSsl = enableSsl
				};

				var message = new MailMessage
				{
					From = new MailAddress(fromEmail, fromName),
					Subject = subject,
					Body = body,
					IsBodyHtml = true
				};

				message.To.Add(to);
				await client.SendMailAsync(message);
				_logger.LogInformation($"Email sent successfully to {to}");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Failed to send email to {to}");
				throw;
			}
		}
	}
}