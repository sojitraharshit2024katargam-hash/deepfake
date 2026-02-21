using DEEPFAKE.DTOs;
using System.Collections.Generic;

namespace DEEPFAKE.Services.Interfaces
{
    public interface IUrlAnalysisService
    {
        UrlAnalysisResult Analyze(string url, int userId);

        List<UrlHistoryDto> GetHistory(int userId, int limit);

        void ClearHistory(int userId);
    }
}
