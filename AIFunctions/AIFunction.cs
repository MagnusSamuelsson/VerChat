using Newtonsoft.Json.Linq;
using OpenAI.Responses;

namespace VericateChat.AIFunctions
{
    public interface IAIFunction
    {
        string Name { get; }
        string Description { get; }

        ResponseTool GetToolDefinition();

        Task<FunctionCallOutputResponseItem> ExecuteAsync(JObject args, string callId, IServiceProvider serviceProvider);
    }
}