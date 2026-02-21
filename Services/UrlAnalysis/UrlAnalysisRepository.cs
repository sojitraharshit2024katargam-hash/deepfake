using DEEPFAKE.DTOs;
using Npgsql;

namespace DEEPFAKE.Services.UrlAnalysis
{
    public class UrlAnalysisRepository
    {
        private readonly string _connection;

        public UrlAnalysisRepository(IConfiguration config)
        {
            _connection = config.GetConnectionString("DefaultConnection");
        }

        // SAVE
        public void Save(int userId, string url, int score, string risk, string reasons)
        {
            using var con = new NpgsqlConnection(_connection);

            var cmd = new NpgsqlCommand(@"
                INSERT INTO UrlAnalysisLogs
                (UserId, Url, SecurityScore, RiskLevel, Reasons, CreatedAt)
                VALUES
                (@UserId, @Url, @Score, @Risk, @Reasons, @Date)", con);

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Url", url);
            cmd.Parameters.AddWithValue("@Score", score);
            cmd.Parameters.AddWithValue("@Risk", risk);
            cmd.Parameters.AddWithValue("@Reasons", reasons);
            cmd.Parameters.AddWithValue("@Date", DateTime.Now);

            con.Open();
            cmd.ExecuteNonQuery();
        }

        // HISTORY
        public List<UrlHistoryDto> GetHistory(int userId, int limit)
        {
            var list = new List<UrlHistoryDto>();

            using var con = new NpgsqlConnection(_connection);

            var cmd = new NpgsqlCommand(@"
                SELECT
                    Url,
                    SecurityScore,
                    RiskLevel,
                    CreatedAt
                FROM UrlAnalysisLogs
                WHERE UserId=@UserId
                ORDER BY CreatedAt DESC
                LIMIT @Limit", con);

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Limit", limit);

            con.Open();

            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new UrlHistoryDto
                {
                    Url = r.GetString(0),
                    SecurityScore = r.GetInt32(1),
                    RiskLevel = r.GetString(2),
                    CreatedAt = r.GetDateTime(3)
                });
            }

            return list;
        }

        // CLEAR
        public void Clear(int userId)
        {
            using var con = new NpgsqlConnection(_connection);

            var cmd = new NpgsqlCommand(
                "DELETE FROM UrlAnalysisLogs WHERE UserId=@UserId", con);

            cmd.Parameters.AddWithValue("@UserId", userId);

            con.Open();
            cmd.ExecuteNonQuery();
        }
    }
}