using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SemanticKernel.Prompt;

public class AIChatLLM : IChatCompletionService
{
    private readonly HttpClient _httpClient = new();
    private readonly string _endPoint;

    public AIChatLLM(string endPoint)
    {
        _endPoint = endPoint;
    }

    public IReadOnlyDictionary<string, object?> Attributes { get; }

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null, CancellationToken cancellationToken = new CancellationToken())
    {

        var question = string.Join("\n", chatHistory.Select(m => $"{m.Role}: {m.Content}"));
        var settings = executionSettings as AIChatExecutionSettings;
        string ebookId = settings?.EbookID ?? "DEFAULT_EBOOK";

        var payload = new
        {
            ebook_id = ebookId,
            question = question
        };
        
        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );
        
        var response = await _httpClient.PostAsync(_endPoint, content, cancellationToken);
        
        response.EnsureSuccessStatusCode();

        var resultJson = await response.Content.ReadAsStringAsync(cancellationToken);
        
        var message = new ChatMessageContent(AuthorRole.Assistant, resultJson ?? "[No response]");
        
        return new List<ChatMessageContent> { message };
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var ebookId = (executionSettings as AIChatExecutionSettings)?.EbookID ?? "";
        var userMessage = chatHistory.LastOrDefault(m => m.Role == AuthorRole.User)?.Content ?? "";

        var payload = new
        {
            ebook_id = ebookId,
            question = userMessage
        };

        var request = new HttpRequestMessage(HttpMethod.Post, _endPoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (!string.IsNullOrWhiteSpace(line))
            {
                yield return new StreamingChatMessageContent(AuthorRole.Assistant, line);
            }
        }
    }
}