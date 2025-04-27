using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using OpenAI.Responses;
using VericateChat.Interfaces;
using VericateChat.Models;

namespace VericateChat.AIFunctions
{
    public class RunAnySqlQueryFunction() : IAIFunction
    {
        public string Name => "run_any_sql_query";
        public string Description => "Kör valfri SQL-fråga mot databasen";

        private static readonly string[] requiredProps = ["sql_query"];

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
                        sql_query = new
                        {
                            type = "string",
                            description = "SQL-fråga att köra"
                        }
                    },
                    required = requiredProps,
                    additionalProperties = false
                })
            );
        }

        public async Task<FunctionCallOutputResponseItem> ExecuteAsync(JObject args, string callId, IServiceProvider serviceProvider)
        {
            var sqlQuery = args["sql_query"]?.ToString();
            if (string.IsNullOrWhiteSpace(sqlQuery))
            {
                return new FunctionCallOutputResponseItem(callId, "Ingen SQL-fråga angiven.");
            }

            IChatRepository chatRepository = serviceProvider.GetRequiredService<IChatRepository>();

            SqlResult result = await chatRepository.RunQueryAsync(sqlQuery);
            string resultJson = JsonConvert.SerializeObject(result);

            return new FunctionCallOutputResponseItem(callId, resultJson);
        }
    }
}