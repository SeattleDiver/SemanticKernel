using System.Text.RegularExpressions;

public static class CodeExtractor
{
    public static string ExtractCSharpCode(string llmResponse)
    {
        // Extracts code between ```csharp and ``` 
        var match = Regex.Match(llmResponse, @"```csharp\s*(.*?)\s*```", RegexOptions.Singleline);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        // Fallback in case Gemini forgets the markdown formatting
        return llmResponse;
    }
}