namespace Hermess.Models
{
    public class SendRequest
    {
        public required Guid ReceiverId { get; set; }

        public required string Content { get; set; }

        public List<string> Files { get; set; } = new List<string>();
    }
}
