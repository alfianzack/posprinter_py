using System;
using System.IO;
using System.Text.Json;

namespace PosPrinterApp.Models
{
    public class PosPrinterPollSettings
    {
        public bool Enabled { get; set; } = false;
        public string BaseUrl { get; set; } = "https://dxnpos-train.dxn2u.com";
        public string CompanyId { get; set; } = "";
        public string UserId { get; set; } = ""; // user_id di Yii2 (diambil dari API)
        public string ApiKey { get; set; } = ""; // X-PosPrinter-Key (satu-satunya yang perlu di-input)
        public int IntervalMs { get; set; } = 3000; // Default, bisa di-override dari API
        public int MaxJobs { get; set; } = 5; // Default, bisa di-override dari API

        // Simpan API key ke file sederhana (hanya 1 baris)
        public static string ApiKeyFilePath => Path.Combine(AppContext.BaseDirectory, "posprinter_api_key.txt");

        public static string LoadApiKey()
        {
            try
            {
                if (File.Exists(ApiKeyFilePath))
                {
                    return File.ReadAllText(ApiKeyFilePath).Trim();
                }
            }
            catch { }
            return "";
        }

        public static void SaveApiKey(string apiKey)
        {
            try
            {
                File.WriteAllText(ApiKeyFilePath, apiKey.Trim());
            }
            catch { }
        }
    }
}

