#pragma warning disable OPENAI001
using OpenAI.Responses;
using System.Text;

public class OpenAI_SDK_Response
{
    private readonly ResponsesClient GPTModel;
    private readonly List<ResponseItem> history = new();
    private readonly CreateResponseOptions config;
    private readonly DateTimeTools? _tools;

    public OpenAI_SDK_Response(string model, string? systemPrompt = null, DateTimeTools? tools = null)
    {
        string? openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(openAIKey))
        {
            throw new InvalidOperationException("Missing environment variable: OPENAI_API_KEY");
        }

        GPTModel = new ResponsesClient(openAIKey);

        config = new CreateResponseOptions
        {
            Model = model,
            TruncationMode = ResponseTruncationMode.Auto,
            EndUserId = "user-1234"
        };

        bool isGpt5 = model.StartsWith("gpt-5", StringComparison.OrdinalIgnoreCase);

        if (isGpt5)
        {
            config.ReasoningOptions = new ResponseReasoningOptions
            {
                ReasoningEffortLevel = ResponseReasoningEffortLevel.Medium,
                ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Auto
            };
        }
        else
        {
            config.Temperature = 0.7f;
            config.TopP = 0.95f;
        }

        if (!string.IsNullOrEmpty(systemPrompt))
        {
            config.Instructions = systemPrompt;
        }

        _tools = tools;
        if (tools != null)
        {
            var noParams = BinaryData.FromString(
                """{"type":"object","properties":{},"required":[],"additionalProperties":false}""");
            config.Tools.Add(ResponseTool.CreateFunctionTool("GetDate", noParams, true, "Get today's date"));
            config.Tools.Add(ResponseTool.CreateFunctionTool("GetTime", noParams, true, "Get the current time"));
        }
    }

    public async Task<string> Call(string userMessage)
    {
        history.Add(ResponseItem.CreateUserMessageItem(userMessage));

        config.InputItems.Clear();
        foreach (var item in history)
        {
            config.InputItems.Add(item);
        }

        ResponseResult response = await GPTModel.CreateResponseAsync(config);

        foreach (var item in response.OutputItems)
        {
            history.Add(item);
        }

        return response.GetOutputText() ?? string.Empty;
    }

    public async IAsyncEnumerable<string> CallStream(string userMessage)
    {
        history.Add(ResponseItem.CreateUserMessageItem(userMessage));

        if (_tools != null)
        {
            config.StreamingEnabled = false;
            const int maxSteps = 5;

            for (int step = 0; step < maxSteps; step++)
            {
                config.InputItems.Clear();
                foreach (var item in history)
                    config.InputItems.Add(item);

                var response = (await GPTModel.CreateResponseAsync(config)).Value;

                foreach (var item in response.OutputItems)
                    history.Add(item);

                var toolCalls = response.OutputItems.OfType<FunctionCallResponseItem>().ToList();

                if (toolCalls.Count == 0)
                {
                    var text = response.GetOutputText() ?? string.Empty;
                    if (!string.IsNullOrEmpty(text))
                        yield return text;
                    yield break;
                }

                foreach (var tc in toolCalls)
                    history.Add(ResponseItem.CreateFunctionCallOutputItem(tc.CallId, _tools.Execute(tc.FunctionName)));
            }
            yield break;
        }

        config.InputItems.Clear();
        foreach (var item in history)
        {
            config.InputItems.Add(item);
        }

        var sb = new StringBuilder();
        config.StreamingEnabled = true;

        await foreach (var update in GPTModel.CreateResponseStreamingAsync(config))
        {
            if (update is StreamingResponseOutputTextDeltaUpdate textDelta)
            {
                if (!string.IsNullOrEmpty(textDelta.Delta))
                {
                    sb.Append(textDelta.Delta);
                    yield return textDelta.Delta;
                }
            }
        }

        config.StreamingEnabled = false;
        history.Add(ResponseItem.CreateAssistantMessageItem(sb.ToString()));
    }

    public void ClearHistory()
    {
        history.Clear();
        config.InputItems.Clear();
    }
}