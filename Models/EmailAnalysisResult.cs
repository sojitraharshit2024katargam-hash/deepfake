namespace DEEPFAKE.Models
{
    public class EmailAnalysisResult
    {
        public int SecurityScore { get; set; }
        public string RiskLevel { get; set; }
        public List<AnalysisFinding> Findings { get; set; } = new();
        public List<ScoreContribution> ScoreBreakdown { get; set; } = new();
    }
}
