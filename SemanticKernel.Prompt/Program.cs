using System.Text;
using Microsoft.Extensions.Configuration;
using SemanticKernel.Prompt;
using AuthorRole = Microsoft.SemanticKernel.ChatCompletion.AuthorRole;
using ChatHistory = Microsoft.SemanticKernel.ChatCompletion.ChatHistory;


var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();
string apiUrl = configuration["LLM:ApiUrl"] ?? throw new Exception("LLM ApiUrl is missing");

// Create custom LLM
var myLLM = new AIChatLLM(apiUrl);

// Create chat history and add user question
var chatHistory = new ChatHistory();

// Set execution settings with Ebook ID
var settings = new AIChatExecutionSettings()
{
    EbookID = " "
};

Console.WriteLine("Ask me anything about Economics! (Type 'exit' to quit)");

while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("\nYou: ");
    Console.ResetColor();

    var userInput = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(userInput) || userInput.Trim().Equals("exit", StringComparison.CurrentCultureIgnoreCase))
        break;

    chatHistory.AddMessage(AuthorRole.User, userInput);

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("Assistant: ");
    Console.ResetColor();
    var replyBuilder = new StringBuilder();

    await foreach (var chunk in myLLM.GetStreamingChatMessageContentsAsync(chatHistory, settings))
    {
        Console.Write(chunk.Content);
        replyBuilder.Append(chunk.Content);
    }
    var assistantReply = replyBuilder.ToString();
    chatHistory.AddMessage(AuthorRole.Assistant, assistantReply);

    Console.WriteLine(); // for clean line
}