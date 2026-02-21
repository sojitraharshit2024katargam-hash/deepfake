using System;

namespace DEEPFAKE.DTOs
{
    public class UrlHistoryDto
    {
        public int SecurityScore { get; set; }

        public string RiskLevel { get; set; }

        public string Url { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
