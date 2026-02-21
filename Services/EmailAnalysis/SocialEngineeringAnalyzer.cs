using DEEPFAKE.Models;

namespace DEEPFAKE.Services.EmailAnalysis
{
    public class SocialEngineeringAnalyzer
    {
        public int Analyze(string content, List<AnalysisFinding> findings)
        {
            int risk = 0;
            var text = content.ToLower();

            // Panic language
            string[] panicPhrases =
            {
                "new sign-in",
                "unusual activity",
                "secure your account",
                "verify your account",
                "account compromised",
                "unknown device",
                "suspicious activity"
            };

            if (panicPhrases.Any(p => text.Contains(p)))
            {
                risk += 30;
                findings.Add(new AnalysisFinding
                {
                    Type = "Phishing",
                    Severity = "Warning",
                    Message = "Account panic / fear-based language detected"
                });
            }

            // Geo fear trigger
            string[] riskyLocations = { "russia", "china", "north korea", "iran" };
            if (riskyLocations.Any(l => text.Contains(l)))
            {
                risk += 15;
                findings.Add(new AnalysisFinding
                {
                    Type = "Phishing",
                    Severity = "Warning",
                    Message = "High-risk geographic location used to induce urgency"
                });
            }

            return risk;
        }
    }
}
