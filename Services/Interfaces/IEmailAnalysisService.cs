using DEEPFAKE.DTOs;
using DEEPFAKE.Models;

namespace DEEPFAKE.Services.Interfaces
{
    public interface IEmailAnalysisService
    {
        // Analyze email for a specific user
        EmailAnalysisResult Analyze(string emailContent, int? userId);

        // Get history for a specific user
        List<EmailAnalysisHistoryDto> GetHistory(int userId, int limit);

        void ClearHistory(int userId);

    }
}
