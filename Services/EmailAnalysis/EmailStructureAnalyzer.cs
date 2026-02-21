namespace DEEPFAKE.Services.EmailAnalysis
{
    public class EmailStructureAnalyzer
    {
        public int Analyze(string content)
        {
            int score = 0;
            var text = content.ToLower();

            // 1. Headers
            if (text.Contains("from:") || text.Contains("to:") || text.Contains("subject:"))
                score += 30;

            // 2. Sentence structure
            bool hasSpaces = text.Contains(" ");
            bool hasWords = text.Split(' ').Any(w => w.Length > 2);
            bool hasMultipleSentences = text.Contains(".") || text.Contains("\n");

            if (hasSpaces && hasWords && hasMultipleSentences)
                score += 25;

            // 3. Greeting
            string[] greetings = { "dear", "hello", "hi ", "greetings" };
            if (greetings.Any(g => text.Contains(g)))
                score += 15;

            // 4. Signature
            string[] signatures = { "regards", "thanks", "sincerely", "best", "team" };
            if (signatures.Any(s => text.Contains(s)))
                score += 15;

            // 5. Links
            if (text.Contains("http://") || text.Contains("https://") || text.Contains("www."))
                score += 15;

            return score;
        }
    }
}
