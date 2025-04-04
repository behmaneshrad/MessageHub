using EmailReceiver.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailReceiver.Domain.Interfaces
{
	public interface IEmailRepository
	{
		Task<string> SaveEmailAsync(EmailMessage email);
		Task<EmailMessage> GetEmailByIdAsync(string id);
		Task<IEnumerable<EmailMessage>> GetPendingEmailsAsync();
		Task UpdateEmailStatusAsync(string id, bool isSent);
	}
}