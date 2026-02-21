using DEEPFAKE.DTOs;
using DEEPFAKE.Helpers;
using DEEPFAKE.Models;
using DEEPFAKE.Services.Interfaces;
using DEEPFAKE.Services.EmailAnalysis;

namespace DEEPFAKE.Services.Implementations
{
    public class EmailAnalysisService : IEmailAnalysisService
    {
        private readonly EmailAnalysisRepository _repository;

        public EmailAnalysisService(EmailAnalysisRepository repository)
        {
            _repository = repository;
        }

        // ======================================================
        // 🔍 MAIN ANALYSIS METHOD
        // ======================================================
        public EmailAnalysisResult Analyze(string emailContent, int? userId)
        {
            var result = new EmailAnalysisResult();

            // ===============================
            // 1️⃣ INPUT VALIDATION
            // ===============================
            if (string.IsNullOrWhiteSpace(emailContent) ||
                emailContent.Length < 50 ||
                IsGarbageInput(emailContent))
            {
                result.SecurityScore = 0;
                result.RiskLevel = "Invalid";

                result.Findings.Add(new AnalysisFinding
                {
                    Type = "Input Validation",
                    Severity = "Danger",
                    Message = "Input does not look like a real email. Please paste full email content."
                });

                result.ScoreBreakdown.Add(new ScoreContribution
                {
                    Reason = "Invalid or meaningless input",
                    Points = -100
                });

                return result;
            }

            int score = 100;
            var lower = emailContent.ToLower();

            result.ScoreBreakdown.Add(new ScoreContribution
            {
                Reason = "Base security score",
                Points = 100
            });

            // ===============================
            // 🔴 SPOOFING
            // ===============================
            if (lower.Contains("http://"))
            {
                score -= 30;

                result.Findings.Add(new AnalysisFinding
                {
                    Type = "Spoofing",
                    Severity = "Danger",
                    Message = "Unsecured HTTP links detected"
                });

                result.ScoreBreakdown.Add(new ScoreContribution
                {
                    Reason = "Unsecured HTTP links",
                    Points = -30
                });
            }

            // ===============================
            // 🟡 PHISHING
            // ===============================
            if (lower.Contains("urgent") ||
                lower.Contains("verify") ||
                lower.Contains("immediately") ||
                lower.Contains("action required"))
            {
                score -= 40;

                result.Findings.Add(new AnalysisFinding
                {
                    Type = "Phishing",
                    Severity = "Warning",
                    Message = "Urgent or verification language detected"
                });

                result.ScoreBreakdown.Add(new ScoreContribution
                {
                    Reason = "Phishing-style urgency language",
                    Points = -40
                });
            }

            // ===============================
            // 🔴 MALWARE
            // ===============================
            if (lower.Contains(".exe") ||
                lower.Contains(".zip") ||
                lower.Contains(".js"))
            {
                score -= 50;

                result.Findings.Add(new AnalysisFinding
                {
                    Type = "Malware",
                    Severity = "Danger",
                    Message = "Suspicious attachment detected"
                });

                result.ScoreBreakdown.Add(new ScoreContribution
                {
                    Reason = "Executable or archive attachment",
                    Points = -50
                });
            }

            // ===============================
            // 🟢 POSITIVE STRUCTURE
            // ===============================
            if (lower.Contains("from:") &&
                lower.Contains("subject:") &&
                lower.Contains("\n"))
            {
                score += 20;

                result.ScoreBreakdown.Add(new ScoreContribution
                {
                    Reason = "Valid email structure detected",
                    Points = +20
                });
            }

            // ===============================
            // 2️⃣ FINAL SCORE
            // ===============================
            result.SecurityScore = Math.Clamp(score, 0, 100);

            result.RiskLevel =
                result.SecurityScore >= 80 ? "Low" :
                result.SecurityScore >= 50 ? "Medium" : "High";

            // ===============================
            // 3️⃣ SAVE TO DATABASE
            // ===============================
            var log = new EmailAnalysisLog
            {
                UserId = userId,
                EmailHash = HashHelper.Sha256(emailContent),
                SecurityScore = result.SecurityScore,
                RiskLevel = result.RiskLevel,
                FindingsCount = result.Findings.Count,
                CreatedAt = DateTime.Now
            };

            _repository.Save(log);

            return result;
        }

        // ======================================================
        // 🧠 GARBAGE INPUT DETECTOR
        // ======================================================
        private bool IsGarbageInput(string text)
        {
            if (!text.Contains(" ")) return true;
            if (text.Distinct().Count() < 5) return true;
            if (!text.Any(c => ".!?\n".Contains(c))) return true;
            return false;
        }

        // ======================================================
        // 📜 HISTORY
        // ======================================================
        public List<EmailAnalysisHistoryDto> GetHistory(int userId, int limit)
        {
            return _repository.GetHistoryByUser(userId, limit);
        }

        public void ClearHistory(int userId)
        {
            _repository.ClearHistory(userId);
        }


    }
}
