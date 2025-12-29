using System.Text;

namespace PosPrinterApp.Resources
{
    /// <summary>
    /// Logo DXN dalam format ASCII untuk print di POS printer
    /// </summary>
    public static class DxnLogo
    {
        /// <summary>
        /// Logo DXN sederhana (1 baris)
        /// </summary>
        public static string GetSimpleLogo()
        {
            return "     DXN POS SYSTEM     ";
        }

        /// <summary>
        /// Logo DXN dengan border
        /// </summary>
        public static string GetLogoWithBorder()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=================================");
            sb.AppendLine("        DXN POS SYSTEM         ");
            sb.AppendLine("=================================");
            return sb.ToString();
        }

        /// <summary>
        /// Logo DXN ASCII art (lebih besar)
        /// </summary>
        public static string GetAsciiLogo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("  _____  __  __  _   _ ");
            sb.AppendLine(" |  __ \\|  \\/  || \\ | |");
            sb.AppendLine(" | |  | | \\  / ||  \\| |");
            sb.AppendLine(" | |  | | |\\/| || . ` |");
            sb.AppendLine(" | |__| | |  | || |\\  |");
            sb.AppendLine(" |_____/|_|  |_||_| \\_|");
            sb.AppendLine();
            sb.AppendLine("    POS SYSTEM");
            return sb.ToString();
        }

        /// <summary>
        /// Logo DXN dengan styling untuk receipt
        /// </summary>
        public static string GetReceiptLogo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=================================");
            sb.AppendLine("        DXN POS SYSTEM         ");
            sb.AppendLine("=================================");
            return sb.ToString();
        }

        /// <summary>
        /// Logo DXN dengan informasi tambahan
        /// </summary>
        public static string GetFullLogo(string? companyName = null, string? address = null, string? phone = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=================================");
            sb.AppendLine("        DXN POS SYSTEM         ");
            sb.AppendLine("=================================");
            
            if (!string.IsNullOrEmpty(companyName))
            {
                sb.AppendLine($"  {companyName}");
            }
            
            if (!string.IsNullOrEmpty(address))
            {
                sb.AppendLine($"  {address}");
            }
            
            if (!string.IsNullOrEmpty(phone))
            {
                sb.AppendLine($"  Telp: {phone}");
            }
            
            sb.AppendLine("=================================");
            return sb.ToString();
        }
    }
}

