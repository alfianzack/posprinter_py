using System;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Text;

namespace PosPrinterApp.Services
{
    public class RawPrinterHelper
    {
        [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPWStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.drv", EntryPoint = "ClosePrinter", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartDocPrinterW", CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

        [DllImport("winspool.drv", EntryPoint = "EndDocPrinter", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartPagePrinter", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "EndPagePrinter", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "WritePrinter", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDocName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pOutputFile;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDataType;

            public DOCINFOA()
            {
                pDocName = "";
                pOutputFile = null;
                pDataType = "RAW";
            }
        }

        public static bool SendBytesToPrinter(string szPrinterName, IntPtr pBytes, int dwCount)
        {
            IntPtr hPrinter = IntPtr.Zero;
            DOCINFOA di = new DOCINFOA();
            bool bSuccess = false;
            int dwWritten = 0;

            di.pDocName = "POS Receipt";
            di.pDataType = "RAW";

            if (OpenPrinter(szPrinterName.Normalize(), out hPrinter, IntPtr.Zero))
            {
                if (StartDocPrinter(hPrinter, 1, di))
                {
                    if (StartPagePrinter(hPrinter))
                    {
                        bSuccess = WritePrinter(hPrinter, pBytes, dwCount, out dwWritten);
                        EndPagePrinter(hPrinter);
                    }
                    EndDocPrinter(hPrinter);
                }
                ClosePrinter(hPrinter);
            }

            if (!bSuccess)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Exception($"Error printing. Error code: {error}");
            }

            return bSuccess;
        }

        public static bool SendStringToPrinter(string szPrinterName, string szString)
        {
            IntPtr pBytes = IntPtr.Zero;
            // Gunakan ASCII encoding untuk ESC/POS commands
            byte[] pBytesArray = Encoding.ASCII.GetBytes(szString);
            int dwCount = pBytesArray.Length;
            pBytes = Marshal.AllocCoTaskMem(dwCount);
            Marshal.Copy(pBytesArray, 0, pBytes, dwCount);
            bool success = SendBytesToPrinter(szPrinterName, pBytes, dwCount);
            Marshal.FreeCoTaskMem(pBytes);
            return success;
        }
    }
}

