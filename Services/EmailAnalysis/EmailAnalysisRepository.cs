using DEEPFAKE.DTOs;
using DEEPFAKE.Models;
using Microsoft.Data.SqlClient;

namespace DEEPFAKE.Services.EmailAnalysis
{
    public class EmailAnalysisRepository
    {
        private readonly string _connectionString;

        public EmailAnalysisRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public void Save(EmailAnalysisLog log)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(@"
                INSERT INTO EmailAnalysisLogs
                (UserId, EmailHash, SecurityScore, RiskLevel, FindingsCount, CreatedAt)
                VALUES
                (@UserId, @EmailHash, @SecurityScore, @RiskLevel, @FindingsCount, @CreatedAt)", con);

            cmd.Parameters.AddWithValue("@UserId", (object?)log.UserId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EmailHash", log.EmailHash);
            cmd.Parameters.AddWithValue("@SecurityScore", log.SecurityScore);
            cmd.Parameters.AddWithValue("@RiskLevel", log.RiskLevel);
            cmd.Parameters.AddWithValue("@FindingsCount", log.FindingsCount);
            cmd.Parameters.AddWithValue("@CreatedAt", log.CreatedAt);

            con.Open();
            cmd.ExecuteNonQuery();
        }

        public List<EmailAnalysisHistoryDto> GetHistoryByUser(int userId, int limit)
        {
            var list = new List<EmailAnalysisHistoryDto>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(@"
        SELECT TOP (@Limit)
            SecurityScore,
            RiskLevel,
            FindingsCount,
            CreatedAt
        FROM EmailAnalysisLogs
        WHERE UserId = @UserId
        ORDER BY CreatedAt DESC
    ", conn);

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Limit", limit);

            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new EmailAnalysisHistoryDto
                {
                    SecurityScore = reader.GetInt32(0),
                    RiskLevel = reader.GetString(1),
                    FindingsCount = reader.GetInt32(2),
                    CreatedAt = reader.GetDateTime(3)
                });
            }

            return list;
        }

        public void ClearHistory(int userId)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "DELETE FROM EmailAnalysisLogs WHERE UserId = @UserId", con);

            cmd.Parameters.AddWithValue("@UserId", userId);
            con.Open();
            cmd.ExecuteNonQuery();
        }




    }


}
