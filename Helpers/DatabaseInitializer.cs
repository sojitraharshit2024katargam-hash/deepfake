using Npgsql;

namespace DEEPFAKE.Helpers
{
    public static class DatabaseInitializer
    {
        public static void Initialize(string connectionString)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            var cmd = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS Users (
                    UserId SERIAL PRIMARY KEY,
                    FullName VARCHAR(150),
                    Email VARCHAR(150) UNIQUE NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    CreatedAt TIMESTAMP NOT NULL,
                    IsEmailVerified BOOLEAN DEFAULT FALSE,
                    OTPCode VARCHAR(10),
                    OTPGeneratedAt TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS EmailAnalysisLogs (
                    Id SERIAL PRIMARY KEY,
                    UserId INTEGER REFERENCES Users(UserId) ON DELETE CASCADE,
                    EmailHash TEXT,
                    SecurityScore INTEGER,
                    RiskLevel VARCHAR(20),
                    FindingsCount INTEGER,
                    CreatedAt TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS UrlAnalysisLogs (
                    Id SERIAL PRIMARY KEY,
                    UserId INTEGER REFERENCES Users(UserId) ON DELETE CASCADE,
                    Url TEXT,
                    SecurityScore INTEGER,
                    RiskLevel VARCHAR(20),
                    Reasons TEXT,
                    CreatedAt TIMESTAMP
                );
            ", conn);

            cmd.ExecuteNonQuery();
        }
    }
}