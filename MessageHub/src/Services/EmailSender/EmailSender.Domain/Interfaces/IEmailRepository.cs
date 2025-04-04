using EmailSender.Domain.Entities;

namespace EmailSender.Domain.Interfaces
{
    public interface IEmailRepository
	{
		Task SaveEmailAsync(EmailMessage email);
		Task<EmailMessage> GetEmailByIdAsync(string id);
		Task UpdateEmailStatusAsync(string id, bool isSent, string errorMessage = null);
	}
}