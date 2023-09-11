using Azure;
using Azure.AI.OpenAI;
using PowershellGpt.Config;

namespace PowershellGpt.AzureAi;

public class AzureAiClient
{
    private readonly string ModelName;
    private readonly string EndpointUrl;
    private readonly ChatCompletionsOptions options;

    private OpenAIClient client;
    private Task<Response<StreamingChatCompletions>>? initTask;
    private bool initCompleted = false;

    public AzureAiClient(GptEndpointType type, string modelDeploymentName, string key, string? endpointUrl = null,
        string? systemPrompt = null)
    {
        ModelName = modelDeploymentName;
        
        try 
        {
            if (type == GptEndpointType.OpenAIApi)
            {
                EndpointUrl = "<OpenAI endpoint>";
                client = new OpenAIClient(key);
            }
            else
            {
                _ = endpointUrl ?? throw new ArgumentNullException("openAiEndpointUrl");
                EndpointUrl = endpointUrl;
                client = new OpenAIClient(new Uri(endpointUrl), new AzureKeyCredential(key));
            }

        }
        catch (Exception e)
        {
            throw new Exception ("Could not initialize open ai client", e);
        }

        var modelConfig = AppConfiguration.AppConfig.ModelConfig;
        options = new ChatCompletionsOptions()
        {
            Messages = { },
            Temperature = modelConfig.Temperature,
            MaxTokens = modelConfig.MaxTokenCount,
            NucleusSamplingFactor = modelConfig.NucleusSamplingFactor,
            FrequencyPenalty = modelConfig.FrequencyPenalty,
            PresencePenalty = modelConfig.PresencePenalty
        };

        // Use system prompt if it has been defined
        if (systemPrompt != null)
        {
            initTask = GetStreamingResponse(ChatRole.System, systemPrompt);
        }
        else
        {
            initTask = null;
        }
    }

    public async IAsyncEnumerable<string> Ask(string userPrompt)
    {
        await EnsureInitCompleted();
        var response = await GetStreamingResponse(ChatRole.User, userPrompt);
        await foreach (StreamingChatChoice choice in response.Value.GetChoicesStreaming())
        {
            await foreach (ChatMessage message in choice.GetMessageStreaming())
            {
                yield return message.Content;
            }
        }
    }

    public async IAsyncEnumerable<string> GetSystemResponse()
    {
        if (initTask != null)
        {
            var response = await initTask;
            await foreach (StreamingChatChoice choice in response.Value.GetChoicesStreaming())
            {
                await foreach (ChatMessage message in choice.GetMessageStreaming())
                {
                    yield return message.Content;
                }
            }
        }
        initCompleted = true;
    }

    private async Task<Response<StreamingChatCompletions>> GetStreamingResponse(ChatRole role, string message)
    {
        var chatMessage = new ChatMessage(role, message);
        options.Messages.Add(chatMessage);
        try
        {
            return await client.GetChatCompletionsStreamingAsync(
                deploymentOrModelName: ModelName,
                options
            );
        }
        catch (Azure.RequestFailedException e)
        {
            if (e.Status == 401)
            {
                throw new AuthorizationFailedException($"Could not authorize to open api endpoint {EndpointUrl} model {ModelName}", e);
            }
            throw;
        }
    }

    public class AuthorizationFailedException : Exception
    {
        public AuthorizationFailedException(string? message, Exception? innerException)
            :base(message, innerException)
            {

            }
    }

    private async Task EnsureInitCompleted()
    {
        if (initCompleted) return;
        
        await foreach (var _ in GetSystemResponse()); //loop through and discard the IAsyncEnumerables
    }
}
