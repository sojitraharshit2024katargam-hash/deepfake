using DEEPFAKE.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace DEEPFAKE.Controllers
{
    public class AccountController : Controller
    {
        private readonly string connectionString;

        public AccountController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // ========================= REGISTER =========================
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User user, string password)
        {
            string otp = GenerateOtp();

            user.PasswordHash = HashPassword(password);
            user.CreatedAt = DateTime.Now;
            user.IsEmailVerified = false;
            user.OTPCode = otp;
            user.OTPGeneratedAt = DateTime.Now;

            using NpgsqlConnection con = new NpgsqlConnection(connectionString);

            string query = @"
                INSERT INTO Users
                (FullName, Email, PasswordHash, CreatedAt, IsEmailVerified, OTPCode, OTPGeneratedAt)
                VALUES
                (@FullName, @Email, @PasswordHash, @CreatedAt, @IsEmailVerified, @OTPCode, @OTPGeneratedAt)
            ";

            NpgsqlCommand cmd = new NpgsqlCommand(query, con);
            cmd.Parameters.AddWithValue("@FullName", user.FullName);
            cmd.Parameters.AddWithValue("@Email", user.Email);
            cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            cmd.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);
            cmd.Parameters.AddWithValue("@IsEmailVerified", user.IsEmailVerified);
            cmd.Parameters.AddWithValue("@OTPCode", user.OTPCode);
            cmd.Parameters.AddWithValue("@OTPGeneratedAt", user.OTPGeneratedAt);

            con.Open();
            cmd.ExecuteNonQuery();

            SendOtpEmail(user.Email, otp);

            TempData["Email"] = user.Email;
            return RedirectToAction("VerifyOtp");
        }

        // ========================= VERIFY OTP =========================
        public IActionResult VerifyOtp()
        {
            return View();
        }

        [HttpPost]
        public IActionResult VerifyOtp(string email, string otp)
        {
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);

            string query = @"SELECT OTPGeneratedAt FROM Users WHERE Email=@Email AND OTPCode=@OTP";

            NpgsqlCommand cmd = new NpgsqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@OTP", otp);

            con.Open();
            object result = cmd.ExecuteScalar();

            if (result != null)
            {
                DateTime generatedAt = Convert.ToDateTime(result);

                if ((DateTime.Now - generatedAt).TotalMinutes <= 10)
                {
                    string update = @"
                        UPDATE Users
                        SET IsEmailVerified = TRUE,
                            OTPCode = NULL,
                            OTPGeneratedAt = NULL
                        WHERE Email = @Email
                    ";

                    NpgsqlCommand updateCmd = new NpgsqlCommand(update, con);
                    updateCmd.Parameters.AddWithValue("@Email", email);
                    updateCmd.ExecuteNonQuery();

                    return RedirectToAction("Login");
                }
            }

            ViewBag.Error = "Invalid or expired OTP";
            TempData["Email"] = email;
            return View();
        }

        // ========================= LOGIN =========================
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            string hash = HashPassword(password);

            using NpgsqlConnection con = new NpgsqlConnection(connectionString);

            string query = @"
                SELECT UserId, FullName, IsEmailVerified
                FROM Users
                WHERE Email=@Email AND PasswordHash=@PasswordHash
            ";

            NpgsqlCommand cmd = new NpgsqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@PasswordHash", hash);

            con.Open();
            NpgsqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                bool isVerified = reader.GetBoolean(2);

                if (!isVerified)
                {
                    ViewBag.Error = "Please verify your email before logging in.";
                    return View();
                }

                HttpContext.Session.SetInt32("UserId", reader.GetInt32(0));
                HttpContext.Session.SetString("UserName", reader.GetString(1));

                return RedirectToAction("Dashboard", "Home");
            }

            ViewBag.Error = "Invalid email or password";
            return View();
        }

        // ========================= LOGOUT =========================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        // ========================= HELPERS =========================
        private string GenerateOtp()
        {
            Random rnd = new Random();
            return rnd.Next(100000, 999999).ToString();
        }

        private string HashPassword(string password)
        {
            using SHA256 sha = SHA256.Create();
            return Convert.ToBase64String(
                sha.ComputeHash(Encoding.UTF8.GetBytes(password))
            );
        }

        private void SendOtpEmail(string toEmail, string otp)
        {
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("yourgmail@gmail.com");
            mail.To.Add(toEmail);
            mail.Subject = "DEEPFAKE | Email Verification OTP";
            mail.Body = $"Your OTP is: {otp}\n\nThis OTP is valid for 10 minutes.\n\n– DEEPFAKE Team";

            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(
                    "sojitraharshit2024.katargam@gmail.com",
                    "nnnuspsbmsamfohv"
                ),
                EnableSsl = true
            };

            smtp.Send(mail);
        }
    }
}