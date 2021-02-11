using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace hashcheck
{
    public static partial class Extensions
    {
        /// <summary>
        ///     A string extension method that truncates.
        /// </summary>
        /// <param name="this">The @this to act on.</param>
        /// <param name="maxLength">The maximum length.</param>
        /// <returns>A string.</returns>
        public static string Truncate(this string @this, int maxLength)
        {
            int TestLength = Encoding.UTF8.GetBytes(@this).Length;
            if (TestLength >= maxLength)
            {
                return new string(@this.TakeWhile((c, i) => Encoding.UTF8.GetByteCount(@this.Substring(0, i + 1)) <= maxLength).ToArray());
            }
            else
            {
                return @this;
            }
        }

        private static string InvokeShellAndGetOutput(string fileName, string arguments)
        {
            Process p = new();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = fileName;
            p.StartInfo.Arguments = arguments;
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output;
        }

        enum OSType
        {
            windows, linux
        }

        [DllImport("stat.so", CharSet = CharSet.Unicode, EntryPoint = "GetInode", CallingConvention = CallingConvention.Cdecl)]
        static extern int GetInode([MarshalAs(UnmanagedType.LPStr)] StringBuilder FileName);

        public static long GetFileId(this FileInfo fileInfo)
        {
            long INode = 0;
            //try
            //{
            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;
            switch (pid)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE: //Windows
                    string output = InvokeShellAndGetOutput("fsutil", $"file queryfileid \"{fileInfo.FullName}\"");
                    string parsedOutput = output.Remove(0, 11).Trim();
                    //Sigh, a check for really long filenames that can't be read through normal means because of windows path limitations
                    if (parsedOutput == "system cannot find the path specified.") { INode = -1; } else { INode = Convert.ToInt64(parsedOutput, 16); }
                    break;
                case PlatformID.Unix: //Linux
                    StringBuilder x1 = new();
                    x1.Append(fileInfo.FullName);
                    int test = GetInode(x1);
                    INode = test;
                    break;
                case PlatformID.MacOSX: //Mac
                    break;
                default: //Other
                    break;
            }
            //}
            //catch (Exception) { }
            return INode;
        }
    }
}
