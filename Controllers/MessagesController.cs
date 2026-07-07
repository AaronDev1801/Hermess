using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles;
using Supabase;
using Hermess.Models;
using Hermess.Hubs;

namespace Hermess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly Supabase.Client _supabase;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IConfiguration _config;

        public MessagesController(Supabase.Client supabase, IHubContext<ChatHub> hubContext, IConfiguration config)
        {
            _supabase = supabase;
            _hubContext = hubContext;
            _config = config;
        }

        //Valida el token con Supabase y devuelve el userId (sin depender del JWT secret local)
        private async Task<string?> GetUserIdFromToken()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            var accessToken = authHeader["Bearer ".Length..];

            try
            {
                var supabaseUrl = _config["Supabase:Url"]!;
                var supabaseKey = _config["Supabase:Key"]!;

                using var http = new HttpClient();
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                http.DefaultRequestHeaders.Add("apikey", supabaseKey);

                var response = await http.GetAsync($"{supabaseUrl}/auth/v1/user");
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                return json.GetProperty("id").GetString();
            }
            catch
            {
                return null;
            }
        }

        //Enviar mensaje directo
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendRequest request)
        {
            var senderId = await GetUserIdFromToken();

            if (string.IsNullOrEmpty(senderId))
                return Unauthorized(new { error = "Token inválido o expirado" });

            if (request == null || request.ReceiverId == Guid.Empty)
                return BadRequest(new { error = "Datos inválidos: falta receiverId" });

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = request.ReceiverId.ToString(),
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                IsMassive = false
            };

            var insertResult = await _supabase.From<Message>().Insert(message);
            var savedMessage = insertResult.Models.FirstOrDefault();

            var savedFiles = new List<object>();

            // Archivos adjuntos
            if (request.Files != null && request.Files.Any() && savedMessage != null)
            {
                var provider = new FileExtensionContentTypeProvider();

                foreach (var fileUrl in request.Files)
                {
                    if (!provider.TryGetContentType(fileUrl, out var contentType))
                        contentType = "application/octet-stream";

                    string mappedType = "document";
                    if (contentType.StartsWith("image/")) mappedType = "image";
                    else if (contentType.StartsWith("video/")) mappedType = "video";
                    else if (contentType.StartsWith("audio/")) mappedType = "audio";

                    var fileEntity = new FileEntity
                    {
                        OwnerId = Guid.Parse(senderId),
                        MessageId = Guid.Parse(savedMessage.Id),
                        FileUrl = fileUrl,
                        FileType = mappedType,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _supabase.From<FileEntity>().Insert(fileEntity);
                    
                    savedFiles.Add(new {
                        file_url = fileUrl,
                        file_type = mappedType
                    });
                }
            }

            var responseData = new
            {
                id = savedMessage?.Id ?? Guid.NewGuid().ToString(),
                senderId = message.SenderId,
                receiverId = message.ReceiverId,
                content = message.Content,
                createdAt = message.CreatedAt,
                isMassive = message.IsMassive,
                files = savedFiles
            };

            await _hubContext.Clients.User(request.ReceiverId.ToString())
                .SendAsync("ReceiveMessage", responseData);

            return Ok(new { success = true, message = "Mensaje directo enviado", data = responseData });
        }

        //Enviar mensaje masivo
        [HttpPost("broadcast")]
        public async Task<IActionResult> BroadcastMessage([FromBody] BroadcastRequest request)
        {
            var senderId = await GetUserIdFromToken();

            if (string.IsNullOrEmpty(senderId))
                return Unauthorized(new { error = "Token inválido o expirado" });

            if (request == null || string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new { error = "Datos inválidos: falta content" });

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = senderId, //ID del remitente porque receiver_id requiere un UUID que exista en la tabla users (Otro gran de dolor de cabeza el darme cuenta)
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                IsMassive = true
            };

            var insertResult = await _supabase.From<Message>().Insert(message);
            var savedMessage = insertResult.Models.FirstOrDefault();

            var savedFiles = new List<object>();

            // Archivos adjuntos
            if (request.Files != null && request.Files.Any() && savedMessage != null)
            {
                var provider = new FileExtensionContentTypeProvider();

                foreach (var fileUrl in request.Files)
                {
                    if (!provider.TryGetContentType(fileUrl, out var contentType))
                        contentType = "application/octet-stream";

                    string mappedType = "document";
                    if (contentType.StartsWith("image/")) mappedType = "image";
                    else if (contentType.StartsWith("video/")) mappedType = "video";
                    else if (contentType.StartsWith("audio/")) mappedType = "audio";

                    var fileEntity = new FileEntity
                    {
                        OwnerId = Guid.Parse(senderId),
                        MessageId = Guid.Parse(savedMessage.Id),
                        FileUrl = fileUrl,
                        FileType = mappedType,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _supabase.From<FileEntity>().Insert(fileEntity);

                    savedFiles.Add(new {
                        file_url = fileUrl,
                        file_type = mappedType
                    });
                }
            }

            var responseData = new
            {
                id = savedMessage?.Id ?? Guid.NewGuid().ToString(),
                senderId = message.SenderId,
                receiverId = message.ReceiverId,
                content = message.Content,
                createdAt = message.CreatedAt,
                isMassive = message.IsMassive,
                files = savedFiles
            };

            await _hubContext.Clients.All.SendAsync("ReceiveBroadcast", responseData);

            return Ok(new { success = true, message = "Mensaje masivo enviado", data = responseData });
        }
    }
}
