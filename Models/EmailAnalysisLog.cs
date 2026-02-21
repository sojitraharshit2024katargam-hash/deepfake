namespace DEEPFAKE.Models
{
    public class EmailAnalysisLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string EmailHash { get; set; }
        public int SecurityScore { get; set; }
        public string RiskLevel { get; set; }
        public int FindingsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
