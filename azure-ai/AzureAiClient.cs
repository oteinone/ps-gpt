using Azure;
using Azure.AI.OpenAI;
using PowershellGpt.Config;

namespace PowershellGpt.AzureAi;

public class AzureAiClient
{
    const float TEMPERATURE = (float)0.7;
    const int  MAX_TOKENS = 2000;
    const float NUCLEUS_SAMPLING_FACTOR = (float)0.95;
    const float FREQUENCY_PENALTY = 0;
    const float PRESENCE_PENALTY = 0;
    const string DEFAULT_SYSTEMPROMPT = "You are an AI assistant that helps people find information.";


    private readonly string ModelName;
    private readonly string Endpoint;
    private readonly ChatCompletionsOptions options;

    private OpenAIClient client;
    private Task<Response<StreamingChatCompletions>> initTask;
    private bool initCompleted = false;

    public AzureAiClient(GptEndpointType? type, string endpointUrl, string modelDeploymentName, string key,
        string? systemPrompt = null)
    {
        _ = endpointUrl ?? throw new ArgumentNullException("openAiEndpointUrl");
        _ = modelDeploymentName ?? throw new ArgumentNullException("openAiDeploymentModelName");
        _ = key ?? throw new ArgumentNullException("azureOpenAiKey");
    
        ModelName = modelDeploymentName;
        Endpoint = endpointUrl;
               
        try 
        {
            if (type == GptEndpointType.OpenAIApi)
            {
                client = new OpenAIClient(key);
            }
            else
            {
                client = new OpenAIClient(new Uri(endpointUrl), new AzureKeyCredential(key));
            }

        }
        catch (Exception e)
        {
            throw new Exception ("Could not initialize open ai client", e);
        }

        options = new ChatCompletionsOptions()
        {
            Messages = { },
            Temperature = TEMPERATURE,
            MaxTokens = MAX_TOKENS,
            NucleusSamplingFactor = NUCLEUS_SAMPLING_FACTOR,
            FrequencyPenalty = FREQUENCY_PENALTY,
            PresencePenalty = PRESENCE_PENALTY,
        };

        initTask = GetStreamingResponse(ChatRole.System, !string.IsNullOrEmpty(systemPrompt) ? systemPrompt : DEFAULT_SYSTEMPROMPT);
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
        var response = await initTask;
        await foreach (StreamingChatChoice choice in response.Value.GetChoicesStreaming())
        {
            await foreach (ChatMessage message in choice.GetMessageStreaming())
            {
                yield return message.Content;
            }
        }
        initCompleted = true;
    }

    private async Task<Response<StreamingChatCompletions>> GetStreamingResponse(ChatRole role, string message)
    {
        var chatMessage = new ChatMessage(ChatRole.User, message);
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
                throw new AuthorizationFailedException($"Could not authorize to open api endpoint {Endpoint} model {ModelName}", e);
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
