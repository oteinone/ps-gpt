using Azure;
using Azure.AI.OpenAI;
using PowershellGpt.Config;
using PowershellGpt.Config.Provider;

namespace PowershellGpt.AzureAi;

public interface IAiClient
{
    void InitSystemPrompt(string systemPrompt);
    IAsyncEnumerable<string> Ask(string userPrompt);
    IAsyncEnumerable<string> GetSystemResponse(); 
}

public class AzureAiClient : IAiClient
{
    private readonly string ModelName;
    private readonly string EndpointUrl;
    private readonly ChatCompletionsOptions options;

    private OpenAIClient client;
    private Task<Response<StreamingChatCompletions>>? initTask;
    private bool initCompleted = false;

    public AzureAiClient(IAppConfigurationProvider configProvider)
    {
        var appConfig = configProvider.AppConfig;

        if (appConfig.EndpointType == null) throw new Exception("Could not init AI client as endpoint type was not found in configuration");
        if (appConfig.ApiKey == null) throw new Exception("Could not init AI client as api key was not found in configuration");
        if (appConfig.Model == null) throw new Exception("Could not init AI client as model name was not found in configuration");

        try 
        {
            if (appConfig.EndpointType == GptEndpointType.OpenAIApi)
            {
                EndpointUrl = "<OpenAI endpoint>";
                client = new OpenAIClient(appConfig.ApiKey);
            }
            else
            {
                if (appConfig.EndpointUrl == null) throw new Exception("Could not init AI client as endpoint url was not found in configuration");
                EndpointUrl = appConfig.EndpointUrl;
                client = new OpenAIClient(new Uri(appConfig.EndpointUrl), new AzureKeyCredential(appConfig.ApiKey));
            }

        }
        catch (Exception e)
        {
            throw new Exception ("Could not initialize open ai client", e);
        }

        ModelName = appConfig.Model;

        var modelConfig = Services.AppConfigurationProvider.AppConfig.ModelConfig;
        options = new ChatCompletionsOptions()
        {
            Messages = { },
            Temperature = modelConfig.Temperature,
            MaxTokens = modelConfig.MaxTokenCount,
            NucleusSamplingFactor = modelConfig.NucleusSamplingFactor,
            FrequencyPenalty = modelConfig.FrequencyPenalty,
            PresencePenalty = modelConfig.PresencePenalty
        };

        initTask = null;
    }

    public void InitSystemPrompt(string systemPrompt)
    {
        initTask = GetStreamingResponse(ChatRole.System, systemPrompt);
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
