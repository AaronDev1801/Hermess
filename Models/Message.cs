using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace Hermess.Models
{
    [Table("messages")] // nombre de tu tabla en Supabase
    public class Message : BaseModel
    {
        // 👇 Constructor vacío requerido por Supabase
        public Message() { }

        [PrimaryKey("id", shouldInsert: false)]
        public string Id { get; set; } = string.Empty;

        [Column("sender_id")]
        public string SenderId { get; set; } = string.Empty;

        [Column("receiver_id")]
        public string ReceiverId { get; set; } = string.Empty;

        [Column("content")]
        public string Content { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("is_massive")]
        public bool IsMassive { get; set; } = false;
    }
}
