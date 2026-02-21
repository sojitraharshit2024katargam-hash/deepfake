using DEEPFAKE.DTOs;
using DEEPFAKE.Services.Interfaces;
using DEEPFAKE.Services.UrlAnalysis;
using System.Text.RegularExpressions;

namespace DEEPFAKE.Services.Implementations
{
    public class UrlAnalysisService : IUrlAnalysisService
    {
        private readonly UrlAnalysisRepository _repo;

        public UrlAnalysisService(UrlAnalysisRepository repo)
        {
            _repo = repo;
        }

        public UrlAnalysisResult Analyze(string url, int userId)
        {
            var result = new UrlAnalysisResult();

            int score = 100;

            // HTTPS
            if (!url.StartsWith("https://"))
            {
                score -= 25;
                result.Reasons.Add("No HTTPS encryption");
            }

            // IP URL
            if (Regex.IsMatch(url, @"\d+\.\d+\.\d+\.\d+"))
            {
                score -= 30;
                result.Reasons.Add("IP based URL detected");
            }

            // Shorteners
            if (url.Contains("bit.ly") ||
                url.Contains("tinyurl") ||
                url.Contains("t.co"))
            {
                score -= 20;
                result.Reasons.Add("URL shortener used");
            }

            // Suspicious Words
            if (url.Contains("login") ||
                url.Contains("verify") ||
                url.Contains("secure") ||
                url.Contains("update"))
            {
                score -= 25;
                result.Reasons.Add("Suspicious keywords found");
            }

            // Long URL
            if (url.Length > 100)
            {
                score -= 15;
                result.Reasons.Add("Very long URL");
            }

            result.SecurityScore = Math.Clamp(score, 0, 100);

            result.RiskLevel =
                result.SecurityScore >= 80 ? "Low" :
                result.SecurityScore >= 50 ? "Medium" :
                "High";

            // Save
            _repo.Save(
                userId,
                url,
                result.SecurityScore,
                result.RiskLevel,
                string.Join(", ", result.Reasons)
            );

            return result;
        }

        public List<UrlHistoryDto> GetHistory(int userId, int limit)
        {
            return _repo.GetHistory(userId, limit);
        }

        public void ClearHistory(int userId)
        {
            _repo.Clear(userId);
        }
    }
}
