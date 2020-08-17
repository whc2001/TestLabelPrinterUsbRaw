using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TestPrinter
{
    public static class FileStreamExtension
    {
        public static void PrinterWrite(this Stream stream, byte[] data)
        {
            stream.Write(data, 0, data.Length);
        }

        public static byte[] PrinterRead(this Stream stream, int timeOut = 250)
        {
            List<byte> read = new List<byte>();
            byte[] buf = new byte[256];
            int readBytes = 0;

            while (true)
            {
                Task<int> readTask = stream.ReadAsync(buf, 0, buf.Length);
                if (Task.WaitAny(new Task[] { readTask }, timeOut) != -1 && (readBytes = readTask.Result) > 0)
                    read.AddRange(buf.Take(readBytes));
                else
                    break;
            }
            return read.ToArray();
        }
    }

    class Program
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern SafeFileHandle CreateFile([MarshalAs(UnmanagedType.LPTStr)] string filename, [MarshalAs(UnmanagedType.U4)] FileAccess access, [MarshalAs(UnmanagedType.U4)] FileShare share, IntPtr securityAttributes, [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition, [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes, IntPtr templateFile);

        static void Main(string[] args)
        {
            // https://www.vbforums.com/showthread.php?770307-RESOLVED-VS2008-VS-2012-Win-7-USB-Printing-to-Zebra-Bidirectional-Communications-USB004
            // https://stackoverflow.com/a/13533429/8215816
            var ptr = CreateFile(@"\\?\USB#VID_6868&PID_0500&MI_00#6&1a69986f&0&0000#{28d78fad-5a12-11d1-ae5b-0000f803a8c2}", FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
            Console.WriteLine(ptr.DangerousGetHandle());

            FileStream stream = new FileStream(ptr, FileAccess.ReadWrite);

            // http://docs.gainscha.com:8181/docs/developer/developer-1c2evsb1a344e
            stream.PrinterWrite(Encoding.ASCII.GetBytes("SOUND 1,100\r\n"));
            stream.PrinterWrite(Encoding.ASCII.GetBytes("SIZE 40 mm,30 mm\r\n"));
            stream.PrinterWrite(Encoding.ASCII.GetBytes("CLS\r\n"));
            stream.PrinterWrite(Encoding.ASCII.GetBytes("HOME\r\n"));
            stream.PrinterWrite(Encoding.ASCII.GetBytes("TEXT 10,10,\"3\",0,1,1,\"Hello World\\[R]\\[A]Gainscha GP-2120TU\\[R]\\[A]USB RAW Printing\"\r\n"));
            stream.PrinterWrite(Encoding.ASCII.GetBytes("PRINT 1\r\n"));

            stream.PrinterWrite(new byte[3] { 0x1B, 0x21, 0x3F });
            Console.WriteLine(stream.PrinterRead()[0]);

            Console.ReadKey();
        }
    }
}
