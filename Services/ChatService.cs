using OpenAI.Responses;
using System.ClientModel;
using Newtonsoft.Json;
using VericateChat.Interfaces;
using VericateChat.Models;
using VericateChat.Helpers;
using VericateChat.AIFunctions;
using Newtonsoft.Json.Linq;

namespace VericateChat.Services
{
    public class ChatService : IChatService
    {
        private readonly OpenAIResponseClient _aiClient;
        private readonly IChatRepository _chatRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserBlockCacheService _userBlockCacheService;
        private readonly IServiceProvider _serviceProvider;
        private readonly List<IAIFunction> _functions;

        public ChatService(
            IChatRepository chatRepository,
            IHttpContextAccessor httpContextAccessor,
            IUserBlockCacheService userBlockCacheService,
            IServiceProvider serviceProvider
            )
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                         ?? throw new InvalidOperationException("Missing OPENAI_API_KEY");

            _aiClient = new OpenAIResponseClient(
                model: "gpt-4o-mini",
                apiKey: apiKey

            );
            _chatRepository = chatRepository;
            _httpContextAccessor = httpContextAccessor;
            _userBlockCacheService = userBlockCacheService;
            _serviceProvider = serviceProvider;
            _functions =
                [
                    new RunAnySqlQueryFunction(),
                    new BlockSuspiciousUserFunction(),
                ];
        }

        public async Task StreamChatAsync(ChatRequest request, Stream outputStream, CancellationToken cancellationToken)
        {
            StreamWriter writer = new(outputStream);

            string? state = request.State;

            List<ResponseItem> pending = [];

            MessageResponseItem newMessage = ResponseItem.CreateUserMessageItem(request.Message);
            Console.WriteLine($"New message: {newMessage}");
            pending.Add(newMessage);
            bool writing = false;
            while (pending.Count != 0)
            {

                ResponseCreationOptions options = CreateOptions(state);
                AsyncCollectionResult<StreamingResponseUpdate> stream = _aiClient.CreateResponseStreamingAsync(pending, options, cancellationToken);
                await writer.WriteAsync($"event: status\ndata: request_sent\n\n");
                await writer.FlushAsync(cancellationToken);
                var nextPending = new List<ResponseItem>();

                await foreach (StreamingResponseUpdate update in stream.WithCancellation(cancellationToken))
                {
                    switch (update)
                    {
                        case StreamingResponseCreatedUpdate created:
                            state = created.Response.Id;
                            await writer.WriteAsync($"event: status\ndata: thinking\n\n");
                            await writer.FlushAsync(cancellationToken);
                            break;
                        case StreamingResponseOutputTextDeltaUpdate textDelta:
                            if (!writing)
                            {
                                await writer.WriteAsync($"event: status\ndata: streaming\n\n");
                                await writer.FlushAsync(cancellationToken);
                                writing = true;
                            }

                            var data = textDelta.Delta;
                            await writer.WriteAsync("event: message\n");
                            await writer.FlushAsync(cancellationToken);
                            var json = JsonConvert.SerializeObject(textDelta.Delta);
                            await writer.WriteAsync($"event: message\ndata: {json}\n\n");
                            await writer.FlushAsync(cancellationToken);
                            break;
                        case StreamingResponseOutputTextDoneUpdate textDone:
                            await writer.WriteAsync($"event: state\ndata: {state}\n\n");
                            await writer.FlushAsync(cancellationToken);
                            writing = false;
                            break;

                        case StreamingResponseOutputItemDoneUpdate outputItemDoneUpdate:
                            if (outputItemDoneUpdate.Item is FunctionCallResponseItem functionCallResponseItem)
                            {
                                FunctionCallOutputResponseItem functionResponse = await HandleFunctionCall(functionCallResponseItem);
                                if (functionResponse != null)
                                {
                                    nextPending.Add(functionResponse);
                                }

                            }
                            break;
                    }
                }
                pending = nextPending;
            }
        }

        private async Task<FunctionCallOutputResponseItem> HandleFunctionCall(FunctionCallResponseItem functionCallResponseItem)
        {
            var aiFunction = _functions.FirstOrDefault(f => f.Name == functionCallResponseItem.FunctionName);

            if (aiFunction != null)
            {
                JObject args = JsonConvert.DeserializeObject<JObject>(functionCallResponseItem.FunctionArguments.ToString()) ?? [];
                return await aiFunction.ExecuteAsync(args, functionCallResponseItem.CallId, _serviceProvider);
            }

            return new FunctionCallOutputResponseItem(
                            functionCallResponseItem.CallId,
                            string.Empty
                        );
        }

        private ResponseCreationOptions CreateOptions(string? state)
        {
            var options = new ResponseCreationOptions();

            foreach (var func in _functions)
            {
                options.Tools.Add(func.GetToolDefinition());
            }

            options.PreviousResponseId = state ?? null;
            options.ParallelToolCallsEnabled = true;
            options.ToolChoice = ResponseToolChoice.CreateAutoChoice();
            options.Instructions = InstructionLoader.GetInstructions();
            return options;
        }
    }
}
