namespace Hermess.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string Email { get; set; }
        public required string DisplayName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
