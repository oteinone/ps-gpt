using Azure;
using Azure.AI.OpenAI;

namespace PowershellGpt.AzureAi;

public class AzureAiClient
{
    
    const float TEMPERATURE = (float)0.7;
    const int  MAX_TOKENS = 2000;
    const float NUCLEUS_SAMPLING_FACTOR = (float)0.95;
    const float FREQUENCY_PENALTY = 0;
    const float PRESENCE_PENALTY = 0;

    private readonly string AzureOpenAiModelName;
    const string DEFAULT_SYSTEMPROMPT = "You are an AI assistant that helps people find information.";

    private readonly ChatCompletionsOptions options;

    private OpenAIClient client;

    public AzureAiClient(string openAiEndpointUrl, string openAiDeploymentModelName, string azureOpenAiKey, string? systemPrompt = null)
    {
        _ = openAiEndpointUrl ?? throw new ArgumentNullException("openAiEndpointUrl");
        _ = openAiDeploymentModelName ?? throw new ArgumentNullException("openAiDeploymentModelName");
        _ = azureOpenAiKey ?? throw new ArgumentNullException("azureOpenAiKey");

        AzureOpenAiModelName = openAiDeploymentModelName;
        
        if (String.IsNullOrEmpty(azureOpenAiKey))
        {
            throw new Exception("No AZURE_OPENAI_API_KEY defined. Define it as an environmental variable.");
        }
        try 
        {
            client = new OpenAIClient(
                new Uri(openAiEndpointUrl),
                new AzureKeyCredential(azureOpenAiKey));
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

        initTask = InitConversationWithSystemRole(systemPrompt ?? DEFAULT_SYSTEMPROMPT);
    }

    public async IAsyncEnumerable<string> Ask(string userPrompt)
    {
        await initTask;
        var response = await GetStreamingCompletionsAsync(userPrompt);
        await foreach (StreamingChatChoice choice in response.Value.GetChoicesStreaming())
        {
            await foreach (ChatMessage message in choice.GetMessageStreaming())
            {
                yield return message.Content;
            }
        }
    }

    private async Task<Response<StreamingChatCompletions>> GetStreamingCompletionsAsync(string message)
    {
        var chatMessage = new ChatMessage(ChatRole.User, message);
        options.Messages.Add(chatMessage);
        return await client.GetChatCompletionsStreamingAsync(
            deploymentOrModelName: AzureOpenAiModelName,
            options
        );
    }

    private Task initTask;

    private async Task<Response<ChatCompletions>> InitConversationWithSystemRole(string systemPrompt)
    {
        var chatMessage = new ChatMessage(ChatRole.System, systemPrompt);
        var response = await client.GetChatCompletionsAsync(
            deploymentOrModelName: AzureOpenAiModelName,
            new ChatCompletionsOptions()
            {
                Messages =
                {
                    chatMessage,
                }
            });
        return response;
    }
}
