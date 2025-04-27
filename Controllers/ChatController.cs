using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using VericateChat.Interfaces;
using VericateChat.Models;

namespace VericateChat.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController(
        IChatService chatService,
        IStateCacheService stateCacheService,
        IUserBlockCacheService userBlockCacheService
            ) : Controller
    {
        private readonly IChatService _chatService = chatService;
        private readonly IStateCacheService _stateCacheService = stateCacheService;
        private readonly IUserBlockCacheService _userBlockCacheService = userBlockCacheService;

        [HttpPost]
        public async Task<IActionResult> PostResponses([FromBody] ChatRequest request)
        {
            SetSSEHeaders();
            var responseStream = Response.Body;
            await using var writer = new StreamWriter(responseStream);

            if (request.State is not null && _stateCacheService.IsDuplicate(request.State))
            { 
                return new EmptyResult();
            }
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

            if (_userBlockCacheService.IsBlocked(remoteIp))
            {
                await WriteBlockedMessageAsync(writer, HttpContext.RequestAborted);
                return new EmptyResult();
            }

            var chatTask = _chatService.StreamChatAsync(request, responseStream, HttpContext.RequestAborted);
            var pingTask = SendPingsAsync(writer, HttpContext.RequestAborted);

            await chatTask;

            await writer.WriteAsync($"event: status\ndata: ready\n\n");
            await writer.FlushAsync(HttpContext.RequestAborted);
            return new EmptyResult();
        }

        private static async Task SendPingsAsync(StreamWriter writer, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                await writer.WriteAsync("event: ping\ndata: {}\n\n");
                await writer.FlushAsync(cancellationToken);
            }
        }
        private void SetSSEHeaders()
        {
            Response.StatusCode = 200;
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
        }
        private static async Task WriteBlockedMessageAsync(StreamWriter writer, CancellationToken cancellationToken)
        {
            await writer.WriteAsync($"event: status\ndata: streaming\n\n");
            await writer.FlushAsync(cancellationToken);

            await writer.WriteAsync("event: message\n");
            await writer.FlushAsync(cancellationToken);

            string message = JsonConvert.SerializeObject("Du är fortfarande blockerad, vänta lite till");
            await writer.WriteAsync($"event: message\ndata: {message}\n\n");
            await writer.FlushAsync(cancellationToken);

            await writer.WriteAsync($"event: status\ndata: ready\n\n");
            await writer.FlushAsync(cancellationToken);
        }
    }
}
