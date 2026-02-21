namespace DEEPFAKE.Models
{
    public class AnalysisFinding
    {
        public string Type { get; set; } = string.Empty;   // Spoofing / Phishing / Malware
        public string Severity { get; set; } = string.Empty; // Safe / Warning / Danger
        public string Message { get; set; } = string.Empty;
    }
}
