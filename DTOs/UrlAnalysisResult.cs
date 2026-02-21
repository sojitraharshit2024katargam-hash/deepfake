using System.Collections.Generic;

namespace DEEPFAKE.DTOs
{
    public class UrlAnalysisResult
    {
        public int SecurityScore { get; set; }

        public string RiskLevel { get; set; }

        public List<string> Reasons { get; set; } = new();
    }
}
