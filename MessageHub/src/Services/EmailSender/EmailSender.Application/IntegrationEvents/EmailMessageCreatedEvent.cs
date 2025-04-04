namespace EmailSender.Application.IntegrationEvents
{
	public class EmailMessageCreatedEvent
	{
		public string Id { get; set; }
		public string RecipientEmail { get; set; }
		public string Subject { get; set; }
		public string Body { get; set; }
		public DateTime CreatedDate { get; set; }
	}
}