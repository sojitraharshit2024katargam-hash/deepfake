using DEEPFAKE.DTOs;

namespace DEEPFAKE.Helpers
{
    public static class InMemoryEmailHistory
    {
        private static readonly List<EmailAnalysisHistoryDto> _history = new();

        public static void Add(EmailAnalysisHistoryDto item)
        {
            _history.Insert(0, item); // newest first

            if (_history.Count > 20)
                _history.RemoveAt(_history.Count - 1);
        }

        public static List<EmailAnalysisHistoryDto> Get(int limit)
        {
            return _history.Take(limit).ToList();
        }
    }
}
