using OpenAI.Responses;

namespace VericateChat.Models
{
    public record ChatRequest(
        string? Message,
        string? State,
        List<ResponseItem>? Responses
    );
}
