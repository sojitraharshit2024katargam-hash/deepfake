using System.Text.RegularExpressions;

namespace DEEPFAKE.Services.EmailAnalysis
{
    public class TextEntropyAnalyzer
    {
        public bool IsValidEmailText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.ToLower();

            // Minimum length (but not too high)
            if (text.Length < 80)
                return false;

            bool hasHeaderLikeText =
                text.Contains("@") ||
                text.Contains("reply-to") ||
                text.Contains("<") && text.Contains(">");

            bool hasParagraphs =
                text.Contains("\n") || text.Contains("\r");

            bool hasGreeting =
                text.Contains("hi ") ||
                text.Contains("hello") ||
                text.Contains("welcome") ||
                text.Contains("dear");

            bool hasSignature =
                text.Contains("regards") ||
                text.Contains("thanks") ||
                text.Contains("team") ||
                text.Contains("inc.") ||
                text.Contains("unsubscribe");

            bool hasLink =
                text.Contains("http://") ||
                text.Contains("https://") ||
                text.Contains("www.");

            int signals =
                (hasHeaderLikeText ? 1 : 0) +
                (hasParagraphs ? 1 : 0) +
                (hasGreeting ? 1 : 0) +
                (hasSignature ? 1 : 0) +
                (hasLink ? 1 : 0);

            // At least 2 real email signals = valid input
            return signals >= 2;
        }

    }
}
