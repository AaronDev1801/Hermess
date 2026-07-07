namespace Hermess.Models
{
    public class BroadcastRequest
    {
        public required string Content { get; set; }

        public List<string> Files { get; set; } = new List<string>();
    }
}
