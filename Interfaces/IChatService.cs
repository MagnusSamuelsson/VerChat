using VericateChat.Models;

namespace VericateChat.Interfaces
{
    public interface IChatService
    {
        Task StreamChatAsync(ChatRequest request, Stream outputStream, CancellationToken cancellationToken);
    }
}
