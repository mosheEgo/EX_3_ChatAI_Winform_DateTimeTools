using Google.GenAI;
using Google.GenAI.Types;
using System.Text;

public class Gemini_SDK
{
    private readonly Client GeminiModel;
    private readonly string Model;
    private readonly List<Content> history = new();
    private readonly string? SystemPrompt;
    private readonly GenerateContentConfig config;
    private readonly DateTimeTools? _tools;

    public Gemini_SDK(string model, string? systemPrompt = null, DateTimeTools? tools = null)
    {
        string? geminiApiKey =
            System.Environment.GetEnvironmentVariable("GEMINI_API_KEY") ??
            System.Environment.GetEnvironmentVariable("GOOGLE_API_KEY");

        if (string.IsNullOrWhiteSpace(geminiApiKey))
        {
            throw new InvalidOperationException("Missing environment variable: GEMINI_API_KEY / GOOGLE_API_KEY");
        }

        GeminiModel = new Client(apiKey: geminiApiKey);
        Model = model;
        SystemPrompt = systemPrompt;

        config = new GenerateContentConfig
        {
            Temperature = 0.7f, // Creativity (higher = more random). The 'f' suffix makes this a float literal (single-precision)
            TopP = 0.95f, // pick next tokens only from the smallest set whose cumulative probability ≥ 0.95 (filters unlikely tokens)
            TopK = 40, // Top-K sampling
            //MaxOutputTokens = 256, // Max tokens in the response
            CandidateCount = 1, // Number of candidates to generate
            StopSequences = new List<string> { "END" }, // Stop tokens
            PresencePenalty = 0.0f, // Penalize new topic repetition
            FrequencyPenalty = 0.0f, // Penalize frequent tokens
            Seed = 0, // Deterministic sampling seed
            ResponseMimeType = tools == null ? "text/plain" : null // tools require unrestricted mime type
        };

        if (!string.IsNullOrEmpty(SystemPrompt))
        {
            config.SystemInstruction = new Content { Parts = [new Part { Text = SystemPrompt }] };
        }

        _tools = tools;
        if (tools != null)
        {
            var noParams = new Schema { Type = Google.GenAI.Types.Type.Object };
            config.Tools = new List<Tool>
            {
                new Tool
                {
                    FunctionDeclarations =
                    [
                        new FunctionDeclaration { Name = "GetDate", Description = "Get today's date",     Parameters = noParams },
                        new FunctionDeclaration { Name = "GetTime", Description = "Get the current time", Parameters = noParams }
                    ]
                }
            };
        }
    }

    public async Task<string> Call(string userMessage)
    {
        history.Add(new Content { Role = "user", Parts = [new Part { Text = userMessage }] });

        var response = await GeminiModel.Models.GenerateContentAsync(
            model: Model, contents: history, config: config
        );
        var candidates = response.Candidates;
        if (candidates is null || candidates.Count == 0)
        {
            history.Add(new Content { Role = "model", Parts = [new Part { Text = string.Empty }] });
            return string.Empty;
        }

        var parts = candidates[0].Content?.Parts;
        var text = parts is { Count: > 0 } ? parts[0].Text : string.Empty;

        text ??= string.Empty;
        history.Add(new Content { Role = "model", Parts = [new Part { Text = text }] });
        return text;
    }

    public async IAsyncEnumerable<string> CallStream(string userMessage)
    {
        history.Add(new Content { Role = "user", Parts = [new Part { Text = userMessage }] });

        if (_tools != null)
        {
            const int maxSteps = 5;

            for (int step = 0; step < maxSteps; step++)
            {
                var response = await GeminiModel.Models.GenerateContentAsync(
                    model: Model, contents: history, config: config);

                var parts = response.Candidates?[0].Content?.Parts;
                if (parts == null) yield break;

                var fnCalls = parts.Where(p => p.FunctionCall != null)
                                   .Select(p => p.FunctionCall!)
                                   .ToList();

                if (fnCalls.Count == 0)
                {
                    history.Add(response.Candidates![0].Content!);
                    var text = parts.FirstOrDefault(p => p.Text != null)?.Text ?? string.Empty;
                    if (!string.IsNullOrEmpty(text))
                        yield return text;
                    yield break;
                }

                history.Add(response.Candidates![0].Content!);

                var toolResponseParts = fnCalls.Select(fc => new Part
                {
                    FunctionResponse = new FunctionResponse
                    {
                        Name = fc.Name,
                        Response = new Dictionary<string, object> { { "result", _tools.Execute(fc.Name!) } }
                    }
                }).ToList();

                history.Add(new Content { Role = "user", Parts = toolResponseParts });
            }
            yield break;
        }

        var sb = new StringBuilder();

        await foreach (var chunk in GeminiModel.Models.GenerateContentStreamAsync(
            model: Model, contents: history, config: config))
        {
            var text = chunk.Candidates?[0].Content?.Parts?[0].Text;
            if (!string.IsNullOrEmpty(text))
            {
                sb.Append(text);
                yield return text;
            }
        }

        history.Add(new Content { Role = "model", Parts = [new Part { Text = sb.ToString() }] });
    }

    public void ClearHistory() => history.Clear();
}