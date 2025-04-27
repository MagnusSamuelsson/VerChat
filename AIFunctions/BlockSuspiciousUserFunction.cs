using Newtonsoft.Json.Linq;
using OpenAI.Responses;
using VericateChat.Interfaces;

namespace VericateChat.AIFunctions
{
    public class BlockSuspiciousUserFunction() : IAIFunction
    {
        public string Name => "block_suspisious_user";
        public string Description => "Om användaren verkar ha onda avsikter, kör du bara den här funktionen. Så blockeras den i en timma";

        private static readonly string[] requiredProps = ["ban_reason"];

        public ResponseTool GetToolDefinition()
        {
            return ResponseTool.CreateFunctionTool(
                Name,
                Description,
                BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        ban_reason = new
                        {
                            type = "string",
                            description = "Anledning till att användaren blockerats"
                        }
                    },
                    required = requiredProps,
                    additionalProperties = false
                })
            );
        }

        public async Task<FunctionCallOutputResponseItem> ExecuteAsync(JObject args, string callId, IServiceProvider serviceProvider)
        {
            var blockReason = args["ban_reason"]?.ToString();

            IHttpContextAccessor httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
            IUserBlockCacheService userBlockCacheService = serviceProvider.GetRequiredService<IUserBlockCacheService>();

            string ip = httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "";

            await userBlockCacheService.Block(ip, blockReason);

            return new FunctionCallOutputResponseItem(callId, blockReason);
        }
    }
}