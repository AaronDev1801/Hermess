using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace Hermess.Models
{
    [Table("messages")] 
    public class Message : BaseModel
    {
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
