using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;

namespace Civ4API
{
    public static class Worker
    {
        const int WM_GETTEXTLENGTH = 0x000E;
        const int WM_GETTEXT = 0x000D;
        public delegate bool Win32Callback(IntPtr hwnd, IntPtr lParam);

        const int WM_COMMAND = 0x0111;
        const int BM_CLICKED = 245;

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.Dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr parentHandle, Win32Callback callback, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static public extern IntPtr GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, StringBuilder lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, uint msg, int wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            List<IntPtr> list = gch.Target as List<IntPtr>;
            if (list == null)
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            list.Add(handle);
            return true;
        }

        public static List<IntPtr> GetChildWindows(IntPtr parent)
        {
            List<IntPtr> result = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(result);
            try
            {
                Win32Callback childProc = new Win32Callback(EnumWindow);
                EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }
            return result;
        }

        public static string GetWinClass(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return null;
            StringBuilder classname = new StringBuilder(100);
            IntPtr result = GetClassName(hwnd, classname, classname.Capacity);
            if (result != IntPtr.Zero)
                return classname.ToString();
            return null;
        }

        public static IEnumerable<IntPtr> EnumAllWindows(IntPtr hwnd, string childClassName)
        {
            List<IntPtr> children = GetChildWindows(hwnd);
            if (children == null)
                yield break;
            foreach (IntPtr child in children)
            {
                if (GetWinClass(child) == childClassName)
                    yield return child;
                foreach (var childchild in EnumAllWindows(child, childClassName))
                    yield return childchild;
            }
        }
        public static Tuple<Dictionary<string,string>,int> Scrape()
        {
            Process pitbossHandle = null;
            //while (!close)
            {
                if (pitbossHandle == null)
                {
                    pitbossHandle = AquireHandle();
                }

                if (pitbossHandle == null)
                {
                    Console.WriteLine("Failed to aquire handle.");
                    return new Tuple<Dictionary<string, string>, int>(new Dictionary<string, string>()
                    {
                        {"name", "" },
                        {"year", "" },
                        {"time", "" },
                    }, 500);
                    //Thread.Sleep(60 * 1000);
                    //continue;
                }

                var window = pitbossHandle.MainWindowHandle;

                var children = GetChildWindows(window);

                var nameAndYear = GetText(children[0]);
                var split = nameAndYear.Split('-');

                var results = new Dictionary<string, string>()
                {
                    {"name", split[0].Trim() },
                    {"year", split[1].Trim() },
                    {"time", GetText(children[2]).Trim() },
                };

                return new Tuple<Dictionary<string, string>, int>(results, 200);
            }




        }

        public static int SetTurnTimer(int value)
        {
            Process pitbossHandle = null;
            if (pitbossHandle == null)
            {
                pitbossHandle = AquireHandle();
            }

            if (pitbossHandle == null)
            {
                return 500;
            }
            var children = GetChildWindows(pitbossHandle.MainWindowHandle);

            SetForegroundWindow(pitbossHandle.MainWindowHandle);
            System.Threading.Thread.Sleep(1000);
            PostMessage(children[1], BM_CLICKED, IntPtr.Zero, IntPtr.Zero);
            System.Threading.Thread.Sleep(1000);
            System.Windows.Forms.SendKeys.SendWait("+{END}");
            System.Threading.Thread.Sleep(1000);
            System.Windows.Forms.SendKeys.SendWait(value+"\r");

            return 200;
        }

        static string GetText(IntPtr handle)
        {
            int txtlen = (int)SendMessage(handle, WM_GETTEXTLENGTH, 20, null);
            StringBuilder text = new StringBuilder(txtlen);
            int RetVal = (int)SendMessage(handle, WM_GETTEXT, text.Capacity, text);

            var output = text.ToString();
            return output.Substring(0, output.Length);
        }
        static Process AquireHandle()
        {
            var Processes = Process.GetProcessesByName("Civ4BeyondSword_PitBoss");

            if (Processes.Length > 0)
            {
                return Processes[0];
            }

            return null;
        }
    }
}