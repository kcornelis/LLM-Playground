using System.Text;
using OpenAI;
using OpenAI.Responses;

namespace Playground.GameCatalog.Api.Services;

public class ResponseService(OpenAIClient _openAiClient)
{
    private const string RagChatModel = "gpt-4o-mini";

    public async Task<string> GenerateAnswerAsync(string systemPrompt, string fewShotExamples, string question, IEnumerable<string> contextSnippets)
    {
#pragma warning disable OPENAI001 // Responses API marked experimental in SDK
        var responses = _openAiClient.GetOpenAIResponseClient(RagChatModel);

        var sb = new StringBuilder();
        sb.AppendLine("System:");
        sb.AppendLine(systemPrompt);
        sb.AppendLine();
        sb.AppendLine("Examples:");
        sb.AppendLine(fewShotExamples);
        sb.AppendLine();
        sb.Append("Question: ").Append(question);
        sb.AppendLine();
        sb.AppendLine("Context:");

        foreach (var s in contextSnippets)
        {
            if (!string.IsNullOrWhiteSpace(s))
            {
                sb.Append("- ").AppendLine(s.Trim());
            }
        }

        var result = await responses.CreateResponseAsync(
            userInputText: sb.ToString(),
            new ResponseCreationOptions());

        foreach (var item in result.Value.OutputItems)
        {
            if (item is MessageResponseItem message)
            {
                var text = message.Content?.FirstOrDefault()?.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text!;
                }
            }
        }

        return "I don't know based on the catalog.";
#pragma warning restore OPENAI001
    }
}
