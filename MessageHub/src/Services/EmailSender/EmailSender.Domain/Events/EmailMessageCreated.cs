namespace EmailSender.Domain.Events
{
    public class EmailMessageCreated
    {
        public string Id { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}