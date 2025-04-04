

namespace EmailReceiver.Domain.Entities
{
	public class EmailMessage
	{
		public string Id { get; set; }
		public string RecipientEmail { get; set; }
		public string Subject { get; set; }
		public string Body { get; set; }
		public DateTime CreatedDate { get; set; }
		public bool IsSent { get; set; }
	}
}