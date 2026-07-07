namespace Hermess.Models
{
    public class BroadcastRequest
    {
        // Contenido del mensaje, obligatorio
        public required string Content { get; set; }

        // Lista de archivos adjuntos (opcional)
        public List<string> Files { get; set; } = new List<string>();
    }
}
