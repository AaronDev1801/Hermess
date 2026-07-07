namespace Hermess.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Estas propiedades son obligatorias al instanciar la clase
        public required string Email { get; set; }
        public required string DisplayName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
