using Microsoft.SemanticKernel;

namespace SemanticKernel.Prompt;

public class AIChatExecutionSettings : PromptExecutionSettings
{
    public string EbookID { get; set; } = string.Empty;

}