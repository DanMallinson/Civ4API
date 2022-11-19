using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PitBossScraper
{
    internal class Program
    {
        const int WM_GETTEXTLENGTH = 0x000E;
        const int WM_GETTEXT = 0x000D;
        public delegate bool Win32Callback(IntPtr hwnd, IntPtr lParam);

        private int WM_COMMAND = 0x0111;
        private const int BM_CLICKED = 245;

        [DllImport("user32.Dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr parentHandle, Win32Callback callback, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static public extern IntPtr GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, StringBuilder lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, uint msg, int wParam, IntPtr lParam);

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
        static void Main(string[] args)
        {
            var dictionary = new Dictionary<string, int>()
            {
                { "Turn",0 },
                //{ "Turn Timer Button", 1 },
                { "Time", 2 },
            };

            var close = false;
            Process pitbossHandle = null;

            //while (!close)
            {
                if (pitbossHandle == null)
                {
                    pitbossHandle = AquireHandle();
                }

                if(pitbossHandle == null)
                {
                    Console.WriteLine("Failed to aquire handle.");
                    //Thread.Sleep(60 * 1000);
                    //continue;
                }

                var window = pitbossHandle.MainWindowHandle;

                var children = GetChildWindows(window);

                /*
                for(var i = 0; i < children.Count; ++i)
                {
                    var child = children[i];
                    var childClass = GetWinClass(child);
                    var childName = new StringBuilder();
                    GetClassName(child, childName, 255);

                    int txtlen = (int)SendMessage(child, WM_GETTEXTLENGTH, 20, null);
                    StringBuilder text = new StringBuilder(txtlen);
                    int RetVal = (int)SendMessage(child, WM_GETTEXT, text.Capacity, text);

                    if (childName.Length > 0)
                    { }
                }
                */

                var nameAndYear = GetText(children[0]);
                var split = nameAndYear.Split('-');

                Console.WriteLine("Game Name: " + split[0].Trim());
                Console.WriteLine("Year     : " + split[1].Trim());
                Console.WriteLine("Time     : " + GetText(children[2]).Trim());

                Console.ReadLine();
            }


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
