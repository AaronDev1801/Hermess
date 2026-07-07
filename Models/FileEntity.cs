using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace Hermess.Models
{
    [Table("files")] // 👈 nombre de tu tabla en Supabase
    public class FileEntity : BaseModel
    {
        // 👇 Constructor vacío requerido por Supabase
        public FileEntity() { }

        [PrimaryKey("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("owner_id")]
        public Guid OwnerId { get; set; }

        [Column("message_id")]
        public Guid MessageId { get; set; }

        [Column("file_url")]
        public string FileUrl { get; set; } = string.Empty;

        [Column("file_type")]
        public string FileType { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

