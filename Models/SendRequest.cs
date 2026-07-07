namespace Hermess.Models
{
    public class SendRequest
    {
        // Id del receptor del mensaje directo
        public required Guid ReceiverId { get; set; }

        // Contenido del mensaje
        public required string Content { get; set; }

        // Lista de archivos adjuntos (URLs)
        public List<string> Files { get; set; } = new List<string>();
    }
}
