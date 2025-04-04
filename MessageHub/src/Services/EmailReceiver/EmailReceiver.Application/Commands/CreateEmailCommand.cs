

namespace EmailReceiver.Application.Commands
{
	public class CreateEmailCommand
	{
		public string RecipientEmail { get; set; }
		public string Subject { get; set; }
		public string Body { get; set; }
	}
}