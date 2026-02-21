namespace DEEPFAKE.DTOs
{
    public class EmailAnalysisHistoryDto
    {
        public DateTime CreatedAt { get; set; }
        public int SecurityScore { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public int FindingsCount { get; set; }
    }
}
