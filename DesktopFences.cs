using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

[assembly: System.Reflection.AssemblyTitle("Desktop Fences")]
[assembly: System.Reflection.AssemblyDescription("Untouched-Desktop Windows 10 & 11 Fence Organizer")]
[assembly: System.Reflection.AssemblyCompany("DesktopFences")]
[assembly: System.Reflection.AssemblyProduct("Desktop Fences")]
[assembly: System.Reflection.AssemblyVersion("3.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("3.0.0.0")]

namespace DesktopFences
{
    #region 1. Win32 Interop & OS Compatibility (Windows 10 & Windows 11 24H2+)

    internal static class NativeMethods
    {
        public const int GWL_EXSTYLE = -20;
        public const int GWLP_HWNDPARENT = -8;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_LAYERED = 0x00080000;

        public const int WM_WINDOWPOSCHANGING = 0x0046;
        public const int WM_WINDOWPOSCHANGED = 0x0047;
        public const int WM_ACTIVATE = 0x0006;
        public const int WM_ACTIVATEAPP = 0x001C;
        public const int WM_MOUSEACTIVATE = 0x0021;
        public const int MA_NOACTIVATE = 3;
        public const int WM_SYSCOMMAND = 0x0112;
        public const int WM_HOTKEY = 0x0312;
        public const int SC_MINIMIZE = 0xF020;

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;

        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        public const int SW_SHOWNA = 8;

        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        public const int DWMWCP_ROUND = 2;
        public const int DWMSBT_TRANSIENTWINDOW = 3;

        public const uint SHGFI_ICON = 0x000000100;
        public const uint SHGFI_LARGEICON = 0x000000000;
        public const uint SHGFI_PIDL = 0x000000008;
        public const uint SHGFI_DISPLAYNAME = 0x000000200;
        public const uint SHGFI_TYPENAME = 0x000000400;
        public const uint SHGFI_SYSICONINDEX = 0x000004000;
        public const uint SIGDN_NORMALDISPLAY = 0x00000000;
        public const uint SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000;
        public const int SHIL_EXTRALARGE = 0x02; // 48x48 32-bit ARGB
        public const int SHIL_JUMBO = 0x04;      // 256x256 32-bit ARGB HD
        public const int ILD_TRANSPARENT = 0x00000001;
        public const int ILD_IMAGE = 0x00000020;
        public const int SHCNE_DELETE = 0x00000004;
        public const int SHCNE_RMDIR = 0x00000010;
        public const int SHCNE_UPDATEDIR = 0x00001000;
        public const int SHCNE_UPDATEITEM = 0x00002000;
        public const uint SHCNF_PATHW = 0x0005;
        public const uint SHCNF_FLUSHNOWAIT = 0x2000;
        public const uint SEE_MASK_INVOKEIDLIST = 0x0000000C;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int RtlGetVersion(ref OSVERSIONINFOEX versionInfo);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("user32.dll")]
        public static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            return IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        public static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("shell32.dll", EntryPoint = "SHGetFileInfoW", CharSet = CharSet.Unicode)]
        public static extern IntPtr SHGetFileInfoPidl(IntPtr pidl, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHParseDisplayName(string pszName, IntPtr pbc, out IntPtr ppidl, uint sfgaoIn, out uint psfgaoOut);

        [DllImport("shell32.dll")]
        public static extern IntPtr ILCombine(IntPtr pidl1, IntPtr pidl2);

        [DllImport("shell32.dll")]
        public static extern void ILFree(IntPtr pidl);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHGetNameFromIDList(IntPtr pidl, uint sigdnName, out IntPtr ppszName);

        [DllImport("shell32.dll", EntryPoint = "SHEmptyRecycleBinW", CharSet = CharSet.Unicode)]
        public static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

        [DllImport("shell32.dll", EntryPoint = "#727")]
        public static extern int SHGetImageList(int iImageList, ref Guid riid, out IImageList ppv);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern void SHChangeNotify(int wEventId, uint uFlags, string dwItem1, string dwItem2);

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string psz1, string psz2);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGELISTDRAWPARAMS
        {
            public int cbSize;
            public IntPtr himl;
            public int i;
            public IntPtr hdcDst;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int xBitmap;
            public int yBitmap;
            public int rgbBk;
            public int rgbFg;
            public int fStyle;
            public int dwRop;
            public int fState;
            public int Frame;
            public int crEffect;
        }

        [ComImport]
        [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IImageList
        {
            [PreserveSig] int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);
            [PreserveSig] int ReplaceIcon(int i, IntPtr hicon, ref int pi);
            [PreserveSig] int SetOverlayImage(int iImage, int iOverlay);
            [PreserveSig] int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);
            [PreserveSig] int AddMasked(IntPtr hbmImage, int crMask, ref int pi);
            [PreserveSig] int Draw(ref IMAGELISTDRAWPARAMS pimldp);
            [PreserveSig] int Remove(int i);
            [PreserveSig] int GetIcon(int i, int flags, out IntPtr picon);
        }

        [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport, Guid("0000010b-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            [PreserveSig] int IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        // Outside-click selection clearing (selected icons un-highlight when clicking the desktop / other apps)
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct OSVERSIONINFOEX
        {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct AccentPolicy
        {
            public int AccentState;
            public int AccentFlags;
            public uint GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            public string pFrom;
            public string pTo;
            public ushort fFlags;
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }
    }

    public static class OsVersionHelper
    {
        public static int BuildNumber { get; private set; }
        public static bool IsWindows11 { get { return BuildNumber >= 22000; } }
        public static bool IsWindows11_22H2OrNewer { get { return BuildNumber >= 22621; } }

        static OsVersionHelper()
        {
            try
            {
                var osvi = new NativeMethods.OSVERSIONINFOEX();
                osvi.dwOSVersionInfoSize = Marshal.SizeOf(typeof(NativeMethods.OSVERSIONINFOEX));
                if (NativeMethods.RtlGetVersion(ref osvi) == 0 && osvi.dwBuildNumber > 0)
                {
                    BuildNumber = osvi.dwBuildNumber;
                    return;
                }
            }
            catch { }
            BuildNumber = Environment.OSVersion.Version.Build;
        }
    }

    /// <summary>
    /// Pins individual bounded Fence windows to HWND_BOTTOM and blocks Win+D minimize
    /// WITHOUT hiding or overlaying the native Windows desktop.
    /// </summary>
    public static class DesktopHostManager
    {
        public static bool IsPeekModeActive { get; set; }

        public static void ResolveDesktopHandles(out IntPtr hostHwnd, out IntPtr defViewHwnd, out IntPtr listViewHwnd)
        {
            IntPtr progman = NativeMethods.FindWindow("Progman", null);
            IntPtr defView = NativeMethods.FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            IntPtr resolvedHost = progman;

            if (defView == IntPtr.Zero)
            {
                NativeMethods.EnumWindows((topHwnd, _) =>
                {
                    IntPtr candidate = NativeMethods.FindWindowEx(topHwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (candidate != IntPtr.Zero)
                    {
                        defView = candidate;
                        resolvedHost = topHwnd;
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);
            }

            hostHwnd = resolvedHost;
            defViewHwnd = defView;
            listViewHwnd = defView != IntPtr.Zero
                ? NativeMethods.FindWindowEx(defView, IntPtr.Zero, "SysListView32", null)
                : IntPtr.Zero;
        }

        public static void EnsureNativeDesktopIconsVisible()
        {
            // Safety guarantee: ensure SHELLDLL_DefView and SysListView32 are 100% visible (SW_SHOW = 5) and repainted
            try
            {
                IntPtr hostHwnd, defView, listView;
                ResolveDesktopHandles(out hostHwnd, out defView, out listView);

                if (hostHwnd != IntPtr.Zero)
                    NativeMethods.ShowWindow(hostHwnd, 5);
                if (defView != IntPtr.Zero)
                {
                    NativeMethods.ShowWindow(defView, 5);
                    NativeMethods.InvalidateRect(defView, IntPtr.Zero, true);
                }
                if (listView != IntPtr.Zero)
                {
                    NativeMethods.ShowWindow(listView, 5);
                    if (!NativeMethods.IsWindowVisible(listView) && defView != IntPtr.Zero)
                    {
                        // Toggle Explorer's internal Show Desktop Icons command (0x7402) ONLY if still hidden
                        NativeMethods.SendMessage(defView, 0x0111, new IntPtr(0x7402), IntPtr.Zero);
                        NativeMethods.ShowWindow(listView, 5);
                    }
                    NativeMethods.InvalidateRect(listView, IntPtr.Zero, true);
                    NativeMethods.UpdateWindow(listView);
                }

                // Also walk all Progman / WorkerW child trees via EnumChildWindows to catch any secondary SysListView32
                NativeMethods.EnumWindows((topHwnd, _) =>
                {
                    var sb = new StringBuilder(128);
                    NativeMethods.GetClassName(topHwnd, sb, 128);
                    string cls = sb.ToString();
                    if (cls == "Progman" || cls == "WorkerW")
                    {
                        NativeMethods.EnumChildWindows(topHwnd, (childHwnd, __) =>
                        {
                            var csb = new StringBuilder(128);
                            NativeMethods.GetClassName(childHwnd, csb, 128);
                            string ccls = csb.ToString();
                            if (ccls == "SHELLDLL_DefView" || ccls == "SysListView32")
                            {
                                NativeMethods.ShowWindow(childHwnd, 5);
                                NativeMethods.InvalidateRect(childHwnd, IntPtr.Zero, true);
                                NativeMethods.UpdateWindow(childHwnd);
                            }
                            return true;
                        }, IntPtr.Zero);
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
        }

        public static void AttachToDesktopLayer(Window window)
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(window).EnsureHandle();
                IntPtr hostHwnd, defView, listView;
                ResolveDesktopHandles(out hostHwnd, out defView, out listView);

                long exStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
                // Use WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE so clicking or dragging an icon inside a fence
                // NEVER brings the fence window above the user's Web Browser or open apps!
                exStyle = exStyle | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;
                NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(exStyle));

                // Do NOT set GWLP_HWNDPARENT to Progman/WorkerW — otherwise clicking or dragging from a fence
                // causes Windows to raise Progman/WorkerW and all owned fences on top of the Web Browser,
                // making the browser look transparent with fence icons showing through!
                NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWLP_HWNDPARENT, IntPtr.Zero);
                UpdateZOrder(hwnd);

                HwndSource source = HwndSource.FromHwnd(hwnd);
                if (source != null)
                {
                    source.AddHook(DesktopWindowProc);
                }
            }
            catch { }
        }

        public static void UpdateZOrder(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;

            IntPtr targetZ = IsPeekModeActive ? NativeMethods.HWND_TOPMOST : NativeMethods.HWND_BOTTOM;
            NativeMethods.SetWindowPos(
                hwnd,
                targetZ,
                0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
        }

        public static void PushAllFencesToDesktopBottom()
        {
            if (IsPeekModeActive || App.Instance == null || App.Instance.ActiveWindows == null)
                return;

            foreach (var win in App.Instance.ActiveWindows)
            {
                try
                {
                    IntPtr h = new WindowInteropHelper(win).Handle;
                    if (h != IntPtr.Zero)
                        UpdateZOrder(h);
                }
                catch { }
            }
        }

        private static IntPtr DesktopWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_SYSCOMMAND)
            {
                int command = wParam.ToInt32() & 0xFFF0;
                if (command == NativeMethods.SC_MINIMIZE)
                {
                    handled = true;
                    return IntPtr.Zero;
                }
            }
            else if (msg == NativeMethods.WM_WINDOWPOSCHANGING && !IsPeekModeActive && lParam != IntPtr.Zero)
            {
                // Clamp fence strictly to HWND_BOTTOM whenever Windows tries to raise its Z-order during click or drag-and-drop!
                try
                {
                    var wp = (NativeMethods.WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(NativeMethods.WINDOWPOS));
                    if ((wp.flags & NativeMethods.SWP_NOZORDER) == 0 && wp.hwndInsertAfter != NativeMethods.HWND_BOTTOM)
                    {
                        wp.hwndInsertAfter = NativeMethods.HWND_BOTTOM;
                        wp.flags |= NativeMethods.SWP_NOACTIVATE;
                        Marshal.StructureToPtr(wp, lParam, false);
                    }
                }
                catch { }
            }
            else if ((msg == NativeMethods.WM_ACTIVATE || msg == NativeMethods.WM_ACTIVATEAPP) && !IsPeekModeActive)
            {
                UpdateZOrder(hwnd);
            }
            return IntPtr.Zero;
        }
    }

    public static class WindowBackdropManager
    {
        public static void ApplyFrostedGlass(Window window, bool isDarkMode, bool enableAcrylic = true)
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(window).EnsureHandle();
                int darkFlag = isDarkMode ? 1 : 0;
                NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkFlag, sizeof(int));

                // NOTE: Because FenceWindow uses AllowsTransparency = true (true 32-bit Premultiplied ARGB per-pixel alpha),
                // we do NOT call Win10 SetWindowCompositionAttribute(ACCENT_ENABLE_ACRYLICBLURBEHIND), which on Win10
                // washes out child icon colors by bleeding wallpaper blur through pixels and draws rectangular corners.
                // With pure 32-bit per-pixel WPF composition, every app icon renders at Alpha=255 100% vivid HD color!
                if (OsVersionHelper.IsWindows11)
                {
                    int cornerPref = NativeMethods.DWMWCP_ROUND;
                    NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));
                }
            }
            catch { }
        }

        private static void ApplyWin10CompositionBlur(IntPtr hwnd, int accentState, uint abgrTint)
        {
            var accent = new NativeMethods.AccentPolicy
            {
                AccentState = accentState,
                AccentFlags = 2,
                GradientColor = abgrTint
            };
            int size = Marshal.SizeOf(accent);
            IntPtr accentPtr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(accent, accentPtr, false);
                var data = new NativeMethods.WindowCompositionAttributeData
                {
                    Attribute = 19,
                    SizeOfData = size,
                    Data = accentPtr
                };
                NativeMethods.SetWindowCompositionAttribute(hwnd, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }
    }

    #endregion

    #region 2. 10 Dual-Mode Fence Color Themes (Including Navy Blue & Rose Pink)

    public class FenceThemeDefinition
    {
        public string Name { get; set; }
        public string AccentHex { get; set; }
        public string DarkBodyHex { get; set; }
        public string LightBodyHex { get; set; }
        public bool IsTransparentHover { get; set; }
    }

    public static class ThemeCatalog
    {
        public static readonly FenceThemeDefinition[] Themes = new[]
        {
            new FenceThemeDefinition { Name = "Transparent",      AccentHex = "#60A5FA", DarkBodyHex = "#0D131F", LightBodyHex = "#F8FAFC", IsTransparentHover = true },
            new FenceThemeDefinition { Name = "Navy Blue",        AccentHex = "#1E3A8A", DarkBodyHex = "#101935", LightBodyHex = "#E8EFFD" },
            new FenceThemeDefinition { Name = "Rose Pink",        AccentHex = "#EC4899", DarkBodyHex = "#2D1222", LightBodyHex = "#FCEBF4" },
            new FenceThemeDefinition { Name = "Midnight Ocean",   AccentHex = "#3B82F6", DarkBodyHex = "#111C33", LightBodyHex = "#EBF3FE" },
            new FenceThemeDefinition { Name = "Obsidian Slate",   AccentHex = "#64748B", DarkBodyHex = "#141720", LightBodyHex = "#F3F4F6" },
            new FenceThemeDefinition { Name = "Emerald Moss",     AccentHex = "#10B981", DarkBodyHex = "#0F2620", LightBodyHex = "#E9FBF4" },
            new FenceThemeDefinition { Name = "Royal Amethyst",   AccentHex = "#8B5CF6", DarkBodyHex = "#1E1533", LightBodyHex = "#F2EEFE" },
            new FenceThemeDefinition { Name = "Sunset Amber",     AccentHex = "#F59E0B", DarkBodyHex = "#2B1F0E", LightBodyHex = "#FEF5E7" },
            new FenceThemeDefinition { Name = "Crimson Ruby",     AccentHex = "#F43F5E", DarkBodyHex = "#2D1219", LightBodyHex = "#FEECEF" },
            new FenceThemeDefinition { Name = "Nordic Ice",       AccentHex = "#06B6D4", DarkBodyHex = "#0E242B", LightBodyHex = "#E8FAFD" },
            new FenceThemeDefinition { Name = "Stealth Glass",    AccentHex = "#94A3B8", DarkBodyHex = "#0D0F14", LightBodyHex = "#F8FAFC" }
        };

        public static FenceThemeDefinition GetTheme(string nameOrHex)
        {
            if (!string.IsNullOrEmpty(nameOrHex))
            {
                var byName = Themes.FirstOrDefault(t => string.Equals(t.Name, nameOrHex, StringComparison.OrdinalIgnoreCase));
                if (byName != null) return byName;

                var byHex = Themes.FirstOrDefault(t => string.Equals(t.AccentHex, nameOrHex, StringComparison.OrdinalIgnoreCase));
                if (byHex != null) return byHex;
            }
            return Themes[0]; // Default Navy Blue
        }

        public static Color ParseColor(string hex, Color fallback)
        {
            try { return (Color)ColorConverter.ConvertFromString(hex); }
            catch { return fallback; }
        }
    }

    #endregion

    #region 3. Models, Real Managed Fence Folders & Persistence

    public class FenceConfigRoot
    {
        public bool IsDarkMode { get; set; }
        public bool SyncWithWindowsTheme { get; set; }
        public bool LockLayout { get; set; }
        public int IconSizePx { get; set; }
        public double FenceOpacity { get; set; }
        public int SchemaVersion { get; set; }
        public List<FenceModel> Fences { get; set; }

        public FenceConfigRoot()
        {
            IsDarkMode = true;
            SyncWithWindowsTheme = false;
            LockLayout = false;
            IconSizePx = 42;
            FenceOpacity = 0.82;
            SchemaVersion = 3;
            Fences = new List<FenceModel>();
        }
    }

    public class FenceModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double ExpandedHeight { get; set; }
        public bool IsRolledUp { get; set; }
        public bool ExcludeFromQuickHide { get; set; }
        public string ThemeName { get; set; }
        public string CustomColorHex { get; set; }
        public string SortBy { get; set; }
        public bool SortDescending { get; set; }
        public bool HoldPosition { get; set; }
        public string MoveMode { get; set; }
        public List<string> OrderedFileNames { get; set; }

        public FenceModel()
        {
            Id = Guid.NewGuid().ToString("N");
            Title = "New Fence";
            Left = 450;
            Top = 90;
            Width = 380;
            Height = 270;
            ExpandedHeight = 270;
            ThemeName = "Transparent";
            CustomColorHex = "";
            SortBy = "None";
            SortDescending = false;
            HoldPosition = false;
            MoveMode = "Anywhere";
            OrderedFileNames = new List<string>();
        }
    }

    public class DesktopIconItem
    {
        public string FullPath { get; set; }
        public string FileName { get; set; }
        public string DisplayName { get; set; }
        public ImageSource IconSource { get; set; }
        public bool IsFolder { get; set; }
    }

    public static class ConfigManager
    {
        public static FenceConfigRoot Current { get; private set; }
        public static string ConfigDirectory { get; private set; }
        public static string FencesStorageRoot { get; private set; }
        public static string ConfigFilePath { get; private set; }
        public static string SnapshotsDirectory { get; private set; }

        static ConfigManager()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string portableMarker = System.IO.Path.Combine(exeDir, "portable.mode");
            if (File.Exists(portableMarker))
            {
                ConfigDirectory = System.IO.Path.Combine(exeDir, "Data");
            }
            else
            {
                ConfigDirectory = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "DesktopFences");
            }

            Directory.CreateDirectory(ConfigDirectory);
            FencesStorageRoot = System.IO.Path.Combine(ConfigDirectory, "Fences");
            Directory.CreateDirectory(FencesStorageRoot);
            SnapshotsDirectory = System.IO.Path.Combine(ConfigDirectory, "Snapshots");
            Directory.CreateDirectory(SnapshotsDirectory);
            ConfigFilePath = System.IO.Path.Combine(ConfigDirectory, "layout.json");
        }

        public static string GetFenceFolderPath(string fenceId)
        {
            string dir = System.IO.Path.Combine(FencesStorageRoot, fenceId);
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
                    var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                    var loaded = serializer.Deserialize<FenceConfigRoot>(json);
                    if (loaded != null && loaded.Fences != null && loaded.Fences.Count > 0)
                    {
                        foreach (var f in loaded.Fences)
                        {
                            if (string.IsNullOrEmpty(f.ThemeName))
                                f.ThemeName = "Navy Blue";
                            if (f.OrderedFileNames == null)
                                f.OrderedFileNames = new List<string>();
                            GetFenceFolderPath(f.Id);
                        }
                        Current = loaded;
                        Save();
                        return;
                    }
                }
            }
            catch { }

            Current = CreateDefaultStarterConfig();
            Save(createDailySnapshot: true);
        }

        private static readonly object SaveLock = new object();

        public static void SaveAsync()
        {
            ThreadPool.QueueUserWorkItem(_ => Save(false));
        }

        public static void Save(bool createDailySnapshot = false)
        {
            try
            {
                lock (SaveLock)
                {
                    var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                    string json = serializer.Serialize(Current);
                    File.WriteAllText(ConfigFilePath, json, Encoding.UTF8);

                    if (createDailySnapshot)
                    {
                        string snapFile = System.IO.Path.Combine(
                            SnapshotsDirectory,
                            "snapshot-" + DateTime.Now.ToString("yyyy-MM-dd") + ".json");
                        if (!File.Exists(snapFile))
                        {
                            File.WriteAllText(snapFile, json, Encoding.UTF8);
                        }
                    }
                }
            }
            catch { }
        }

        private static FenceConfigRoot CreateDefaultStarterConfig()
        {
            var root = new FenceConfigRoot();
            var f1 = new FenceModel
            {
                Title = "Work & Apps",
                Left = 340,
                Top = 50,
                Width = 340,
                Height = 240,
                ExpandedHeight = 240,
                ThemeName = "Navy Blue"
            };
            var f2 = new FenceModel
            {
                Title = "Creative & Media",
                Left = 700,
                Top = 50,
                Width = 340,
                Height = 240,
                ExpandedHeight = 240,
                ThemeName = "Rose Pink"
            };
            var f3 = new FenceModel
            {
                Title = "Documents & Files",
                Left = 340,
                Top = 310,
                Width = 340,
                Height = 230,
                ExpandedHeight = 230,
                ThemeName = "Emerald Moss"
            };
            GetFenceFolderPath(f1.Id);
            GetFenceFolderPath(f2.Id);
            GetFenceFolderPath(f3.Id);
            root.Fences.Add(f1);
            root.Fences.Add(f2);
            root.Fences.Add(f3);
            return root;
        }
    }

    #endregion

    #region 4. Zero-Duplicate Real File Transfer & Fail-Safe Desktop Restoration

    public class VirtualDesktopItemInfo
    {
        public string Clsid { get; set; }
        public string ParsingName { get; set; }
        public string DisplayName { get; set; }
    }

    public static class VirtualDesktopShellHelper
    {
        public const string VirtualExt = ".vfence";
        private static readonly Regex GuidRegex = new Regex(@"\{[0-9A-Fa-f]{8}\-[0-9A-Fa-f]{4}\-[0-9A-Fa-f]{4}\-[0-9A-Fa-f]{4}\-[0-9A-Fa-f]{12}\}", RegexOptions.Compiled);

        public static bool IsVirtualItemFile(string path)
        {
            return !string.IsNullOrEmpty(path) && path.EndsWith(VirtualExt, StringComparison.OrdinalIgnoreCase);
        }

        public static VirtualDesktopItemInfo ReadVirtualFile(string path)
        {
            if (!IsVirtualItemFile(path) || !File.Exists(path))
                return null;

            try
            {
                var info = new VirtualDesktopItemInfo();
                foreach (string rawLine in File.ReadAllLines(path, Encoding.UTF8))
                {
                    int eq = rawLine.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = rawLine.Substring(0, eq).Trim();
                    string v = rawLine.Substring(eq + 1).Trim();
                    if (string.Equals(k, "CLSID", StringComparison.OrdinalIgnoreCase)) info.Clsid = v;
                    else if (string.Equals(k, "ParsingName", StringComparison.OrdinalIgnoreCase)) info.ParsingName = v;
                    else if (string.Equals(k, "DisplayName", StringComparison.OrdinalIgnoreCase)) info.DisplayName = v;
                }
                if (string.IsNullOrEmpty(info.ParsingName) && !string.IsNullOrEmpty(info.Clsid))
                    info.ParsingName = "::" + info.Clsid;
                if (string.IsNullOrEmpty(info.DisplayName))
                    info.DisplayName = System.IO.Path.GetFileNameWithoutExtension(path);
                return info;
            }
            catch { return null; }
        }

        public static List<VirtualDesktopItemInfo> ParseShellIdListArray(IDataObject dataObj, out List<string> filePathsFallback)
        {
            filePathsFallback = new List<string>();
            var virtualItems = new List<VirtualDesktopItemInfo>();
            if (dataObj == null || !dataObj.GetDataPresent("Shell IDList Array"))
                return virtualItems;

            try
            {
                var ms = dataObj.GetData("Shell IDList Array") as MemoryStream;
                if (ms == null) return virtualItems;

                byte[] bytes = ms.ToArray();
                if (bytes.Length < 8) return virtualItems;

                GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                try
                {
                    IntPtr basePtr = handle.AddrOfPinnedObject();
                    uint count = BitConverter.ToUInt32(bytes, 0);
                    if (count == 0 || bytes.Length < 4 + (count + 1) * 4)
                        return virtualItems;

                    uint parentOffset = BitConverter.ToUInt32(bytes, 4);
                    IntPtr pidlParent = IntPtr.Add(basePtr, (int)parentOffset);

                    for (uint i = 1; i <= count; i++)
                    {
                        uint childOffset = BitConverter.ToUInt32(bytes, (int)(4 + i * 4));
                        IntPtr pidlChild = IntPtr.Add(basePtr, (int)childOffset);
                        IntPtr pidlFull = NativeMethods.ILCombine(pidlParent, pidlChild);
                        if (pidlFull == IntPtr.Zero) continue;

                        try
                        {
                            string parsingName = GetNameFromPidl(pidlFull, NativeMethods.SIGDN_DESKTOPABSOLUTEPARSING);
                            string displayName = GetNameFromPidl(pidlFull, NativeMethods.SIGDN_NORMALDISPLAY);

                            if (!string.IsNullOrEmpty(parsingName) && (File.Exists(parsingName) || Directory.Exists(parsingName)))
                            {
                                filePathsFallback.Add(parsingName);
                                continue;
                            }

                            if (!string.IsNullOrEmpty(parsingName))
                            {
                                var matches = GuidRegex.Matches(parsingName);
                                if (matches.Count > 0)
                                {
                                    string clsid = matches[matches.Count - 1].Value;
                                    if (string.IsNullOrWhiteSpace(displayName))
                                        displayName = "Desktop Icon";

                                    virtualItems.Add(new VirtualDesktopItemInfo
                                    {
                                        Clsid = clsid,
                                        ParsingName = "::" + clsid,
                                        DisplayName = displayName
                                    });
                                }
                            }
                        }
                        finally
                        {
                            NativeMethods.ILFree(pidlFull);
                        }
                    }
                }
                finally
                {
                    handle.Free();
                }
            }
            catch { }

            return virtualItems;
        }

        private static string GetNameFromPidl(IntPtr pidl, uint sigdn)
        {
            IntPtr ptr = IntPtr.Zero;
            try
            {
                if (NativeMethods.SHGetNameFromIDList(pidl, sigdn, out ptr) == 0 && ptr != IntPtr.Zero)
                    return Marshal.PtrToStringUni(ptr);
            }
            catch { }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(ptr);
            }
            return null;
        }

        public static void SetNativeDesktopClsidHidden(string clsid, bool hideFromNativeDesktop)
        {
            if (string.IsNullOrWhiteSpace(clsid)) return;

            string[] subKeys = new[]
            {
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel",
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\ClassicStartMenu"
            };

            foreach (string subKeyPath in subKeys)
            {
                try
                {
                    using (var key = Registry.CurrentUser.CreateSubKey(subKeyPath))
                    {
                        if (key == null) continue;

                        // Preserve existing case if value already exists, otherwise write lowercase & standard form
                        string targetName = clsid;
                        foreach (string existingName in key.GetValueNames())
                        {
                            if (string.Equals(existingName, clsid, StringComparison.OrdinalIgnoreCase))
                            {
                                targetName = existingName;
                                break;
                            }
                        }
                        key.SetValue(targetName, hideFromNativeDesktop ? 1 : 0, RegistryValueKind.DWord);
                    }
                }
                catch { }
            }

            RefreshNativeDesktopShell();
        }

        public static void RefreshNativeDesktopShell()
        {
            try
            {
                IntPtr host, defView, listView;
                DesktopHostManager.ResolveDesktopHandles(out host, out defView, out listView);
                if (defView != IntPtr.Zero)
                {
                    // 0x7103 = FCIDM_SHVIEW_REFRESH (Native Explorer Desktop F5 Refresh)
                    NativeMethods.PostMessage(defView, 0x0111, new IntPtr(0x7103), IntPtr.Zero);
                }
                NativeMethods.SHChangeNotify(0x08000000, 0x2000, null, null);
            }
            catch { }
        }

        public static void InvokeVirtualVerb(VirtualDesktopItemInfo vInfo, string verb)
        {
            if (vInfo == null) return;

            // Special handling for Windows Spotlight ("Learn about this picture") verbs & Recycle Bin
            if (!string.IsNullOrEmpty(verb) &&
                string.Equals(vInfo.Clsid, "{2cc5ca98-6485-489a-920e-b3e88a6ccce3}", StringComparison.OrdinalIgnoreCase))
            {
                InvokePidlVerb(vInfo.ParsingName, verb);
                return;
            }

            if (string.IsNullOrEmpty(verb))
            {
                if (InvokePidlVerb(vInfo.ParsingName, null))
                    return;

                try
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", "shell:" + vInfo.ParsingName) { UseShellExecute = true });
                }
                catch { }
            }
            else
            {
                InvokePidlVerb(vInfo.ParsingName, verb);
            }
        }

        private static bool InvokePidlVerb(string parsingName, string verb)
        {
            IntPtr pidl = IntPtr.Zero;
            try
            {
                uint attrs;
                if (NativeMethods.SHParseDisplayName(parsingName, IntPtr.Zero, out pidl, 0, out attrs) == 0 && pidl != IntPtr.Zero)
                {
                    var sei = new NativeMethods.SHELLEXECUTEINFO();
                    sei.cbSize = Marshal.SizeOf(sei);
                    sei.fMask = NativeMethods.SEE_MASK_INVOKEIDLIST;
                    sei.lpVerb = verb;
                    sei.lpIDList = pidl;
                    sei.nShow = 1;
                    return NativeMethods.ShellExecuteEx(ref sei);
                }
            }
            catch { }
            finally
            {
                if (pidl != IntPtr.Zero)
                    NativeMethods.ILFree(pidl);
            }
            return false;
        }
    }

    public static class FenceFileTransferService
    {
        public static bool MoveVirtualItemIntoFence(VirtualDesktopItemInfo vInfo, FenceModel targetFence)
        {
            if (vInfo == null || string.IsNullOrEmpty(vInfo.Clsid) || targetFence == null)
                return false;

            try
            {
                // Remove any existing .vfence for this CLSID across all fences so zero duplicates ever exist
                if (Directory.Exists(ConfigManager.FencesStorageRoot))
                {
                    foreach (string fenceDir in Directory.GetDirectories(ConfigManager.FencesStorageRoot))
                    {
                        foreach (string vf in Directory.GetFiles(fenceDir, "*" + VirtualDesktopShellHelper.VirtualExt))
                        {
                            var existing = VirtualDesktopShellHelper.ReadVirtualFile(vf);
                            if (existing != null && string.Equals(existing.Clsid, vInfo.Clsid, StringComparison.OrdinalIgnoreCase))
                            {
                                try { File.Delete(vf); } catch { }
                            }
                        }
                    }
                }

                string safeTitle = string.Join("_", (vInfo.DisplayName ?? "Desktop Icon").Split(System.IO.Path.GetInvalidFileNameChars())).Trim();
                if (string.IsNullOrEmpty(safeTitle)) safeTitle = "Desktop Icon";

                string targetDir = ConfigManager.GetFenceFolderPath(targetFence.Id);
                string fileName = safeTitle + VirtualDesktopShellHelper.VirtualExt;
                string destPath = System.IO.Path.Combine(targetDir, fileName);

                string content =
                    "CLSID=" + vInfo.Clsid + "\r\n" +
                    "ParsingName=" + vInfo.ParsingName + "\r\n" +
                    "DisplayName=" + vInfo.DisplayName + "\r\n";
                File.WriteAllText(destPath, content, Encoding.UTF8);

                if (!targetFence.OrderedFileNames.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                    targetFence.OrderedFileNames.Add(fileName);

                // Hide the virtual icon on the native Windows Desktop so zero duplicate remains on wallpaper!
                VirtualDesktopShellHelper.SetNativeDesktopClsidHidden(vInfo.Clsid, true);
                return true;
            }
            catch { return false; }
        }

        public static void SweepDroppedVirtualFilesOnDesktop()
        {
            try
            {
                string userDesktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (!Directory.Exists(userDesktop)) return;

                foreach (string vf in Directory.GetFiles(userDesktop, "*" + VirtualDesktopShellHelper.VirtualExt))
                {
                    var vInfo = VirtualDesktopShellHelper.ReadVirtualFile(vf);
                    try { File.Delete(vf); } catch { }
                    if (vInfo != null && !string.IsNullOrEmpty(vInfo.Clsid))
                    {
                        VirtualDesktopShellHelper.SetNativeDesktopClsidHidden(vInfo.Clsid, false);
                    }
                }
            }
            catch { }
        }
        /// <summary>
        /// Moves a file or directory safely into a Fence's managed folder on disk.
        /// Because it is MOVED from %USERPROFILE%\Desktop into the Fence folder,
        /// Windows Explorer removes it from the native desktop immediately (0 duplicates).
        /// </summary>
        public static bool MoveItemIntoFence(string sourcePath, FenceModel targetFence)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourcePath)) return false;
                string fullSource = System.IO.Path.GetFullPath(sourcePath);
                if (!File.Exists(fullSource) && !Directory.Exists(fullSource)) return false;

                string targetDir = ConfigManager.GetFenceFolderPath(targetFence.Id);
                string sourceDir = System.IO.Path.GetDirectoryName(fullSource);
                if (string.Equals(sourceDir, targetDir, StringComparison.OrdinalIgnoreCase))
                    return false; // Already in this fence

                string publicDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
                if (!string.IsNullOrEmpty(publicDesktop) &&
                    fullSource.StartsWith(publicDesktop, StringComparison.OrdinalIgnoreCase))
                {
                    var res = MessageBox.Show(
                        "'" + System.IO.Path.GetFileName(fullSource) + "' is a Public Desktop shortcut shared by all users.\n\n" +
                        "Moving it into a fence requires Administrator permission. Try moving it with elevation?",
                        "Public Desktop Shortcut",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (res == MessageBoxResult.Yes)
                    {
                        string destPublic = GetConflictSafeDestination(targetDir, System.IO.Path.GetFileName(fullSource));
                        return MoveWithElevation(fullSource, destPublic);
                    }
                    return false;
                }

                string fileName = System.IO.Path.GetFileName(fullSource);
                string destPath = GetConflictSafeDestination(targetDir, fileName);
                bool isDir = Directory.Exists(fullSource);

                // Pre-transfer cached HD icon before moving so rendering after move takes 0.0ms!
                ShellIconExtractor.WarmAndTransferCache(fullSource, destPath);

                if (isDir)
                    Directory.Move(fullSource, destPath);
                else
                    File.Move(fullSource, destPath);

                // Immediately notify Windows Explorer so the icon vanishes from the native Desktop in <16ms!
                try
                {
                    NativeMethods.SHChangeNotify(
                        isDir ? NativeMethods.SHCNE_RMDIR : NativeMethods.SHCNE_DELETE,
                        NativeMethods.SHCNF_PATHW | NativeMethods.SHCNF_FLUSHNOWAIT,
                        fullSource,
                        null);
                }
                catch { }

                string finalName = System.IO.Path.GetFileName(destPath);
                if (!targetFence.OrderedFileNames.Contains(finalName, StringComparer.OrdinalIgnoreCase))
                    targetFence.OrderedFileNames.Add(finalName);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not move item into fence:\n" + ex.Message, "Desktop Fences", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        /// <summary>
        /// Moves a single fenced item safely back to %USERPROFILE%\Desktop with conflict-safe naming.
        /// </summary>
        public static bool RestoreItemToUserDesktop(string fencedItemPath)
        {
            try
            {
                if (!File.Exists(fencedItemPath) && !Directory.Exists(fencedItemPath))
                    return false;

                if (VirtualDesktopShellHelper.IsVirtualItemFile(fencedItemPath))
                {
                    var vInfo = VirtualDesktopShellHelper.ReadVirtualFile(fencedItemPath);
                    try { File.Delete(fencedItemPath); } catch { }
                    if (vInfo != null && !string.IsNullOrEmpty(vInfo.Clsid))
                    {
                        VirtualDesktopShellHelper.SetNativeDesktopClsidHidden(vInfo.Clsid, false);
                    }
                    return true;
                }

                string userDesktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string fileName = System.IO.Path.GetFileName(fencedItemPath);
                string destPath = GetConflictSafeDestination(userDesktop, fileName);
                bool isDir = Directory.Exists(fencedItemPath);

                ShellIconExtractor.WarmAndTransferCache(fencedItemPath, destPath);

                if (isDir)
                    Directory.Move(fencedItemPath, destPath);
                else
                    File.Move(fencedItemPath, destPath);

                try
                {
                    NativeMethods.SHChangeNotify(
                        NativeMethods.SHCNE_UPDATEDIR,
                        NativeMethods.SHCNF_PATHW | NativeMethods.SHCNF_FLUSHNOWAIT,
                        userDesktop,
                        null);
                }
                catch { }

                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Fail-safe method: Restores ALL items from a specific fence folder back to the native Windows Desktop.
        /// </summary>
        public static void RestoreFenceFolderToDesktop(string fenceId)
        {
            try
            {
                string dir = ConfigManager.GetFenceFolderPath(fenceId);
                if (!Directory.Exists(dir)) return;

                foreach (string subDir in Directory.GetDirectories(dir))
                    RestoreItemToUserDesktop(subDir);

                foreach (string file in Directory.GetFiles(dir))
                    RestoreItemToUserDesktop(file);
            }
            catch { }
        }

        /// <summary>
        /// Fail-safe method: Restores ALL items across every fence folder back to %USERPROFILE%\Desktop.
        /// Called on "Restore All Icons to Desktop" or Uninstaller (--restore-icons).
        /// </summary>
        public static int RestoreAllFencesToDesktop()
        {
            int count = 0;
            try
            {
                if (!Directory.Exists(ConfigManager.FencesStorageRoot))
                    return 0;

                foreach (string fenceDir in Directory.GetDirectories(ConfigManager.FencesStorageRoot))
                {
                    foreach (string subDir in Directory.GetDirectories(fenceDir))
                    {
                        if (RestoreItemToUserDesktop(subDir)) count++;
                    }
                    foreach (string file in Directory.GetFiles(fenceDir))
                    {
                        if (RestoreItemToUserDesktop(file)) count++;
                    }
                }
            }
            catch { }
            return count;
        }

        public static string GetConflictSafeDestination(string targetFolder, string fileName)
        {
            string candidate = System.IO.Path.Combine(targetFolder, fileName);
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
                return candidate;

            string baseName = System.IO.Path.GetFileNameWithoutExtension(fileName);
            string ext = System.IO.Path.GetExtension(fileName);
            int counter = 2;
            while (true)
            {
                string next = System.IO.Path.Combine(targetFolder, baseName + " (" + counter + ")" + ext);
                if (!File.Exists(next) && !Directory.Exists(next))
                    return next;
                counter++;
            }
        }

        private static bool MoveWithElevation(string src, string dst)
        {
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", "/c move /Y \"" + src + "\" \"" + dst + "\"")
                {
                    Verb = "runas",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                var p = Process.Start(psi);
                if (p != null) p.WaitForExit(5000);
                return File.Exists(dst) || Directory.Exists(dst);
            }
            catch { return false; }
        }
    }

    public static class ShellIconExtractor
    {
        private static readonly Dictionary<string, ImageSource> PathCache = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<int, ImageSource> SysIndexCache = new Dictionary<int, ImageSource>();
        private static readonly Dictionary<string, string> ExtTypeNameCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static NativeMethods.IImageList _cachedJumboList;
        private static NativeMethods.IImageList _cachedExtraLargeList;
        private static bool _imageListsInitialized;

        public static void EnsureImageLists()
        {
            if (_imageListsInitialized) return;
            _imageListsInitialized = true;
            try
            {
                Guid iidImageList = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
                NativeMethods.SHGetImageList(NativeMethods.SHIL_JUMBO, ref iidImageList, out _cachedJumboList);
                NativeMethods.SHGetImageList(NativeMethods.SHIL_EXTRALARGE, ref iidImageList, out _cachedExtraLargeList);
            }
            catch { }
        }

        public static void WarmAndTransferCache(string oldPath, string newPath)
        {
            if (string.IsNullOrEmpty(oldPath) || string.IsNullOrEmpty(newPath))
                return;

            ImageSource existing;
            if (!PathCache.TryGetValue(oldPath, out existing))
            {
                existing = ExtractHighDpiIcon(oldPath);
            }

            if (existing != null)
            {
                PathCache[newPath] = existing;
                PathCache.Remove(oldPath);
            }
        }

        public static DesktopIconItem CreateItem(string path)
        {
            bool isDir = Directory.Exists(path);
            string fileName = System.IO.Path.GetFileName(path);
            string displayName = fileName;

            if (VirtualDesktopShellHelper.IsVirtualItemFile(path))
            {
                var vInfo = VirtualDesktopShellHelper.ReadVirtualFile(path);
                displayName = (vInfo != null && !string.IsNullOrEmpty(vInfo.DisplayName))
                    ? vInfo.DisplayName
                    : System.IO.Path.GetFileNameWithoutExtension(path);
            }
            else if (!isDir && (path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".url", StringComparison.OrdinalIgnoreCase)))
            {
                displayName = System.IO.Path.GetFileNameWithoutExtension(path);
            }

            ImageSource icon;
            if (!PathCache.TryGetValue(path, out icon))
            {
                icon = ExtractHighDpiIcon(path);
                if (icon != null)
                    PathCache[path] = icon;
            }

            return new DesktopIconItem
            {
                FullPath = path,
                FileName = fileName,
                DisplayName = string.IsNullOrEmpty(displayName) ? path : displayName,
                IconSource = icon,
                IsFolder = isDir
            };
        }

        public static void InvalidateCache(string path)
        {
            if (!string.IsNullOrEmpty(path))
                PathCache.Remove(path);
        }

        public static string GetItemTypeName(string path)
        {
            if (VirtualDesktopShellHelper.IsVirtualItemFile(path))
                return "System Desktop Icon";
            if (Directory.Exists(path))
                return "File folder";

            string ext = System.IO.Path.GetExtension(path) ?? "";
            string cacheKey = string.IsNullOrEmpty(ext) ? path : ext;
            string cachedType;
            if (ExtTypeNameCache.TryGetValue(cacheKey, out cachedType))
                return cachedType;

            try
            {
                var shfi = new NativeMethods.SHFILEINFO();
                IntPtr res = NativeMethods.SHGetFileInfo(
                    path,
                    0,
                    ref shfi,
                    (uint)Marshal.SizeOf(shfi),
                    NativeMethods.SHGFI_TYPENAME);
                if (res != IntPtr.Zero && !string.IsNullOrWhiteSpace(shfi.szTypeName))
                {
                    ExtTypeNameCache[cacheKey] = shfi.szTypeName;
                    return shfi.szTypeName;
                }
            }
            catch { }

            string fallback = string.IsNullOrEmpty(ext) ? "File" : ext.TrimStart('.').ToUpperInvariant() + " File";
            ExtTypeNameCache[cacheKey] = fallback;
            return fallback;
        }

        private static ImageSource ExtractHighDpiIcon(string path)
        {
            try
            {
                var shfi = new NativeMethods.SHFILEINFO();
                IntPtr res = IntPtr.Zero;

                if (VirtualDesktopShellHelper.IsVirtualItemFile(path))
                {
                    var vInfo = VirtualDesktopShellHelper.ReadVirtualFile(path);
                    if (vInfo != null && !string.IsNullOrEmpty(vInfo.ParsingName))
                    {
                        IntPtr pidl = IntPtr.Zero;
                        uint attrs;
                        if (NativeMethods.SHParseDisplayName(vInfo.ParsingName, IntPtr.Zero, out pidl, 0, out attrs) == 0 && pidl != IntPtr.Zero)
                        {
                            try
                            {
                                res = NativeMethods.SHGetFileInfoPidl(
                                    pidl,
                                    0,
                                    ref shfi,
                                    (uint)Marshal.SizeOf(shfi),
                                    NativeMethods.SHGFI_PIDL | NativeMethods.SHGFI_SYSICONINDEX);
                            }
                            finally
                            {
                                NativeMethods.ILFree(pidl);
                            }
                        }
                    }
                }
                else
                {
                    res = NativeMethods.SHGetFileInfo(
                        path,
                        0,
                        ref shfi,
                        (uint)Marshal.SizeOf(shfi),
                        NativeMethods.SHGFI_SYSICONINDEX);
                }

                if (res != IntPtr.Zero)
                {
                    ImageSource byIndex;
                    if (shfi.iIcon > 0 && SysIndexCache.TryGetValue(shfi.iIcon, out byIndex))
                        return byIndex;

                    EnsureImageLists();

                    if (_cachedJumboList != null)
                    {
                        IntPtr hJumbo = IntPtr.Zero;
                        if (_cachedJumboList.GetIcon(shfi.iIcon, NativeMethods.ILD_TRANSPARENT | NativeMethods.ILD_IMAGE, out hJumbo) == 0 && hJumbo != IntPtr.Zero)
                        {
                            try
                            {
                                var jumboBmp = Imaging.CreateBitmapSourceFromHIcon(
                                    hJumbo,
                                    Int32Rect.Empty,
                                    BitmapSizeOptions.FromEmptyOptions());
                                jumboBmp.Freeze();

                                if (IsTrueJumboIconFast(jumboBmp))
                                {
                                    if (shfi.iIcon > 0) SysIndexCache[shfi.iIcon] = jumboBmp;
                                    return jumboBmp;
                                }
                            }
                            finally
                            {
                                NativeMethods.DestroyIcon(hJumbo);
                            }
                        }
                    }

                    if (_cachedExtraLargeList != null)
                    {
                        IntPtr hExtra = IntPtr.Zero;
                        if (_cachedExtraLargeList.GetIcon(shfi.iIcon, NativeMethods.ILD_TRANSPARENT | NativeMethods.ILD_IMAGE, out hExtra) == 0 && hExtra != IntPtr.Zero)
                        {
                            try
                            {
                                var extraBmp = Imaging.CreateBitmapSourceFromHIcon(
                                    hExtra,
                                    Int32Rect.Empty,
                                    BitmapSizeOptions.FromEmptyOptions());
                                extraBmp.Freeze();
                                if (shfi.iIcon > 0) SysIndexCache[shfi.iIcon] = extraBmp;
                                return extraBmp;
                            }
                            finally
                            {
                                NativeMethods.DestroyIcon(hExtra);
                            }
                        }
                    }
                }
            }
            catch { }

            try
            {
                var shinfo = new NativeMethods.SHFILEINFO();
                IntPtr res = NativeMethods.SHGetFileInfo(
                    path,
                    0,
                    ref shinfo,
                    (uint)Marshal.SizeOf(shinfo),
                    NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_LARGEICON);

                if (res != IntPtr.Zero && shinfo.hIcon != IntPtr.Zero)
                {
                    try
                    {
                        var bmp = Imaging.CreateBitmapSourceFromHIcon(
                            shinfo.hIcon,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        bmp.Freeze();
                        return bmp;
                    }
                    finally
                    {
                        NativeMethods.DestroyIcon(shinfo.hIcon);
                    }
                }
            }
            catch { }
            return null;
        }

        private static bool IsTrueJumboIconFast(BitmapSource bmp)
        {
            try
            {
                if (bmp == null || bmp.PixelWidth < 128 || bmp.PixelHeight < 128)
                    return false;

                // Only sample the center-lower band [y=64..192] rather than copying all 256KB
                int w = bmp.PixelWidth;
                int bandH = Math.Min(128, bmp.PixelHeight - 64);
                int stride = w * 4;
                byte[] bandPixels = new byte[bandH * stride];
                bmp.CopyPixels(new Int32Rect(0, 64, w, bandH), bandPixels, stride, 0);

                for (int i = 3; i < bandPixels.Length; i += 16)
                {
                    if (bandPixels[i] > 12)
                        return true;
                }
                return false;
            }
            catch
            {
                return true;
            }
        }

        public static void OpenItem(string path)
        {
            try
            {
                if (VirtualDesktopShellHelper.IsVirtualItemFile(path))
                {
                    var vInfo = VirtualDesktopShellHelper.ReadVirtualFile(path);
                    VirtualDesktopShellHelper.InvokeVirtualVerb(vInfo, null);
                    return;
                }

                if (File.Exists(path) || Directory.Exists(path))
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open item:\n" + ex.Message, "Desktop Fences", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public static void ShowProperties(string path)
        {
            try
            {
                if (VirtualDesktopShellHelper.IsVirtualItemFile(path))
                {
                    var vInfo = VirtualDesktopShellHelper.ReadVirtualFile(path);
                    VirtualDesktopShellHelper.InvokeVirtualVerb(vInfo, "properties");
                    return;
                }

                var info = new NativeMethods.SHELLEXECUTEINFO();
                info.cbSize = Marshal.SizeOf(info);
                info.lpVerb = "properties";
                info.lpFile = path;
                info.nShow = 5;
                info.fMask = NativeMethods.SEE_MASK_INVOKEIDLIST;
                NativeMethods.ShellExecuteEx(ref info);
            }
            catch { }
        }

        public static bool SendToRecycleBin(IEnumerable<string> paths, bool permanent)
        {
            var rawList = paths.Where(p => File.Exists(p) || Directory.Exists(p)).ToList();
            if (rawList.Count == 0) return false;

            // Virtual desktop items (Recycle Bin / Learn about this picture) should never be deleted;
            // pressing Delete on them safely restores them back to the native Windows Desktop!
            var virtualFiles = rawList.Where(VirtualDesktopShellHelper.IsVirtualItemFile).ToList();
            foreach (var vf in virtualFiles)
                FenceFileTransferService.RestoreItemToUserDesktop(vf);

            var list = rawList.Where(p => !VirtualDesktopShellHelper.IsVirtualItemFile(p)).ToList();
            if (list.Count == 0) return virtualFiles.Count > 0;

            try
            {
                if (permanent)
                {
                    string label = list.Count == 1
                        ? "'" + System.IO.Path.GetFileName(list[0]) + "'"
                        : list.Count + " selected items";
                    var confirm = MessageBox.Show(
                        "Permanently delete " + label + "?",
                        "Confirm Permanent Delete",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    if (confirm != MessageBoxResult.Yes) return false;
                }

                string joined = string.Join("\0", list) + "\0\0";
                var fs = new NativeMethods.SHFILEOPSTRUCT
                {
                    wFunc = 0x0003, // FO_DELETE
                    pFrom = joined,
                    fFlags = (ushort)(permanent ? 0x0010 : (0x0040 | 0x0010)) // FOF_ALLOWUNDO
                };
                int ret = NativeMethods.SHFileOperation(ref fs);
                return ret == 0 && !fs.fAnyOperationsAborted;
            }
            catch { return false; }
        }
    }

    #endregion

    #region 5. Bounded Fence Window (Anchor-Based 1:1 Resize, Marquee Multi-Select & 10 Color Themes)

    public enum ResizeDirection
    {
        None,
        Left,
        Right,
        Top,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public class FenceWindow : Window
    {
        public FenceModel Model { get; private set; }

        private Border _rootBorder;
        private Border _frameStrokeBorder;
        private Border _headerBar;
        private TextBlock _titleText;
        private TextBox _titleEditBox;
        private Border _darkLightPill;
        private TextBlock _darkLightPillText;
        private Button _colorSwatchBtn;
        private Button _rollUpBtn;
        private WrapPanel _iconPanel;
        private ScrollViewer _scrollViewer;
        private Canvas _marqueeOverlayCanvas;
        private Border _marqueeRect;
        private Border _reorderInsertionIndicator;
        private int _currentReorderInsertIndex = -1;
        private TextBlock _emptyHintText;
        private TextBlock _badgeText;
        private FileSystemWatcher _fenceFolderWatcher;

        // Multi-icon selection state
        private readonly HashSet<string> _selectedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Border> _tileBorders = new Dictionary<string, Border>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Border> _tileCheckBadges = new Dictionary<string, Border>(StringComparer.OrdinalIgnoreCase);
        private List<string> _currentOrderedPaths = new List<string>();
        private string _lastClickedPath;
        private string _searchFilter = "";

        // Bug 2 Fix: Tunneling Left/Right/Shift+Right Marquee Multi-Icon Selection & Group Drag State
        private Grid _bodyGrid;
        private bool _isPotentialMarquee;
        private bool _isMarqueeActive;
        private Point _marqueeStartPt;
        private MouseButton _marqueeButton;
        private HashSet<string> _marqueeInitialSelection = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string _pressedTilePath;
        private Point _tileDragStartPt;
        private bool _isTileLeftDragPending;
        private bool _collapseToSingleOnUp;
        private bool _suppressNextContextMenu;

        // Anchor-Based 1:1 Fence Body Drag-Move & Hold-Movement Lock State
        private bool _isPotentialBodyFenceMove;
        private bool _isMovingFence;
        private NativeMethods.POINT _moveStartCursorPx;
        private Point _moveStartWindowDip;
        private double _moveDpiX = 1.0;
        private double _moveDpiY = 1.0;
        private DispatcherTimer _holdToMoveTimer;

        // Bug 1 Fix: Anchor-Based 1:1 Absolute Screen Resize State
        private bool _isResizing;
        private ResizeDirection _resizeDir = ResizeDirection.None;
        private NativeMethods.POINT _resizeStartCursorPx;
        private Rect _resizeStartWindowDip;
        private double _resizeDpiX = 1.0;
        private double _resizeDpiY = 1.0;
        private UIElement _capturedResizeGrip;
        private DragDropEffects _activeDropEffect = DragDropEffects.Move;
        private bool _isDragOverActive;
        private bool _dragHasFileDrop;
        private DispatcherTimer _dragLeaveDebounceTimer;
        private DispatcherTimer _watcherDebounceTimer;
        private bool _isPopupMenuOpen;

        private static readonly DropShadowEffect SharedLabelShadow = CreateFrozenShadow();
        private static DropShadowEffect CreateFrozenShadow()
        {
            var shadow = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 4,
                ShadowDepth = 1,
                Opacity = 0.92
            };
            shadow.Freeze();
            return shadow;
        }

        private const double SnapThreshold = 14.0;

        public FenceWindow(FenceModel model)
        {
            Model = model;
            if (string.IsNullOrEmpty(Model.ThemeName))
                Model.ThemeName = "Navy Blue";

            Title = "DesktopFence - " + model.Title;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize; // Handled by our dedicated Anchor-Based 1:1 resize grips
            ShowInTaskbar = false;
            AllowsTransparency = true; // Enables true 32-bit Premultiplied ARGB so icons stay 100% vivid HD color!
            Background = Brushes.Transparent;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
            FontFamily = new FontFamily("Poppins, Segoe UI Variable Display, Segoe UI");

            MinWidth = 200;
            MinHeight = 38;
            Left = Model.Left;
            Top = Model.Top;
            Width = Math.Max(Model.Width, 220);
            Height = Model.IsRolledUp ? 38 : Math.Max(Model.Height, 160);

            BuildVisualTree();
            InitializeFenceFolderWatcher();

            SourceInitialized += OnSourceInitialized;
            LocationChanged += OnLocationChanged;
            SizeChanged += OnSizeChanged;
            MouseEnter += OnWindowMouseEnter;
            MouseLeave += OnWindowMouseLeave;
            Deactivated += (_, __) => CancelActiveInteractions();
            // Clicking anywhere in this fence (header or body) un-highlights selections in every OTHER fence
            PreviewMouseDown += (_, __) => { if (App.Instance != null) App.Instance.ClearSelectionInOtherFences(this); };
            PreviewKeyDown += OnWindowKeyDown;
            ContextMenuOpening += OnWindowContextMenuOpening;

            AllowDrop = true;
            PreviewDragEnter += OnFencePreviewDragEnter;
            PreviewDragOver += OnFencePreviewDragOver;
            PreviewDragLeave += OnFencePreviewDragLeave;
            PreviewDrop += OnFencePreviewDrop;
        }

        private void BuildVisualTree()
        {
            var outerShellGrid = new Grid();

            // Layer 1: Rounded Backdrop Fill (Margin=1 keeps anti-aliased curve inside window bounds)
            _rootBorder = new Border
            {
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(1)
            };

            // Layer 2: Inner Content Grid with true rounded RectangleGeometry clip so _headerBar
            // and child visuals can NEVER poke out past the 12px upper-left or upper-right corners!
            var mainGrid = new Grid
            {
                Margin = new Thickness(1)
            };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.SizeChanged += (_, __) =>
            {
                mainGrid.Clip = new RectangleGeometry(
                    new Rect(0, 0, Math.Max(0, mainGrid.ActualWidth), Math.Max(0, mainGrid.ActualHeight)),
                    11.5,
                    11.5);
            };

            // Header bar with matching top-left & top-right CornerRadius (11.5, 11.5, 0, 0)
            _headerBar = new Border
            {
                CornerRadius = Model.IsRolledUp ? new CornerRadius(11.5) : new CornerRadius(11.5, 11.5, 0, 0),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(12, 0, 8, 0)
            };
            _headerBar.MouseLeftButtonDown += HeaderBar_MouseLeftButtonDown;
            _headerBar.ContextMenu = new ContextMenu();
            _headerBar.ContextMenuOpening += (s, e) =>
            {
                if (_suppressNextContextMenu)
                {
                    _suppressNextContextMenu = false;
                    e.Handled = true;
                    return;
                }
                _headerBar.ContextMenu = BuildFenceHeaderContextMenu();
            };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _titleText = new TextBlock
            {
                Text = Model.Title,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            _titleEditBox = new TextBox
            {
                Visibility = Visibility.Collapsed,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(4, 2, 4, 2)
            };
            _titleEditBox.KeyDown += TitleEditBox_KeyDown;
            _titleEditBox.LostFocus += (_, __) => CommitInlineRename();

            _badgeText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11,
                Margin = new Thickness(6, 0, 6, 0)
            };

            // Color Theme Swatch Picker Button
            _colorSwatchBtn = new Button
            {
                Content = "🎨",
                Width = 25,
                Height = 25,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                ToolTip = "Fence Color Themes (Navy Blue, Rose Pink, Ocean, Emerald...)"
            };
            _colorSwatchBtn.Click += (_, __) => OpenColorThemeMenu();

            // Compact Dark / Light Pill Switch
            _darkLightPillText = new TextBlock
            {
                Text = ConfigManager.Current.IsDarkMode ? "🌙 Dark" : "☀ Light",
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            _darkLightPill = new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(7, 2, 7, 2),
                Margin = new Thickness(3, 0, 3, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(1),
                ToolTip = "Click to toggle Dark / Light Mode",
                Child = _darkLightPillText
            };
            _darkLightPill.MouseLeftButtonDown += (_, e) =>
            {
                App.Instance.ToggleDarkLightMode();
                e.Handled = true;
            };

            _rollUpBtn = new Button
            {
                Content = Model.IsRolledUp ? "▸" : "▾",
                Width = 24,
                Height = 24,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                ToolTip = "Roll-Up / Collapse Fence"
            };
            _rollUpBtn.Click += (_, __) => ToggleRollUp();

            Grid.SetColumn(_titleText, 0);
            Grid.SetColumn(_titleEditBox, 0);
            Grid.SetColumn(_badgeText, 1);
            Grid.SetColumn(_colorSwatchBtn, 2);
            Grid.SetColumn(_darkLightPill, 3);
            Grid.SetColumn(_rollUpBtn, 4);

            headerGrid.Children.Add(_titleText);
            headerGrid.Children.Add(_titleEditBox);
            headerGrid.Children.Add(_badgeText);
            headerGrid.Children.Add(_colorSwatchBtn);
            headerGrid.Children.Add(_darkLightPill);
            headerGrid.Children.Add(_rollUpBtn);
            _headerBar.Child = headerGrid;

            // Body Grid: Empty Hint + Icon ScrollViewer + Marquee Rubber-Band Selection Canvas
            // IMPORTANT: Use Tunneling (PreviewMouse*) events on _bodyGrid so ScrollViewer NEVER swallows clicks/drags!
            _bodyGrid = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(4, 0, 0, 0)),
                ClipToBounds = true
            };
            _bodyGrid.PreviewMouseLeftButtonDown += OnBodyPreviewMouseDown;
            _bodyGrid.PreviewMouseRightButtonDown += OnBodyPreviewMouseDown;
            _bodyGrid.PreviewMouseMove += OnBodyPreviewMouseMove;
            _bodyGrid.PreviewMouseLeftButtonUp += OnBodyPreviewMouseUp;
            _bodyGrid.PreviewMouseRightButtonUp += OnBodyPreviewMouseUp;
            _bodyGrid.LostMouseCapture += (_, __) =>
            {
                EndBodyFenceMove(saveAndSnap: false);
                EndMarqueeSelection();
            };
            _bodyGrid.ContextMenu = new ContextMenu();
            _bodyGrid.ContextMenuOpening += (s, e) =>
            {
                if (_suppressNextContextMenu)
                {
                    _suppressNextContextMenu = false;
                    e.Handled = true;
                    return;
                }
                _bodyGrid.ContextMenu = BuildFenceHeaderContextMenu();
            };

            _emptyHintText = new TextBlock
            {
                Text = "Drag desktop icons here to transfer into this fence\n(Hold Left-Click, Right-Click, or Shift + Right-Click & drag to select multiple)",
                FontSize = 11.5,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                IsHitTestVisible = false,
                Opacity = 0.62
            };

            _iconPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10),
                Background = Brushes.Transparent
            };

            _scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = _iconPanel,
                Focusable = false,
                Background = Brushes.Transparent
            };
            ApplySlimOverlayScrollbarStyle(_scrollViewer);
            _scrollViewer.Loaded += (_, __) => UpdateScrollbarHoverState(ShouldRevealTransparentChrome(), animate: false);

            _marqueeOverlayCanvas = new Canvas
            {
                IsHitTestVisible = false
            };
            _marqueeRect = new Border
            {
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1.8),
                Visibility = Visibility.Collapsed
            };
            _marqueeOverlayCanvas.Children.Add(_marqueeRect);

            // Windows Explorer-style live insertion bar for drag-and-drop manual icon sorting
            _reorderInsertionIndicator = new Border
            {
                Width = 4,
                Height = 68,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(Color.FromArgb(245, 96, 165, 250)),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(0.8),
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(37, 99, 235),
                    BlurRadius = 8,
                    ShadowDepth = 0,
                    Opacity = 0.95
                }
            };
            _marqueeOverlayCanvas.Children.Add(_reorderInsertionIndicator);

            _bodyGrid.Children.Add(_emptyHintText);
            _bodyGrid.Children.Add(_scrollViewer);
            _bodyGrid.Children.Add(_marqueeOverlayCanvas);

            Grid.SetRow(_headerBar, 0);
            Grid.SetRow(_bodyGrid, 1);
            mainGrid.Children.Add(_headerBar);
            mainGrid.Children.Add(_bodyGrid);

            _rootBorder.Child = mainGrid;

            // Layer 3: Dedicated Unbroken 1px Rounded Frame Border Overlay rendered ON TOP of header & body
            // so the upper-left, upper-right, lower-left, and lower-right curves are 100% seamless!
            _frameStrokeBorder = new Border
            {
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(1),
                Background = Brushes.Transparent,
                IsHitTestVisible = false
            };

            outerShellGrid.Children.Add(_rootBorder);
            outerShellGrid.Children.Add(_frameStrokeBorder);

            // Attach 8 dedicated 7px Anchor-Based 1:1 Resize Grips around outer perimeter
            AttachResizeGrips(outerShellGrid);

            Content = outerShellGrid;
            UpdateThemeVisuals();
        }

        #region Bug 1 Fix: Dedicated 8-Way Anchor-Based 1:1 Absolute Screen Resize Grips

        private void AttachResizeGrips(Grid container)
        {
            const double gripThickness = 7.0;
            const double cornerSize = 14.0;

            // Edges
            container.Children.Add(CreateGrip(ResizeDirection.Left, HorizontalAlignment.Left, VerticalAlignment.Stretch, gripThickness, double.NaN, new Thickness(0, cornerSize, 0, cornerSize), Cursors.SizeWE));
            container.Children.Add(CreateGrip(ResizeDirection.Right, HorizontalAlignment.Right, VerticalAlignment.Stretch, gripThickness, double.NaN, new Thickness(0, cornerSize, 0, cornerSize), Cursors.SizeWE));
            container.Children.Add(CreateGrip(ResizeDirection.Top, HorizontalAlignment.Stretch, VerticalAlignment.Top, double.NaN, gripThickness, new Thickness(cornerSize, 0, cornerSize, 0), Cursors.SizeNS));
            container.Children.Add(CreateGrip(ResizeDirection.Bottom, HorizontalAlignment.Stretch, VerticalAlignment.Bottom, double.NaN, gripThickness, new Thickness(cornerSize, 0, cornerSize, 0), Cursors.SizeNS));

            // Corners
            container.Children.Add(CreateGrip(ResizeDirection.TopLeft, HorizontalAlignment.Left, VerticalAlignment.Top, cornerSize, cornerSize, new Thickness(0), Cursors.SizeNWSE));
            container.Children.Add(CreateGrip(ResizeDirection.TopRight, HorizontalAlignment.Right, VerticalAlignment.Top, cornerSize, cornerSize, new Thickness(0), Cursors.SizeNESW));
            container.Children.Add(CreateGrip(ResizeDirection.BottomLeft, HorizontalAlignment.Left, VerticalAlignment.Bottom, cornerSize, cornerSize, new Thickness(0), Cursors.SizeNESW));
            container.Children.Add(CreateGrip(ResizeDirection.BottomRight, HorizontalAlignment.Right, VerticalAlignment.Bottom, cornerSize, cornerSize, new Thickness(0), Cursors.SizeNWSE));
        }

        private UIElement CreateGrip(
            ResizeDirection dir,
            HorizontalAlignment hAlign,
            VerticalAlignment vAlign,
            double width,
            double height,
            Thickness margin,
            Cursor cursor)
        {
            var grip = new Border
            {
                HorizontalAlignment = hAlign,
                VerticalAlignment = vAlign,
                Width = width,
                Height = height,
                Margin = margin,
                Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)), // Hit-testable transparent grip
                Cursor = cursor
            };

            grip.MouseLeftButtonDown += (s, e) => BeginAnchorResize(grip, dir, e);
            grip.MouseRightButtonDown += (s, e) => BeginAnchorResize(grip, dir, e);
            grip.MouseMove += (s, e) => UpdateAnchorResize(e);
            grip.MouseLeftButtonUp += (s, e) => EndAnchorResize(e);
            grip.MouseRightButtonUp += (s, e) => EndAnchorResize(e);
            grip.LostMouseCapture += (_, __) => StopAnchorResizeImmediate();

            return grip;
        }

        private bool IsFenceMovementLocked()
        {
            return ConfigManager.Current.LockLayout ||
                   Model.HoldPosition ||
                   string.Equals(Model.MoveMode, "Locked", StringComparison.OrdinalIgnoreCase);
        }

        private void BeginAnchorResize(UIElement grip, ResizeDirection dir, MouseButtonEventArgs e)
        {
            if (IsFenceMovementLocked() || Model.IsRolledUp)
                return;

            NativeMethods.POINT pt;
            if (!NativeMethods.GetCursorPos(out pt))
                return;

            // Query monitor DPI scale factors
            var source = PresentationSource.FromVisual(this);
            if (source != null && source.CompositionTarget != null)
            {
                _resizeDpiX = source.CompositionTarget.TransformToDevice.M11;
                _resizeDpiY = source.CompositionTarget.TransformToDevice.M22;
            }
            else
            {
                _resizeDpiX = 1.0;
                _resizeDpiY = 1.0;
            }

            _isResizing = true;
            _resizeDir = dir;
            _resizeStartCursorPx = pt;
            _resizeStartWindowDip = new Rect(Left, Top, Width, Height);
            _capturedResizeGrip = grip;

            if (e.ChangedButton == MouseButton.Right)
                _suppressNextContextMenu = true;

            grip.CaptureMouse();
            e.Handled = true;
        }

        private void UpdateAnchorResize(MouseEventArgs e)
        {
            if (!_isResizing)
                return;

            // Guaranteed live-button guard: if both mouse buttons are released, terminate immediately
            if (e.LeftButton == MouseButtonState.Released && e.RightButton == MouseButtonState.Released)
            {
                StopAnchorResizeImmediate();
                return;
            }

            NativeMethods.POINT curPx;
            if (!NativeMethods.GetCursorPos(out curPx))
                return;

            // Strictly compute from original anchor (Zero feedback loop!)
            double deltaX = (curPx.X - _resizeStartCursorPx.X) / _resizeDpiX;
            double deltaY = (curPx.Y - _resizeStartCursorPx.Y) / _resizeDpiY;

            double maxW = Math.Max(600, SystemParameters.VirtualScreenWidth);
            double maxH = Math.Max(400, SystemParameters.VirtualScreenHeight);

            double newLeft = _resizeStartWindowDip.Left;
            double newTop = _resizeStartWindowDip.Top;
            double newWidth = _resizeStartWindowDip.Width;
            double newHeight = _resizeStartWindowDip.Height;

            // Horizontal component
            if (_resizeDir == ResizeDirection.Right || _resizeDir == ResizeDirection.TopRight || _resizeDir == ResizeDirection.BottomRight)
            {
                newWidth = Clamp(_resizeStartWindowDip.Width + deltaX, MinWidth, maxW);
            }
            else if (_resizeDir == ResizeDirection.Left || _resizeDir == ResizeDirection.TopLeft || _resizeDir == ResizeDirection.BottomLeft)
            {
                double rightAnchor = _resizeStartWindowDip.Left + _resizeStartWindowDip.Width;
                newWidth = Clamp(_resizeStartWindowDip.Width - deltaX, MinWidth, maxW);
                newLeft = rightAnchor - newWidth;
            }

            // Vertical component
            if (_resizeDir == ResizeDirection.Bottom || _resizeDir == ResizeDirection.BottomLeft || _resizeDir == ResizeDirection.BottomRight)
            {
                newHeight = Clamp(_resizeStartWindowDip.Height + deltaY, Math.Max(MinHeight, 120), maxH);
            }
            else if (_resizeDir == ResizeDirection.Top || _resizeDir == ResizeDirection.TopLeft || _resizeDir == ResizeDirection.TopRight)
            {
                double bottomAnchor = _resizeStartWindowDip.Top + _resizeStartWindowDip.Height;
                newHeight = Clamp(_resizeStartWindowDip.Height - deltaY, Math.Max(MinHeight, 120), maxH);
                newTop = bottomAnchor - newHeight;
            }

            Left = newLeft;
            Top = newTop;
            Width = newWidth;
            Height = newHeight;
        }

        private void EndAnchorResize(MouseButtonEventArgs e)
        {
            if (!_isResizing) return;
            StopAnchorResizeImmediate();
            ConfigManager.Save();
            e.Handled = true;
        }

        private void StopAnchorResizeImmediate()
        {
            if (!_isResizing) return;
            _isResizing = false;
            _resizeDir = ResizeDirection.None;
            if (_capturedResizeGrip != null)
            {
                _capturedResizeGrip.ReleaseMouseCapture();
                _capturedResizeGrip = null;
            }
        }

        private static double Clamp(double val, double min, double max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        #endregion

        #region Bug 2 Fix: Unified Tunneling Marquee Multi-Select (Left, Right, Shift+Right) & Group Drag

        private void OnBodyPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Allow scrollbar thumb/track clicks to operate normally
            if (IsDescendantOfScrollBar(e.OriginalSource as DependencyObject))
                return;

            // Keep fence strictly pinned to the desktop layer (HWND_BOTTOM) and never activate over open browsers/apps!
            Focus();
            DesktopHostManager.UpdateZOrder(new WindowInteropHelper(this).Handle);

            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            string hitTilePath = GetTilePathFromNode(e.OriginalSource as DependencyObject);

            _pressedTilePath = hitTilePath;
            _collapseToSingleOnUp = false;

            // Double-click left button on an icon tile -> Launch item(s)
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2 && !string.IsNullOrEmpty(hitTilePath))
            {
                ShellIconExtractor.OpenItem(hitTilePath);
                _isPotentialMarquee = false;
                _isTileLeftDragPending = false;
                e.Handled = true;
                return;
            }

            // RIGHT-CLICK (with or without Shift/Ctrl, over empty space OR over an icon tile):
            // Always prepare potential Marquee rubber-band selection! If dragged >= 4px, rubber-band selects multiple icons!
            if (e.ChangedButton == MouseButton.Right)
            {
                _isTileLeftDragPending = false;
                _isPotentialMarquee = true;
                _isMarqueeActive = false;
                _marqueeButton = MouseButton.Right;
                _marqueeStartPt = e.GetPosition(_marqueeOverlayCanvas);

                if (shift || ctrl)
                {
                    _marqueeInitialSelection = new HashSet<string>(_selectedPaths, StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    _marqueeInitialSelection = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (string.IsNullOrEmpty(hitTilePath))
                    {
                        ClearSelection();
                    }
                    else if (!_selectedPaths.Contains(hitTilePath))
                    {
                        _selectedPaths.Clear();
                        _selectedPaths.Add(hitTilePath);
                        _lastClickedPath = hitTilePath;
                        RefreshTileSelectionVisuals();
                    }
                }
                return; // Do not mark Handled yet so quick right-click (< 4px) can still open context menu
            }

            // LEFT-CLICK:
            if (e.ChangedButton == MouseButton.Left)
            {
                // Case A: Clicked on empty space inside the fence box (not an icon tile)
                if (string.IsNullOrEmpty(hitTilePath))
                {
                    _isTileLeftDragPending = false;
                    if (!shift && !ctrl)
                        ClearSelection();

                    string moveMode = string.IsNullOrWhiteSpace(Model.MoveMode) ? "Anywhere" : Model.MoveMode;
                    bool canMoveFromBody = !shift && !ctrl && !IsFenceMovementLocked() &&
                                           !string.Equals(moveMode, "HeaderOnly", StringComparison.OrdinalIgnoreCase);

                    if (canMoveFromBody)
                    {
                        CaptureDpiForMoveAndInitAnchor();

                        if (string.Equals(moveMode, "HoldDelay", StringComparison.OrdinalIgnoreCase))
                        {
                            // Hold Left-Click for 0.35s to pick up fence; quick swipe (<0.35s) does marquee selection!
                            _isPotentialBodyFenceMove = false;
                            _isMovingFence = false;
                            _marqueeInitialSelection = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            _isPotentialMarquee = true;
                            _isMarqueeActive = false;
                            _marqueeButton = MouseButton.Left;
                            _marqueeStartPt = e.GetPosition(_marqueeOverlayCanvas);

                            StartHoldToMoveTimer();
                            _bodyGrid.CaptureMouse();
                            e.Handled = true;
                            return;
                        }
                        else
                        {
                            // Default "Anywhere": Holding Left-Click anywhere inside the box moves the fence smoothly!
                            _isPotentialMarquee = false;
                            _isMarqueeActive = false;
                            _isPotentialBodyFenceMove = true;
                            _isMovingFence = false;
                            _bodyGrid.CaptureMouse();
                            e.Handled = true;
                            return;
                        }
                    }

                    // If movement is Held/Locked, HeaderOnly, or Shift/Ctrl is held -> Left-Click Marquee Selection!
                    _marqueeInitialSelection = (shift || ctrl)
                        ? new HashSet<string>(_selectedPaths, StringComparer.OrdinalIgnoreCase)
                        : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    _isPotentialMarquee = true;
                    _isMarqueeActive = false;
                    _marqueeButton = MouseButton.Left;
                    _marqueeStartPt = e.GetPosition(_marqueeOverlayCanvas);
                    _bodyGrid.CaptureMouse();
                    e.Handled = true;
                    return;
                }

                // Case B: Left-clicked on an icon tile
                if (shift || ctrl)
                {
                    // Support BOTH Shift/Ctrl click selection AND Shift/Ctrl + Left-Drag marquee selection!
                    _marqueeInitialSelection = new HashSet<string>(_selectedPaths, StringComparer.OrdinalIgnoreCase);
                    HandleTileSelectionClick(hitTilePath, _currentOrderedPaths);
                    _isPotentialMarquee = true;
                    _isMarqueeActive = false;
                    _marqueeButton = MouseButton.Left;
                    _marqueeStartPt = e.GetPosition(_marqueeOverlayCanvas);
                    _isTileLeftDragPending = false;
                    e.Handled = true;
                    return;
                }

                // Plain Left-Click on an icon tile (no Shift/Ctrl):
                // If already part of multi-selection, keep all selected during MouseDown so dragging moves the WHOLE group!
                if (_selectedPaths.Contains(hitTilePath) && _selectedPaths.Count > 1)
                {
                    _collapseToSingleOnUp = true;
                }
                else
                {
                    _selectedPaths.Clear();
                    _selectedPaths.Add(hitTilePath);
                    RefreshTileSelectionVisuals();
                }

                _lastClickedPath = hitTilePath;
                _tileDragStartPt = e.GetPosition(this);
                _isTileLeftDragPending = true;
                _isPotentialMarquee = false;
                e.Handled = true;
            }
        }

        private void CaptureDpiForMoveAndInitAnchor()
        {
            NativeMethods.GetCursorPos(out _moveStartCursorPx);
            _moveStartWindowDip = new Point(Left, Top);
            var source = PresentationSource.FromVisual(this);
            if (source != null && source.CompositionTarget != null)
            {
                _moveDpiX = source.CompositionTarget.TransformToDevice.M11;
                _moveDpiY = source.CompositionTarget.TransformToDevice.M22;
            }
            else
            {
                _moveDpiX = 1.0;
                _moveDpiY = 1.0;
            }
        }

        private void StartHoldToMoveTimer()
        {
            StopHoldToMoveTimer();
            _holdToMoveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
            _holdToMoveTimer.Tick += (_, __) =>
            {
                StopHoldToMoveTimer();
                if (Mouse.LeftButton == MouseButtonState.Pressed && !_isMarqueeActive && !IsFenceMovementLocked())
                {
                    _isPotentialMarquee = false;
                    _marqueeRect.Visibility = Visibility.Collapsed;
                    CaptureDpiForMoveAndInitAnchor();
                    _isMovingFence = true;
                    _bodyGrid.Cursor = Cursors.SizeAll;
                    ApplyTransparentHoverChromeState(animate: false);
                }
            };
            _holdToMoveTimer.Start();
        }

        private void StopHoldToMoveTimer()
        {
            if (_holdToMoveTimer != null)
            {
                _holdToMoveTimer.Stop();
                _holdToMoveTimer = null;
            }
        }

        private void EndBodyFenceMove(bool saveAndSnap)
        {
            StopHoldToMoveTimer();
            bool wasMoving = _isMovingFence;
            _isPotentialBodyFenceMove = false;
            _isMovingFence = false;
            if (_bodyGrid != null)
                _bodyGrid.Cursor = null;

            if (Mouse.Captured == _bodyGrid && !_isPotentialMarquee && !_isMarqueeActive)
                _bodyGrid.ReleaseMouseCapture();

            if (wasMoving && saveAndSnap)
            {
                ApplyMagneticSnap();
                ConfigManager.SaveAsync();
                ApplyTransparentHoverChromeState(animate: true);
            }
        }

        private void OnBodyPreviewMouseMove(object sender, MouseEventArgs e)
        {
            // 0. Check Anchor-Based 1:1 Fence Frame Drag-Move from anywhere inside the box!
            if (_isPotentialBodyFenceMove || _isMovingFence)
            {
                if (e.LeftButton != MouseButtonState.Pressed || IsFenceMovementLocked())
                {
                    EndBodyFenceMove(saveAndSnap: true);
                    return;
                }

                NativeMethods.POINT curPx;
                if (!NativeMethods.GetCursorPos(out curPx))
                    return;

                double dxDip = (curPx.X - _moveStartCursorPx.X) / _moveDpiX;
                double dyDip = (curPx.Y - _moveStartCursorPx.Y) / _moveDpiY;

                if (!_isMovingFence)
                {
                    if (Math.Abs(dxDip) < 4.0 && Math.Abs(dyDip) < 4.0)
                        return; // 4px threshold prevents accidental nudge on single click

                    _isMovingFence = true;
                    _bodyGrid.Cursor = Cursors.SizeAll;
                    ApplyTransparentHoverChromeState(animate: false);
                }

                Left = _moveStartWindowDip.X + dxDip;
                Top = _moveStartWindowDip.Y + dyDip;
                e.Handled = true;
                return;
            }

            // 1. Check Group File Drag-and-Drop (Left-Click dragging an icon tile)
            if (_isTileLeftDragPending && e.LeftButton == MouseButtonState.Pressed && !ConfigManager.Current.LockLayout)
            {
                Point curWinPt = e.GetPosition(this);
                if (Math.Abs(curWinPt.X - _tileDragStartPt.X) >= 5.0 || Math.Abs(curWinPt.Y - _tileDragStartPt.Y) >= 5.0)
                {
                    _isTileLeftDragPending = false;
                    _collapseToSingleOnUp = false;

                    if (!string.IsNullOrEmpty(_pressedTilePath) && !_selectedPaths.Contains(_pressedTilePath))
                    {
                        _selectedPaths.Clear();
                        _selectedPaths.Add(_pressedTilePath);
                        RefreshTileSelectionVisuals();
                    }

                    string[] dragPaths = _selectedPaths
                        .Where(p => File.Exists(p) || Directory.Exists(p))
                        .ToArray();

                    if (dragPaths.Length > 0)
                    {
                        // Order dragPaths by their visual sequence in the fence
                        string[] orderedDragPaths = _currentOrderedPaths
                            .Where(p => dragPaths.Contains(p, StringComparer.OrdinalIgnoreCase))
                            .ToArray();
                        if (orderedDragPaths.Length == 0)
                            orderedDragPaths = dragPaths;

                        string primaryDragPath = (!string.IsNullOrEmpty(_pressedTilePath) && orderedDragPaths.Contains(_pressedTilePath, StringComparer.OrdinalIgnoreCase))
                            ? _pressedTilePath
                            : orderedDragPaths[0];

                        // Dim the source tiles while being dragged (just like Windows Explorer / Desktop)
                        foreach (string dp in orderedDragPaths)
                        {
                            Border b;
                            if (_tileBorders.TryGetValue(dp, out b))
                                b.Opacity = 0.38;
                        }

                        // Force all fences to stay strictly at HWND_BOTTOM behind any open Web Browser or application
                        DesktopHostManager.PushAllFencesToDesktopBottom();

                        var data = new DataObject(DataFormats.FileDrop, orderedDragPaths);
                        data.SetData("DesktopFences.ReorderSourceFenceId", Model.Id);
                        data.SetData("DesktopFences.ReorderPrimaryPath", primaryDragPath);
                        data.SetData("Preferred DropEffect", new MemoryStream(BitConverter.GetBytes((int)DragDropEffects.Move)));

                        var primaryItem = ShellIconExtractor.CreateItem(primaryDragPath);
                        GiveFeedbackEventHandler feedbackHandler = (s, fbArgs) =>
                        {
                            fbArgs.UseDefaultCursors = true;
                            fbArgs.Handled = false;
                            DragGhostCursorAdorner.UpdatePositionAndEffect(fbArgs.Effects);
                        };
                        QueryContinueDragEventHandler queryHandler = (s, qArgs) =>
                        {
                            DragGhostCursorAdorner.UpdatePosition();
                            DesktopHostManager.PushAllFencesToDesktopBottom();
                        };

                        _bodyGrid.GiveFeedback += feedbackHandler;
                        _bodyGrid.QueryContinueDrag += queryHandler;
                        try
                        {
                            DragGhostCursorAdorner.Show(primaryItem, orderedDragPaths.Length);
                            DragDrop.DoDragDrop(_bodyGrid, data, DragDropEffects.Move | DragDropEffects.Copy | DragDropEffects.Link);
                        }
                        finally
                        {
                            _bodyGrid.GiveFeedback -= feedbackHandler;
                            _bodyGrid.QueryContinueDrag -= queryHandler;
                            DragGhostCursorAdorner.HideGhost();
                            HideReorderInsertionIndicator();
                            DesktopHostManager.PushAllFencesToDesktopBottom();
                        }

                        // If a virtual item (.vfence) was dropped onto the native Windows Desktop, restore its real CLSID icon!
                        FenceFileTransferService.SweepDroppedVirtualFilesOnDesktop();
                        PopulateIcons();
                    }
                    return;
                }
            }

            // 2. Check Marquee Multi-Icon Rubber-Band Selection (Left-Drag, Right-Drag, or Shift + Right-Drag!)
            if (!_isPotentialMarquee && !_isMarqueeActive)
                return;

            bool buttonStillPressed = (_marqueeButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed) ||
                                      (_marqueeButton == MouseButton.Right && e.RightButton == MouseButtonState.Pressed);

            if (!buttonStillPressed)
            {
                EndMarqueeSelection();
                return;
            }

            Point curPt = e.GetPosition(_marqueeOverlayCanvas);
            double dx = Math.Abs(curPt.X - _marqueeStartPt.X);
            double dy = Math.Abs(curPt.Y - _marqueeStartPt.Y);

            if (!_isMarqueeActive)
            {
                if (dx < 4.0 && dy < 4.0)
                    return; // 4px drag threshold before activating rubber-band rectangle

                // If user swiped quickly before 0.35s Hold-to-Move elapsed, cancel Hold-to-Move and enter Marquee!
                StopHoldToMoveTimer();
                _isMarqueeActive = true;
                _isTileLeftDragPending = false;
                if (_marqueeButton == MouseButton.Right)
                    _suppressNextContextMenu = true;

                if (Mouse.Captured != _bodyGrid)
                    _bodyGrid.CaptureMouse();

                _marqueeRect.Visibility = Visibility.Visible;
            }

            double x = Math.Min(_marqueeStartPt.X, curPt.X);
            double y = Math.Min(_marqueeStartPt.Y, curPt.Y);
            double w = Math.Max(1, dx);
            double h = Math.Max(1, dy);

            Canvas.SetLeft(_marqueeRect, x);
            Canvas.SetTop(_marqueeRect, y);
            _marqueeRect.Width = w;
            _marqueeRect.Height = h;

            Rect selectionBox = new Rect(x, y, w, h);
            UpdateMarqueeHitTest(selectionBox);
            e.Handled = true;
        }

        private void OnBodyPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            StopHoldToMoveTimer();
            _isTileLeftDragPending = false;

            if (_isPotentialBodyFenceMove || _isMovingFence)
            {
                bool moved = _isMovingFence;
                EndBodyFenceMove(saveAndSnap: true);
                if (moved)
                    e.Handled = true;
                return;
            }

            if (_collapseToSingleOnUp && e.ChangedButton == MouseButton.Left && !string.IsNullOrEmpty(_pressedTilePath))
            {
                _collapseToSingleOnUp = false;
                _selectedPaths.Clear();
                _selectedPaths.Add(_pressedTilePath);
                RefreshTileSelectionVisuals();
            }

            // Quick Shift+RightClick or Ctrl+RightClick on an icon without dragging -> multi-select without opening menu!
            if (!_isMarqueeActive && e.ChangedButton == MouseButton.Right && !string.IsNullOrEmpty(_pressedTilePath))
            {
                bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                if (shift || ctrl)
                {
                    HandleTileSelectionClick(_pressedTilePath, _currentOrderedPaths);
                    _suppressNextContextMenu = true;
                    EndMarqueeSelection();
                    e.Handled = true;
                    return;
                }
            }

            if (_isPotentialMarquee || _isMarqueeActive)
            {
                bool wasActive = _isMarqueeActive;
                EndMarqueeSelection();
                if (wasActive)
                    e.Handled = true;
            }
        }

        private void EndMarqueeSelection()
        {
            _isPotentialMarquee = false;
            _isMarqueeActive = false;
            _marqueeRect.Visibility = Visibility.Collapsed;
            if (Mouse.Captured == _bodyGrid)
                _bodyGrid.ReleaseMouseCapture();
        }

        private void UpdateMarqueeHitTest(Rect selectionBoxInCanvas)
        {
            // Preserve _marqueeInitialSelection so Shift + Right-Click Drag / Ctrl + Drag adds to selection!
            _selectedPaths.Clear();
            foreach (var existing in _marqueeInitialSelection)
                _selectedPaths.Add(existing);

            foreach (var kv in _tileBorders)
            {
                string path = kv.Key;
                Border tile = kv.Value;
                if (!tile.IsVisible) continue;

                try
                {
                    Point topLeft = tile.TranslatePoint(new Point(0, 0), _marqueeOverlayCanvas);
                    Rect tileRect = new Rect(topLeft.X, topLeft.Y, tile.ActualWidth, tile.ActualHeight);
                    if (selectionBoxInCanvas.IntersectsWith(tileRect))
                    {
                        _selectedPaths.Add(path);
                    }
                }
                catch { }
            }

            RefreshTileSelectionVisuals();
        }

        private string GetTilePathFromNode(DependencyObject node)
        {
            while (node != null && node != this)
            {
                var border = node as Border;
                if (border != null && border.Tag is string)
                {
                    string tag = (string)border.Tag;
                    if (tag.StartsWith("TILE:", StringComparison.Ordinal))
                        return tag.Substring(5);
                }
                node = VisualTreeHelper.GetParent(node);
            }
            return null;
        }

        private bool IsDescendantOfScrollBar(DependencyObject node)
        {
            while (node != null && node != this)
            {
                if (node is ScrollBar)
                    return true;
                node = VisualTreeHelper.GetParent(node);
            }
            return false;
        }

        private void OnWindowContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_suppressNextContextMenu)
            {
                _suppressNextContextMenu = false;
                e.Handled = true;
            }
        }

        private void CancelActiveInteractions()
        {
            StopAnchorResizeImmediate();
            EndBodyFenceMove(saveAndSnap: true);
            EndMarqueeSelection();
            DesktopHostManager.UpdateZOrder(new WindowInteropHelper(this).Handle);
        }

        #endregion

        #region Multi-Icon Selection, Group Drag-and-Drop & Two-Way FileSystemWatcher

        private void InitializeFenceFolderWatcher()
        {
            try
            {
                _watcherDebounceTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(55)
                };
                _watcherDebounceTimer.Tick += (_, __) =>
                {
                    _watcherDebounceTimer.Stop();
                    PopulateIcons();
                };

                string dir = ConfigManager.GetFenceFolderPath(Model.Id);
                _fenceFolderWatcher = new FileSystemWatcher(dir)
                {
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true
                };
                _fenceFolderWatcher.Created += (_, __) => SchedulePopulate();
                _fenceFolderWatcher.Deleted += (_, __) => SchedulePopulate();
                _fenceFolderWatcher.Renamed += (_, e) =>
                {
                    ShellIconExtractor.InvalidateCache(e.OldFullPath);
                    SchedulePopulate();
                };
            }
            catch { }
        }

        public void SetWatcherEnabled(bool enabled)
        {
            try
            {
                if (_fenceFolderWatcher != null)
                    _fenceFolderWatcher.EnableRaisingEvents = enabled;
            }
            catch { }
        }

        private void SchedulePopulate()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_watcherDebounceTimer != null)
                {
                    _watcherDebounceTimer.Stop();
                    _watcherDebounceTimer.Start();
                }
                else
                {
                    PopulateIcons();
                }
            }), DispatcherPriority.Background);
        }

        public void ApplySearchFilter(string query)
        {
            _searchFilter = (query ?? "").Trim();
            PopulateIcons();
        }

        public void PopulateIcons()
        {
            _iconPanel.Children.Clear();
            _tileBorders.Clear();
            _tileCheckBadges.Clear();

            string fenceDir = ConfigManager.GetFenceFolderPath(Model.Id);
            var diskEntries = new List<string>();
            if (Directory.Exists(fenceDir))
            {
                try
                {
                    diskEntries.AddRange(Directory.GetDirectories(fenceDir));
                    diskEntries.AddRange(Directory.GetFiles(fenceDir)
                        .Where(f => !f.EndsWith("desktop.ini", StringComparison.OrdinalIgnoreCase)));
                }
                catch { }
            }

            List<string> orderedPaths = SortFenceEntries(diskEntries);

            _currentOrderedPaths = orderedPaths;
            Model.OrderedFileNames = orderedPaths.Select(System.IO.Path.GetFileName).ToList();

            _selectedPaths.RemoveWhere(p => !orderedPaths.Contains(p, StringComparer.OrdinalIgnoreCase));
            _emptyHintText.Visibility = orderedPaths.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            int iconSize = Math.Max(28, Math.Min(72, ConfigManager.Current.IconSizePx));
            double tileWidth = iconSize + 44;
            bool isDark = ConfigManager.Current.IsDarkMode;

            foreach (string path in orderedPaths)
            {
                var item = ShellIconExtractor.CreateItem(path);
                bool matchesFilter = string.IsNullOrEmpty(_searchFilter) ||
                                     item.DisplayName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;

                var tile = BuildIconTile(item, iconSize, tileWidth, isDark, matchesFilter);
                _tileBorders[path] = tile;
                _iconPanel.Children.Add(tile);
            }

            RefreshTileSelectionVisuals();
        }

        private Border BuildIconTile(
            DesktopIconItem item,
            int iconSize,
            double tileWidth,
            bool isDark,
            bool matchesFilter)
        {
            var tileBorder = new Border
            {
                Width = tileWidth,
                Margin = new Thickness(4),
                Padding = new Thickness(6, 6, 6, 4),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1.5),
                BorderBrush = Brushes.Transparent,
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                Opacity = matchesFilter ? 1.0 : 0.22,
                ToolTip = item.DisplayName + "\n(Inside Fence: " + Model.Title + ")",
                Tag = "TILE:" + item.FullPath
            };

            var tileGrid = new Grid();
            var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

            if (item.IconSource != null)
            {
                var img = new Image
                {
                    Source = item.IconSource,
                    Width = iconSize,
                    Height = iconSize,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                stack.Children.Add(img);
            }
            else
            {
                stack.Children.Add(new Border
                {
                    Width = iconSize,
                    Height = iconSize,
                    CornerRadius = new CornerRadius(6),
                    Background = new SolidColorBrush(Color.FromRgb(59, 130, 246))
                });
            }

            bool useLightWallpaperText = isDark || IsTransparentHoverTheme();
            Color textColor = useLightWallpaperText ? Color.FromRgb(245, 247, 250) : Color.FromRgb(17, 24, 39);
            var lbl = new TextBlock
            {
                Text = item.DisplayName,
                FontSize = 11.5,
                FontWeight = useLightWallpaperText ? FontWeights.Normal : FontWeights.Medium,
                Foreground = new SolidColorBrush(textColor),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 34,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 4, 0, 0)
            };

            if (useLightWallpaperText)
            {
                lbl.Effect = SharedLabelShadow;
            }
            stack.Children.Add(lbl);
            tileGrid.Children.Add(stack);

            // Crisp checkmark badge shown when icon is selected
            var checkBadge = new Border
            {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Visibility = Visibility.Collapsed,
                Child = new TextBlock
                {
                    Text = "✓",
                    FontSize = 9.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            _tileCheckBadges[item.FullPath] = checkBadge;
            tileGrid.Children.Add(checkBadge);

            tileBorder.Child = tileGrid;

            tileBorder.MouseEnter += (_, __) =>
            {
                if (!_selectedPaths.Contains(item.FullPath))
                {
                    tileBorder.Background = new SolidColorBrush(
                        isDark ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(30, 0, 0, 0));
                }
            };
            tileBorder.MouseLeave += (_, __) => RefreshTileSelectionVisuals();

            // Rebuild context menu dynamically on right-click open so "(X selected)" count is always live!
            tileBorder.ContextMenu = new ContextMenu();
            tileBorder.ContextMenuOpening += (s, e) =>
            {
                if (_suppressNextContextMenu)
                {
                    _suppressNextContextMenu = false;
                    e.Handled = true;
                    return;
                }
                tileBorder.ContextMenu = BuildIconContextMenu(item);
            };

            return tileBorder;
        }

        private void HandleTileSelectionClick(string clickedPath, List<string> allOrderedPaths)
        {
            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            if (shift && !string.IsNullOrEmpty(_lastClickedPath))
            {
                int i1 = allOrderedPaths.FindIndex(p => string.Equals(p, _lastClickedPath, StringComparison.OrdinalIgnoreCase));
                int i2 = allOrderedPaths.FindIndex(p => string.Equals(p, clickedPath, StringComparison.OrdinalIgnoreCase));
                if (i1 >= 0 && i2 >= 0)
                {
                    if (!ctrl) _selectedPaths.Clear();
                    int start = Math.Min(i1, i2);
                    int end = Math.Max(i1, i2);
                    for (int i = start; i <= end; i++)
                        _selectedPaths.Add(allOrderedPaths[i]);
                }
            }
            else if (ctrl)
            {
                if (_selectedPaths.Contains(clickedPath))
                    _selectedPaths.Remove(clickedPath);
                else
                    _selectedPaths.Add(clickedPath);
                _lastClickedPath = clickedPath;
            }
            else
            {
                if (!_selectedPaths.Contains(clickedPath))
                {
                    _selectedPaths.Clear();
                    _selectedPaths.Add(clickedPath);
                }
                _lastClickedPath = clickedPath;
            }

            RefreshTileSelectionVisuals();
        }

        private void ClearSelection()
        {
            _selectedPaths.Clear();
            RefreshTileSelectionVisuals();
        }

        public bool HasSelection
        {
            get { return _selectedPaths.Count > 0; }
        }

        /// <summary>Clears highlighted icons when the user clicks outside this fence (desktop, taskbar, other apps, other fences).</summary>
        public void ClearSelectionFromOutside()
        {
            if (_selectedPaths.Count == 0 && !_isMarqueeActive)
                return;
            if (_isMarqueeActive || _isPotentialMarquee)
                EndMarqueeSelection();
            _isTileLeftDragPending = false;
            _lastClickedPath = null;
            ClearSelection();
        }

        private void RefreshTileSelectionVisuals()
        {
            Color accent = GetEffectiveAccentColor();
            // Brighten highlight border in Dark Mode so selection stands out clearly even on deep Navy Blue!
            Color borderHighlight = ConfigManager.Current.IsDarkMode
                ? Color.FromRgb(
                    (byte)Math.Min(255, accent.R + 75),
                    (byte)Math.Min(255, accent.G + 85),
                    (byte)Math.Min(255, accent.B + 95))
                : accent;

            var selectedBg = new SolidColorBrush(Color.FromArgb(130, borderHighlight.R, borderHighlight.G, borderHighlight.B));
            var selectedBorder = new SolidColorBrush(Color.FromArgb(235, borderHighlight.R, borderHighlight.G, borderHighlight.B));

            foreach (var kv in _tileBorders)
            {
                bool isSel = _selectedPaths.Contains(kv.Key);
                kv.Value.Background = isSel ? selectedBg : Brushes.Transparent;
                kv.Value.BorderBrush = isSel ? selectedBorder : Brushes.Transparent;

                Border badge;
                if (_tileCheckBadges.TryGetValue(kv.Key, out badge))
                {
                    badge.Visibility = isSel ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            if (_badgeText != null)
            {
                _badgeText.Text = _selectedPaths.Count > 1
                    ? _selectedPaths.Count + " / " + _currentOrderedPaths.Count + " selected"
                    : _currentOrderedPaths.Count.ToString();
            }

            // Arm the outside-click watcher only while something is highlighted (zero CPU when idle)
            if (_selectedPaths.Count > 0)
                OutsideClickSelectionWatcher.Start();
        }

        private ContextMenu BuildIconContextMenu(DesktopIconItem item)
        {
            var cm = new ContextMenu();
            int selCount = Math.Max(1, _selectedPaths.Count);
            string countSuffix = selCount > 1 ? " (" + selCount + " selected)" : "";

            var openItem = new MenuItem { Header = "Open" + countSuffix, FontWeight = FontWeights.SemiBold };
            openItem.Click += (_, __) =>
            {
                foreach (var p in GetActiveSelectionOrSingle(item.FullPath))
                    ShellIconExtractor.OpenItem(p);
            };
            cm.Items.Add(openItem);

            if (VirtualDesktopShellHelper.IsVirtualItemFile(item.FullPath))
            {
                var vInfo = VirtualDesktopShellHelper.ReadVirtualFile(item.FullPath);
                if (vInfo != null)
                {
                    // Recycle Bin special action: Empty Recycle Bin
                    if (string.Equals(vInfo.Clsid, "{645FF040-5081-101B-9F08-00AA002F954E}", StringComparison.OrdinalIgnoreCase))
                    {
                        var emptyBinItem = new MenuItem { Header = "🗑 Empty Recycle Bin" };
                        emptyBinItem.Click += (_, __) =>
                        {
                            try
                            {
                                NativeMethods.SHEmptyRecycleBin(IntPtr.Zero, null, 0);
                                ShellIconExtractor.InvalidateCache(item.FullPath);
                                PopulateIcons();
                            }
                            catch { }
                        };
                        cm.Items.Add(emptyBinItem);
                    }
                    // Windows Spotlight ("Learn about this picture") special native verbs!
                    else if (string.Equals(vInfo.Clsid, "{2cc5ca98-6485-489a-920e-b3e88a6ccce3}", StringComparison.OrdinalIgnoreCase))
                    {
                        var nextPicItem = new MenuItem { Header = "🖼 Switch to next picture" };
                        nextPicItem.Click += (_, __) => VirtualDesktopShellHelper.InvokeVirtualVerb(vInfo, "SpotlightNext");
                        cm.Items.Add(nextPicItem);

                        var likePicItem = new MenuItem { Header = "👍 I like this picture" };
                        likePicItem.Click += (_, __) => VirtualDesktopShellHelper.InvokeVirtualVerb(vInfo, "SpotlightLike");
                        cm.Items.Add(likePicItem);

                        var dislikePicItem = new MenuItem { Header = "👎 Not a fan of this picture" };
                        dislikePicItem.Click += (_, __) => VirtualDesktopShellHelper.InvokeVirtualVerb(vInfo, "SpotlightDislike");
                        cm.Items.Add(dislikePicItem);
                    }
                }
            }
            else if (item.FullPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                item.FullPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) ||
                item.FullPath.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
            {
                var runAdminItem = new MenuItem { Header = "Run as administrator" };
                runAdminItem.Click += (_, __) =>
                {
                    try { Process.Start(new ProcessStartInfo(item.FullPath) { UseShellExecute = true, Verb = "runas" }); }
                    catch { }
                };
                cm.Items.Add(runAdminItem);
            }

            cm.Items.Add(new Separator());

            var restoreDesktopItem = new MenuItem { Header = "🖥 Move Back to Native Desktop" + countSuffix };
            restoreDesktopItem.Click += (_, __) =>
            {
                foreach (var p in GetActiveSelectionOrSingle(item.FullPath))
                    FenceFileTransferService.RestoreItemToUserDesktop(p);
                PopulateIcons();
            };
            cm.Items.Add(restoreDesktopItem);

            var moveMenu = new MenuItem { Header = "▣ Move to Another Fence" + countSuffix };
            foreach (var targetFence in ConfigManager.Current.Fences.Where(f => f.Id != Model.Id))
            {
                var target = targetFence;
                var sub = new MenuItem { Header = target.Title };
                sub.Click += (_, __) =>
                {
                    foreach (var p in GetActiveSelectionOrSingle(item.FullPath))
                        FenceFileTransferService.MoveItemIntoFence(p, target);
                    App.Instance.RefreshAllFences();
                };
                moveMenu.Items.Add(sub);
            }
            if (moveMenu.Items.Count > 0)
                cm.Items.Add(moveMenu);

            cm.Items.Add(new Separator());

            var renameItem = new MenuItem { Header = "Rename (F2)" };
            renameItem.Click += (_, __) => PromptRenameFile(item.FullPath);
            cm.Items.Add(renameItem);

            var deleteItem = new MenuItem { Header = "Delete to Recycle Bin (Del)" + countSuffix };
            deleteItem.Click += (_, __) =>
            {
                ShellIconExtractor.SendToRecycleBin(GetActiveSelectionOrSingle(item.FullPath), permanent: false);
                PopulateIcons();
            };
            cm.Items.Add(deleteItem);

            cm.Items.Add(new Separator());
            cm.Items.Add(BuildSortBySubmenu());
            cm.Items.Add(BuildHoldMovementSubmenu());
            cm.Items.Add(new Separator());

            var propsItem = new MenuItem { Header = "Properties (Alt+Enter)" };
            propsItem.Click += (_, __) => ShellIconExtractor.ShowProperties(item.FullPath);
            cm.Items.Add(propsItem);

            return cm;
        }

        private List<string> GetActiveSelectionOrSingle(string fallbackPath)
        {
            if (_selectedPaths.Count > 0 && _selectedPaths.Contains(fallbackPath))
                return _selectedPaths.ToList();
            return new List<string> { fallbackPath };
        }

        private void OnFencePreviewDragEnter(object sender, DragEventArgs e)
        {
            if (_dragLeaveDebounceTimer != null)
                _dragLeaveDebounceTimer.Stop();

            if (!_isDragOverActive)
            {
                // Accept BOTH normal files (FileDrop) AND virtual Windows Desktop icons (Recycle Bin / Spotlight via "Shell IDList Array")!
                bool hasFiles = e.Data.GetDataPresent(DataFormats.FileDrop);
                bool hasShellIdList = !hasFiles && e.Data.GetDataPresent("Shell IDList Array");
                _dragHasFileDrop = hasFiles || hasShellIdList;

                if (_dragHasFileDrop)
                {
                    // Virtual shell items (Recycle Bin / Learn about this picture) only advertise DragDropEffects.Link in Explorer!
                    if ((e.AllowedEffects & DragDropEffects.Move) != 0)
                        _activeDropEffect = DragDropEffects.Move;
                    else if ((e.AllowedEffects & DragDropEffects.Link) != 0)
                        _activeDropEffect = DragDropEffects.Link;
                    else
                        _activeDropEffect = DragDropEffects.Copy;

                    _isDragOverActive = true;
                    ApplyTransparentHoverChromeState(animate: true);
                }
            }

            if (_dragHasFileDrop)
            {
                e.Effects = _activeDropEffect;
                e.Handled = true;
            }
        }

        private void OnFencePreviewDragOver(object sender, DragEventArgs e)
        {
            if (_dragLeaveDebounceTimer != null)
                _dragLeaveDebounceTimer.Stop();

            if (_dragHasFileDrop)
            {
                UpdateReorderInsertionIndicator(e);
                e.Effects = _activeDropEffect;
                e.Handled = true;
            }
        }

        private void HideReorderInsertionIndicator()
        {
            _currentReorderInsertIndex = -1;
            if (_reorderInsertionIndicator != null)
                _reorderInsertionIndicator.Visibility = Visibility.Collapsed;
        }

        private int ComputeDropInsertionIndex(DragEventArgs e, out Point indicatorCanvasPos, out double indicatorHeight)
        {
            indicatorCanvasPos = new Point(14, 14);
            indicatorHeight = 68;

            if (_currentOrderedPaths == null || _currentOrderedPaths.Count == 0 || _marqueeOverlayCanvas == null)
                return 0;

            Point ptInCanvas = e.GetPosition(_marqueeOverlayCanvas);

            // Find the closest tile to the drop cursor (row-aware in WrapPanel)
            int bestIndex = _currentOrderedPaths.Count;
            double bestDist = double.MaxValue;
            Rect bestRect = Rect.Empty;
            bool insertAfterBest = false;

            for (int i = 0; i < _currentOrderedPaths.Count; i++)
            {
                string path = _currentOrderedPaths[i];
                Border tile;
                if (!_tileBorders.TryGetValue(path, out tile) || !tile.IsVisible)
                    continue;

                try
                {
                    Point tl = tile.TranslatePoint(new Point(0, 0), _marqueeOverlayCanvas);
                    Rect r = new Rect(tl.X, tl.Y, tile.ActualWidth, tile.ActualHeight);

                    // Weight vertical distance more strongly so we snap to the cursor's current row in WrapPanel
                    double cy = r.Top + r.Height * 0.5;
                    double cx = r.Left + r.Width * 0.5;
                    double dy = Math.Abs(ptInCanvas.Y - cy);
                    double dx = Math.Abs(ptInCanvas.X - cx);
                    double dist = dy * 3.2 + dx;

                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestIndex = i;
                        bestRect = r;
                        insertAfterBest = ptInCanvas.X > cx;
                    }
                }
                catch { }
            }

            if (bestRect.IsEmpty)
                return _currentOrderedPaths.Count;

            indicatorHeight = Math.Max(44, bestRect.Height - 6);
            double barX = insertAfterBest
                ? Math.Min(_marqueeOverlayCanvas.ActualWidth - 6, bestRect.Right + 2)
                : Math.Max(2, bestRect.Left - 6);
            double barY = bestRect.Top + 3;
            indicatorCanvasPos = new Point(barX, barY);

            return insertAfterBest ? (bestIndex + 1) : bestIndex;
        }

        private void UpdateReorderInsertionIndicator(DragEventArgs e)
        {
            if (_reorderInsertionIndicator == null)
                return;

            DragGhostCursorAdorner.UpdatePosition();

            Point barPos;
            double barH;
            _currentReorderInsertIndex = ComputeDropInsertionIndex(e, out barPos, out barH);

            _reorderInsertionIndicator.Height = barH;
            Canvas.SetLeft(_reorderInsertionIndicator, barPos.X);
            Canvas.SetTop(_reorderInsertionIndicator, barPos.Y);
            if (_reorderInsertionIndicator.Visibility != Visibility.Visible)
                _reorderInsertionIndicator.Visibility = Visibility.Visible;
        }

        private void OnFencePreviewDragLeave(object sender, DragEventArgs e)
        {
            // Debounce child-element DragLeave events so dragging over tiles never flickers or restarts animations
            if (_dragLeaveDebounceTimer == null)
            {
                _dragLeaveDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(85) };
                _dragLeaveDebounceTimer.Tick += (_, __) =>
                {
                    _dragLeaveDebounceTimer.Stop();
                    _isDragOverActive = false;
                    _dragHasFileDrop = false;
                    HideReorderInsertionIndicator();
                    ApplyTransparentHoverChromeState(animate: true);
                };
            }
            _dragLeaveDebounceTimer.Stop();
            _dragLeaveDebounceTimer.Start();
        }

        private void OnFencePreviewDrop(object sender, DragEventArgs e)
        {
            if (_dragLeaveDebounceTimer != null)
                _dragLeaveDebounceTimer.Stop();

            Point dummyPos;
            double dummyH;
            int targetInsertIndex = ComputeDropInsertionIndex(e, out dummyPos, out dummyH);
            HideReorderInsertionIndicator();

            _isDragOverActive = false;
            _dragHasFileDrop = false;

            string[] droppedFiles = null;
            List<VirtualDesktopItemInfo> virtualDrops = null;

            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    droppedFiles = e.Data.GetData(DataFormats.FileDrop) as string[];
                }
                else if (e.Data.GetDataPresent("Shell IDList Array"))
                {
                    List<string> parsedFiles;
                    virtualDrops = VirtualDesktopShellHelper.ParseShellIdListArray(e.Data, out parsedFiles);
                    if (parsedFiles != null && parsedFiles.Count > 0)
                        droppedFiles = parsedFiles.ToArray();
                }
            }
            catch { }

            e.Effects = _activeDropEffect;
            e.Handled = true;

            bool hasRealFiles = droppedFiles != null && droppedFiles.Length > 0;
            bool hasVirtualItems = virtualDrops != null && virtualDrops.Count > 0;

            if (!hasRealFiles && !hasVirtualItems)
            {
                ApplyTransparentHoverChromeState(animate: true);
                return;
            }

            // Check if this is an internal icon drag-reorder within the SAME fence!
            string thisFenceDir = ConfigManager.GetFenceFolderPath(Model.Id);
            if (hasRealFiles && !hasVirtualItems &&
                droppedFiles.All(f => !string.IsNullOrEmpty(f) &&
                    string.Equals(System.IO.Path.GetDirectoryName(f), thisFenceDir, StringComparison.OrdinalIgnoreCase)))
            {
                ReorderIconsInThisFence(droppedFiles, targetInsertIndex);
                ApplyTransparentHoverChromeState(animate: true);
                return;
            }

            string[] batchCopy = hasRealFiles ? droppedFiles.ToArray() : new string[0];
            var virtualCopy = hasVirtualItems ? new List<VirtualDesktopItemInfo>(virtualDrops) : new List<VirtualDesktopItemInfo>();
            int insertIdxCopy = targetInsertIndex;
            Dispatcher.BeginInvoke(new Action(() => ProcessDroppedFilesFast(batchCopy, virtualCopy, insertIdxCopy)), DispatcherPriority.Render);
        }

        private void ReorderIconsInThisFence(string[] draggedPaths, int targetInsertIndex)
        {
            if (draggedPaths == null || draggedPaths.Length == 0)
                return;

            // Ensure current visual order is captured in Model.OrderedFileNames
            var currentNames = _currentOrderedPaths
                .Select(System.IO.Path.GetFileName)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();

            var draggedNames = draggedPaths
                .Select(System.IO.Path.GetFileName)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();

            if (draggedNames.Count == 0)
                return;

            // Count how many dragged items were strictly BEFORE targetInsertIndex so index adjusts cleanly after removal
            int clampedTarget = Math.Max(0, Math.Min(targetInsertIndex, currentNames.Count));
            int removedBeforeTarget = 0;
            for (int i = 0; i < clampedTarget; i++)
            {
                if (draggedNames.Contains(currentNames[i], StringComparer.OrdinalIgnoreCase))
                    removedBeforeTarget++;
            }

            currentNames.RemoveAll(n => draggedNames.Contains(n, StringComparer.OrdinalIgnoreCase));
            int finalInsertIdx = Math.Max(0, Math.Min(clampedTarget - removedBeforeTarget, currentNames.Count));
            currentNames.InsertRange(finalInsertIdx, draggedNames);

            // Switch to Manual Order ("None") if the user manually dragged icons to reorder them, or keep "None" updated
            Model.OrderedFileNames = currentNames;
            if (!string.Equals(Model.SortBy, "None", StringComparison.OrdinalIgnoreCase))
            {
                Model.SortBy = "None";
                Model.SortDescending = false;
            }

            PopulateIcons();
            ConfigManager.SaveAsync();
        }

        private void ProcessDroppedFilesFast(string[] droppedFiles, List<VirtualDesktopItemInfo> virtualDrops, int targetInsertIndex = -1)
        {
            var affectedSourceFences = new HashSet<FenceWindow>();
            var newlyInsertedFileNames = new List<string>();
            SetWatcherEnabled(false);
            try
            {
                bool anyMoved = false;
                string fencesRoot = ConfigManager.FencesStorageRoot;

                if (virtualDrops != null && virtualDrops.Count > 0)
                {
                    foreach (var w in App.Instance.ActiveWindows)
                    {
                        if (w != this) affectedSourceFences.Add(w);
                    }
                    foreach (var vItem in virtualDrops)
                    {
                        if (FenceFileTransferService.MoveVirtualItemIntoFence(vItem, Model))
                        {
                            anyMoved = true;
                            string lastAdded = Model.OrderedFileNames.LastOrDefault();
                            if (!string.IsNullOrEmpty(lastAdded))
                                newlyInsertedFileNames.Add(lastAdded);
                        }
                    }
                }

                foreach (string file in droppedFiles)
                {
                    if (!string.IsNullOrEmpty(file) && file.StartsWith(fencesRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        string srcDir = System.IO.Path.GetDirectoryName(file);
                        var srcWin = App.Instance.ActiveWindows.FirstOrDefault(w =>
                            string.Equals(ConfigManager.GetFenceFolderPath(w.Model.Id), srcDir, StringComparison.OrdinalIgnoreCase));
                        if (srcWin != null && srcWin != this)
                        {
                            srcWin.SetWatcherEnabled(false);
                            affectedSourceFences.Add(srcWin);
                        }
                    }

                    if (FenceFileTransferService.MoveItemIntoFence(file, Model))
                    {
                        anyMoved = true;
                        string lastAdded = Model.OrderedFileNames.LastOrDefault();
                        if (!string.IsNullOrEmpty(lastAdded))
                            newlyInsertedFileNames.Add(lastAdded);
                    }
                }

                if (anyMoved)
                {
                    // If Auto-Sort is disabled ("None") and the user dropped at a specific slot, place them right there!
                    if (targetInsertIndex >= 0 &&
                        newlyInsertedFileNames.Count > 0 &&
                        string.Equals(string.IsNullOrWhiteSpace(Model.SortBy) ? "None" : Model.SortBy, "None", StringComparison.OrdinalIgnoreCase))
                    {
                        Model.OrderedFileNames.RemoveAll(n => newlyInsertedFileNames.Contains(n, StringComparer.OrdinalIgnoreCase));
                        int ins = Math.Max(0, Math.Min(targetInsertIndex, Model.OrderedFileNames.Count));
                        Model.OrderedFileNames.InsertRange(ins, newlyInsertedFileNames);
                    }

                    string userDesktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    try
                    {
                        NativeMethods.SHChangeNotify(
                            NativeMethods.SHCNE_UPDATEDIR,
                            NativeMethods.SHCNF_PATHW | NativeMethods.SHCNF_FLUSHNOWAIT,
                            userDesktop,
                            null);
                    }
                    catch { }

                    PopulateIcons();
                    foreach (var srcWin in affectedSourceFences)
                        srcWin.PopulateIcons();

                    ConfigManager.SaveAsync();
                }
            }
            finally
            {
                SetWatcherEnabled(true);
                foreach (var srcWin in affectedSourceFences)
                    srcWin.SetWatcherEnabled(true);
                ApplyTransparentHoverChromeState(animate: true);
            }
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (_titleEditBox.Visibility == Visibility.Visible)
                return;

            if (e.Key == Key.Escape)
            {
                CancelActiveInteractions();
                ClearSelection();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.A && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                _selectedPaths.Clear();
                foreach (var k in _tileBorders.Keys)
                    _selectedPaths.Add(k);
                RefreshTileSelectionVisuals();
                e.Handled = true;
                return;
            }

            if (_selectedPaths.Count == 0)
                return;

            string primary = _selectedPaths.First();

            if (e.Key == Key.Enter)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                    ShellIconExtractor.ShowProperties(primary);
                else
                {
                    foreach (var p in _selectedPaths.ToList())
                        ShellIconExtractor.OpenItem(p);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.F2)
            {
                PromptRenameFile(primary);
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                bool permanent = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                ShellIconExtractor.SendToRecycleBin(_selectedPaths.ToList(), permanent);
                PopulateIcons();
                e.Handled = true;
            }
        }

        private void PromptRenameFile(string fullPath)
        {
            string currentName = System.IO.Path.GetFileName(fullPath);
            string input = PromptDialog.Show("Rename Item", "Enter a new name:", currentName);
            if (!string.IsNullOrWhiteSpace(input) && input != currentName)
            {
                try
                {
                    string dir = System.IO.Path.GetDirectoryName(fullPath);
                    string newPath = System.IO.Path.Combine(dir, input);
                    if (Directory.Exists(fullPath))
                        Directory.Move(fullPath, newPath);
                    else if (File.Exists(fullPath))
                        File.Move(fullPath, newPath);

                    ShellIconExtractor.InvalidateCache(fullPath);
                    PopulateIcons();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to rename:\n" + ex.Message, "Desktop Fences", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        #endregion

        #region Theme Visuals, Header Menu & Roll-Up

        private Color GetEffectiveAccentColor()
        {
            if (!string.IsNullOrEmpty(Model.CustomColorHex))
                return ThemeCatalog.ParseColor(Model.CustomColorHex, Color.FromRgb(30, 58, 138));
            var theme = ThemeCatalog.GetTheme(Model.ThemeName);
            return ThemeCatalog.ParseColor(theme.AccentHex, Color.FromRgb(30, 58, 138));
        }

        private bool IsTransparentHoverTheme()
        {
            if (!string.IsNullOrEmpty(Model.CustomColorHex))
                return false;
            var theme = ThemeCatalog.GetTheme(Model.ThemeName);
            return theme != null && theme.IsTransparentHover;
        }

        private bool ShouldRevealTransparentChrome()
        {
            return IsMouseOver ||
                   _isDragOverActive ||
                   _isPopupMenuOpen ||
                   _isResizing ||
                   _isMovingFence ||
                   _isMarqueeActive ||
                   (_titleEditBox != null && _titleEditBox.Visibility == Visibility.Visible);
        }

        private static ResourceDictionary _slimScrollBarResources;
        private static void ApplySlimOverlayScrollbarStyle(ScrollViewer sv)
        {
            try
            {
                if (_slimScrollBarResources == null)
                {
                    const string xaml = @"
<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style TargetType='{x:Type ScrollBar}'>
        <Setter Property='Width' Value='6'/>
        <Setter Property='MinWidth' Value='6'/>
        <Setter Property='Background' Value='Transparent'/>
        <Setter Property='BorderThickness' Value='0'/>
        <Setter Property='Template'>
            <Setter.Value>
                <ControlTemplate TargetType='{x:Type ScrollBar}'>
                    <Grid Background='Transparent' Width='6' Margin='0,4,2,4'>
                        <Track Name='PART_Track' IsDirectionReversed='True'>
                            <Track.Thumb>
                                <Thumb>
                                    <Thumb.Template>
                                        <ControlTemplate TargetType='{x:Type Thumb}'>
                                            <Border CornerRadius='3' Background='#6EFFFFFF' />
                                        </ControlTemplate>
                                    </Thumb.Template>
                                </Thumb>
                            </Track.Thumb>
                        </Track>
                    </Grid>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</ResourceDictionary>";
                    _slimScrollBarResources = (ResourceDictionary)XamlReader.Parse(xaml);
                    if (Application.Current != null)
                    {
                        Application.Current.Resources.MergedDictionaries.Add(_slimScrollBarResources);
                    }
                }
                sv.Resources.MergedDictionaries.Add(_slimScrollBarResources);
            }
            catch { }
        }

        private void UpdateScrollbarHoverState(bool showScrollbar, bool animate)
        {
            if (_scrollViewer == null) return;
            try
            {
                _scrollViewer.ApplyTemplate();
                var vBar = _scrollViewer.Template != null
                    ? _scrollViewer.Template.FindName("PART_VerticalScrollBar", _scrollViewer) as ScrollBar
                    : null;
                if (vBar == null) return;

                vBar.IsHitTestVisible = showScrollbar;
                double targetOpacity = showScrollbar ? 1.0 : 0.0;
                if (animate)
                {
                    var anim = new DoubleAnimation(targetOpacity, TimeSpan.FromMilliseconds(showScrollbar ? 120 : 160));
                    vBar.BeginAnimation(UIElement.OpacityProperty, anim);
                }
                else
                {
                    vBar.BeginAnimation(UIElement.OpacityProperty, null);
                    vBar.Opacity = targetOpacity;
                }
            }
            catch { }
        }

        private void ApplyTransparentHoverChromeState(bool animate)
        {
            if (_rootBorder == null || _headerBar == null)
                return;

            bool isDark = ConfigManager.Current.IsDarkMode;
            var theme = ThemeCatalog.GetTheme(Model.ThemeName);
            Color accent = GetEffectiveAccentColor();
            Color bodyBase = ThemeCatalog.ParseColor(
                isDark ? theme.DarkBodyHex : theme.LightBodyHex,
                isDark ? Color.FromRgb(16, 25, 53) : Color.FromRgb(232, 239, 253));

            bool reveal = ShouldRevealTransparentChrome();
            UpdateScrollbarHoverState(reveal, animate);

            if (!IsTransparentHoverTheme())
            {
                // Standard solid/frosted glass color themes
                byte bgAlpha = (byte)Math.Max(70, Math.Min(245, (int)(ConfigManager.Current.FenceOpacity * 255)));
                _rootBorder.Background = new SolidColorBrush(Color.FromArgb(bgAlpha, bodyBase.R, bodyBase.G, bodyBase.B));
                if (_frameStrokeBorder != null)
                    _frameStrokeBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(isDark ? (byte)165 : (byte)180, accent.R, accent.G, accent.B));
                _headerBar.Background = new SolidColorBrush(Color.FromArgb(isDark ? (byte)135 : (byte)155, accent.R, accent.G, accent.B));
                _headerBar.BorderBrush = new SolidColorBrush(Color.FromArgb(95, accent.R, accent.G, accent.B));
                _headerBar.BeginAnimation(UIElement.OpacityProperty, null);
                _headerBar.Opacity = 1.0;
                _headerBar.IsHitTestVisible = true;
                _emptyHintText.Opacity = 0.62;
                return;
            }

            // Transparent Hover-Reveal Theme:
            // Idle = 100% see-through desktop wallpaper + hidden border + hidden header/settings + hidden scrollbar!
            // Hovered = Crisp border + sleek header bar & settings buttons + slim pill scrollbar pop out smoothly!
            if (reveal)
            {
                _rootBorder.Background = new SolidColorBrush(
                    isDark ? Color.FromArgb(44, 13, 19, 31) : Color.FromArgb(62, 248, 250, 252));
                if (_frameStrokeBorder != null)
                    _frameStrokeBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(205, accent.R, accent.G, accent.B));
                _headerBar.Background = new SolidColorBrush(
                    isDark ? Color.FromArgb(195, 17, 24, 39) : Color.FromArgb(215, 241, 245, 249));
                _headerBar.BorderBrush = new SolidColorBrush(Color.FromArgb(120, accent.R, accent.G, accent.B));
                _headerBar.IsHitTestVisible = true;
                _emptyHintText.Opacity = 0.72;

                if (animate)
                {
                    var fadeIn = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(120));
                    _headerBar.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                }
                else
                {
                    _headerBar.BeginAnimation(UIElement.OpacityProperty, null);
                    _headerBar.Opacity = 1.0;
                }
            }
            else
            {
                // Alpha = 1 keeps Win32 mouse-enter & drop hit-testing active while being 100% invisible!
                _rootBorder.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
                if (_frameStrokeBorder != null)
                    _frameStrokeBorder.BorderBrush = Brushes.Transparent;
                _headerBar.IsHitTestVisible = false;
                _emptyHintText.Opacity = 0.0;

                if (animate)
                {
                    var fadeOut = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(160));
                    _headerBar.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                }
                else
                {
                    _headerBar.BeginAnimation(UIElement.OpacityProperty, null);
                    _headerBar.Opacity = 0.0;
                }
            }
        }

        public void UpdateThemeVisuals()
        {
            bool isDark = ConfigManager.Current.IsDarkMode;
            Color accent = GetEffectiveAccentColor();

            _marqueeRect.Background = new SolidColorBrush(Color.FromArgb(55, accent.R, accent.G, accent.B));
            _marqueeRect.BorderBrush = new SolidColorBrush(Color.FromArgb(215, accent.R, accent.G, accent.B));

            if (isDark)
            {
                var fg = new SolidColorBrush(Color.FromRgb(243, 244, 246));
                _titleText.Foreground = fg;
                _badgeText.Foreground = new SolidColorBrush(Color.FromRgb(209, 213, 219));
                _emptyHintText.Foreground = new SolidColorBrush(Color.FromRgb(209, 213, 219));
                _colorSwatchBtn.Foreground = fg;
                _rollUpBtn.Foreground = fg;

                _darkLightPill.Background = new SolidColorBrush(Color.FromArgb(90, 15, 23, 42));
                _darkLightPill.BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255));
                _darkLightPillText.Foreground = fg;
                _darkLightPillText.Text = "🌙 Dark";
            }
            else
            {
                var fg = new SolidColorBrush(Color.FromRgb(17, 24, 39));
                _titleText.Foreground = fg;
                _badgeText.Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55));
                _emptyHintText.Foreground = new SolidColorBrush(IsTransparentHoverTheme() ? Color.FromRgb(243, 244, 246) : Color.FromRgb(55, 65, 81));
                _colorSwatchBtn.Foreground = fg;
                _rollUpBtn.Foreground = fg;

                _darkLightPill.Background = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255));
                _darkLightPill.BorderBrush = new SolidColorBrush(Color.FromArgb(90, 17, 24, 39));
                _darkLightPillText.Foreground = fg;
                _darkLightPillText.Text = "☀ Light";
            }

            ApplyTransparentHoverChromeState(animate: false);

            if (IsInitialized && new WindowInteropHelper(this).Handle != IntPtr.Zero)
            {
                WindowBackdropManager.ApplyFrostedGlass(this, isDark);
                PopulateIcons();
            }
        }

        private void OpenColorThemeMenu()
        {
            var menu = new ContextMenu();
            menu.Opened += (_, __) => { _isPopupMenuOpen = true; ApplyTransparentHoverChromeState(animate: false); };
            menu.Closed += (_, __) => { _isPopupMenuOpen = false; ApplyTransparentHoverChromeState(animate: true); };

            foreach (var t in ThemeCatalog.Themes)
            {
                var themeDef = t;
                string label = themeDef.IsTransparentHover
                    ? "✨ Transparent (Hover Reveal)"
                    : themeDef.Name + " (" + themeDef.AccentHex + ")";
                var mi = new MenuItem
                {
                    Header = label,
                    IsCheckable = true,
                    IsChecked = string.IsNullOrEmpty(Model.CustomColorHex) &&
                                string.Equals(Model.ThemeName, themeDef.Name, StringComparison.OrdinalIgnoreCase)
                };
                mi.Click += (_, __) =>
                {
                    Model.ThemeName = themeDef.Name;
                    Model.CustomColorHex = "";
                    UpdateThemeVisuals();
                    ConfigManager.Save();
                };
                menu.Items.Add(mi);
            }

            menu.Items.Add(new Separator());
            var customHexItem = new MenuItem { Header = "🎨 Custom Hex Color..." };
            customHexItem.Click += (_, __) =>
            {
                string input = PromptDialog.Show("Custom Fence Color", "Enter a Hex color (e.g. #1E3A8A or #EC4899):",
                    string.IsNullOrEmpty(Model.CustomColorHex) ? "#1E3A8A" : Model.CustomColorHex);
                if (!string.IsNullOrWhiteSpace(input))
                {
                    Model.CustomColorHex = input.Trim();
                    UpdateThemeVisuals();
                    ConfigManager.Save();
                }
            };
            menu.Items.Add(customHexItem);

            menu.PlacementTarget = _colorSwatchBtn;
            menu.IsOpen = true;
        }

        private static string GetSortDisplayName(string path)
        {
            if (VirtualDesktopShellHelper.IsVirtualItemFile(path))
            {
                var vInfo = VirtualDesktopShellHelper.ReadVirtualFile(path);
                if (vInfo != null && !string.IsNullOrEmpty(vInfo.DisplayName))
                    return vInfo.DisplayName;
                return System.IO.Path.GetFileNameWithoutExtension(path) ?? "";
            }
            bool isDir = Directory.Exists(path);
            if (!isDir && (path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".url", StringComparison.OrdinalIgnoreCase)))
                return System.IO.Path.GetFileNameWithoutExtension(path) ?? "";
            return System.IO.Path.GetFileName(path) ?? "";
        }

        private List<string> SortFenceEntries(List<string> diskEntries)
        {
            string mode = string.IsNullOrWhiteSpace(Model.SortBy) ? "None" : Model.SortBy.Trim();
            if (string.Equals(mode, "None", StringComparison.OrdinalIgnoreCase))
            {
                return diskEntries
                    .OrderBy(p =>
                    {
                        int idx = Model.OrderedFileNames.FindIndex(n => string.Equals(n, System.IO.Path.GetFileName(p), StringComparison.OrdinalIgnoreCase));
                        return idx >= 0 ? idx : int.MaxValue;
                    })
                    .ThenBy(p => System.IO.Path.GetFileName(p))
                    .ToList();
            }

            var sorted = new List<string>(diskEntries);
            bool desc = Model.SortDescending;

            sorted.Sort((a, b) =>
            {
                int cmp = 0;
                if (string.Equals(mode, "Name", StringComparison.OrdinalIgnoreCase))
                {
                    cmp = NativeMethods.StrCmpLogicalW(GetSortDisplayName(a), GetSortDisplayName(b));
                }
                else if (string.Equals(mode, "Size", StringComparison.OrdinalIgnoreCase))
                {
                    long sizeA = Directory.Exists(a) ? -1L : (File.Exists(a) ? new FileInfo(a).Length : 0L);
                    long sizeB = Directory.Exists(b) ? -1L : (File.Exists(b) ? new FileInfo(b).Length : 0L);
                    cmp = sizeA.CompareTo(sizeB);
                    if (cmp == 0)
                        cmp = NativeMethods.StrCmpLogicalW(GetSortDisplayName(a), GetSortDisplayName(b));
                }
                else if (string.Equals(mode, "ItemType", StringComparison.OrdinalIgnoreCase))
                {
                    bool dirA = Directory.Exists(a);
                    bool dirB = Directory.Exists(b);
                    if (dirA != dirB)
                        cmp = dirA ? -1 : 1;
                    else
                    {
                        cmp = NativeMethods.StrCmpLogicalW(ShellIconExtractor.GetItemTypeName(a), ShellIconExtractor.GetItemTypeName(b));
                        if (cmp == 0)
                            cmp = NativeMethods.StrCmpLogicalW(GetSortDisplayName(a), GetSortDisplayName(b));
                    }
                }
                else if (string.Equals(mode, "DateModified", StringComparison.OrdinalIgnoreCase))
                {
                    DateTime dtA = Directory.Exists(a) ? Directory.GetLastWriteTimeUtc(a) : File.GetLastWriteTimeUtc(a);
                    DateTime dtB = Directory.Exists(b) ? Directory.GetLastWriteTimeUtc(b) : File.GetLastWriteTimeUtc(b);
                    cmp = dtA.CompareTo(dtB);
                    if (cmp == 0)
                        cmp = NativeMethods.StrCmpLogicalW(GetSortDisplayName(a), GetSortDisplayName(b));
                }

                return desc ? -cmp : cmp;
            });

            return sorted;
        }

        public void SetFenceSortMode(string sortBy, bool? descendingOverride = null)
        {
            string current = string.IsNullOrWhiteSpace(Model.SortBy) ? "None" : Model.SortBy;
            if (descendingOverride.HasValue)
            {
                Model.SortBy = sortBy;
                Model.SortDescending = descendingOverride.Value;
            }
            else if (!string.Equals(sortBy, "None", StringComparison.OrdinalIgnoreCase) &&
                     string.Equals(current, sortBy, StringComparison.OrdinalIgnoreCase))
            {
                // Clicking the already-active option reverses Ascending <-> Descending (native Windows Desktop behavior)
                Model.SortDescending = !Model.SortDescending;
            }
            else
            {
                Model.SortBy = sortBy;
                Model.SortDescending = false;
            }

            PopulateIcons();
            ConfigManager.SaveAsync();
        }

        private MenuItem CreateRadioOptionMenuItem(string label, bool isSelected, Action onClick)
        {
            var radioVisual = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = isSelected ? new SolidColorBrush(Color.FromRgb(59, 130, 246)) : Brushes.Transparent,
                Stroke = new SolidColorBrush(isSelected ? Color.FromRgb(59, 130, 246) : Color.FromRgb(148, 163, 184)),
                StrokeThickness = 1.5,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var mi = new MenuItem
            {
                Header = label,
                Icon = radioVisual,
                FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Normal
            };
            mi.Click += (_, __) => onClick();
            return mi;
        }

        private MenuItem BuildSortBySubmenu()
        {
            string activeMode = string.IsNullOrWhiteSpace(Model.SortBy) ? "None" : Model.SortBy;
            string dirArrow = Model.SortDescending ? " ↓" : " ↑";

            var sortRoot = new MenuItem
            {
                Header = "⇅ Sort by",
                FontWeight = FontWeights.SemiBold
            };

            sortRoot.Items.Add(CreateRadioOptionMenuItem(
                "Name" + (string.Equals(activeMode, "Name", StringComparison.OrdinalIgnoreCase) ? dirArrow : ""),
                string.Equals(activeMode, "Name", StringComparison.OrdinalIgnoreCase),
                () => SetFenceSortMode("Name")));

            sortRoot.Items.Add(CreateRadioOptionMenuItem(
                "Size" + (string.Equals(activeMode, "Size", StringComparison.OrdinalIgnoreCase) ? dirArrow : ""),
                string.Equals(activeMode, "Size", StringComparison.OrdinalIgnoreCase),
                () => SetFenceSortMode("Size")));

            sortRoot.Items.Add(CreateRadioOptionMenuItem(
                "Item Type" + (string.Equals(activeMode, "ItemType", StringComparison.OrdinalIgnoreCase) ? dirArrow : ""),
                string.Equals(activeMode, "ItemType", StringComparison.OrdinalIgnoreCase),
                () => SetFenceSortMode("ItemType")));

            sortRoot.Items.Add(CreateRadioOptionMenuItem(
                "Date Modified" + (string.Equals(activeMode, "DateModified", StringComparison.OrdinalIgnoreCase) ? dirArrow : ""),
                string.Equals(activeMode, "DateModified", StringComparison.OrdinalIgnoreCase),
                () => SetFenceSortMode("DateModified")));

            sortRoot.Items.Add(new Separator());

            bool isManual = string.Equals(activeMode, "None", StringComparison.OrdinalIgnoreCase);
            if (!isManual)
            {
                sortRoot.Items.Add(CreateRadioOptionMenuItem(
                    "Ascending",
                    !Model.SortDescending,
                    () => SetFenceSortMode(activeMode, false)));

                sortRoot.Items.Add(CreateRadioOptionMenuItem(
                    "Descending",
                    Model.SortDescending,
                    () => SetFenceSortMode(activeMode, true)));

                sortRoot.Items.Add(new Separator());
            }

            sortRoot.Items.Add(CreateRadioOptionMenuItem(
                "Manual Order (Auto-Sort Off)",
                isManual,
                () => SetFenceSortMode("None", false)));

            sortRoot.Items.Add(new Separator());
            var openDialogItem = new MenuItem { Header = "⚙ Auto-Sort Option Buttons Panel..." };
            openDialogItem.Click += (_, __) => OpenAutoSortDialog();
            sortRoot.Items.Add(openDialogItem);

            return sortRoot;
        }

        private void OpenAutoSortDialog()
        {
            var dlg = new Window
            {
                Title = "Auto-Sort Settings — " + Model.Title,
                Width = 340,
                Height = 315,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(18, 21, 30)),
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Poppins, Segoe UI Variable Display, Segoe UI")
            };

            var root = new StackPanel { Margin = new Thickness(20) };
            root.Children.Add(new TextBlock
            {
                Text = "Sort by:",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            string currentMode = string.IsNullOrWhiteSpace(Model.SortBy) ? "None" : Model.SortBy;
            var options = new[]
            {
                new KeyValuePair<string, string>("Name", "Name"),
                new KeyValuePair<string, string>("Size", "Size"),
                new KeyValuePair<string, string>("ItemType", "Item Type"),
                new KeyValuePair<string, string>("DateModified", "Date Modified"),
                new KeyValuePair<string, string>("None", "Manual Order (Disable Auto-Sort)")
            };

            foreach (var opt in options)
            {
                string key = opt.Key;
                var rb = new RadioButton
                {
                    GroupName = "SortByGroup",
                    Content = opt.Value,
                    Foreground = Brushes.White,
                    FontSize = 13,
                    Margin = new Thickness(4, 0, 0, 8),
                    IsChecked = string.Equals(currentMode, key, StringComparison.OrdinalIgnoreCase),
                    Cursor = Cursors.Hand
                };
                rb.Checked += (_, __) => SetFenceSortMode(key, Model.SortDescending);
                root.Children.Add(rb);
            }

            root.Children.Add(new TextBlock
            {
                Text = "Order:",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 8, 0, 6),
                Foreground = new SolidColorBrush(Color.FromRgb(209, 213, 219))
            });

            var orderRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 0, 14) };
            var ascRb = new RadioButton
            {
                GroupName = "SortDirGroup",
                Content = "Ascending",
                Foreground = Brushes.White,
                FontSize = 12.5,
                Margin = new Thickness(0, 0, 18, 0),
                IsChecked = !Model.SortDescending,
                Cursor = Cursors.Hand
            };
            ascRb.Checked += (_, __) =>
            {
                string m = string.IsNullOrWhiteSpace(Model.SortBy) ? "Name" : Model.SortBy;
                SetFenceSortMode(m, false);
            };

            var descRb = new RadioButton
            {
                GroupName = "SortDirGroup",
                Content = "Descending",
                Foreground = Brushes.White,
                FontSize = 12.5,
                IsChecked = Model.SortDescending,
                Cursor = Cursors.Hand
            };
            descRb.Checked += (_, __) =>
            {
                string m = string.IsNullOrWhiteSpace(Model.SortBy) ? "Name" : Model.SortBy;
                SetFenceSortMode(m, true);
            };

            orderRow.Children.Add(ascRb);
            orderRow.Children.Add(descRb);
            root.Children.Add(orderRow);

            var doneBtn = new Button
            {
                Content = "Done",
                Width = 86,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Right,
                IsDefault = true,
                Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            doneBtn.Click += (_, __) => dlg.Close();
            root.Children.Add(doneBtn);

            dlg.Content = root;
            dlg.ShowDialog();
        }

        private void SetFenceMovementMode(string mode, bool holdLocked)
        {
            Model.MoveMode = mode;
            Model.HoldPosition = holdLocked;
            ConfigManager.SaveAsync();
        }

        private MenuItem BuildHoldMovementSubmenu()
        {
            string mode = string.IsNullOrWhiteSpace(Model.MoveMode) ? "Anywhere" : Model.MoveMode;
            bool isLocked = IsFenceMovementLocked();

            var moveRoot = new MenuItem
            {
                Header = isLocked ? "🔒 Hold Movement (Locked)" : "✥ Hold & Movement Setting",
                FontWeight = FontWeights.SemiBold
            };

            moveRoot.Items.Add(CreateRadioOptionMenuItem(
                "Move by Holding Left-Click Anywhere in Box",
                !isLocked && string.Equals(mode, "Anywhere", StringComparison.OrdinalIgnoreCase),
                () => SetFenceMovementMode("Anywhere", false)));

            moveRoot.Items.Add(CreateRadioOptionMenuItem(
                "Hold Left-Click (0.35s Delay) to Move Box",
                !isLocked && string.Equals(mode, "HoldDelay", StringComparison.OrdinalIgnoreCase),
                () => SetFenceMovementMode("HoldDelay", false)));

            moveRoot.Items.Add(CreateRadioOptionMenuItem(
                "Move from Top Header Bar Only",
                !isLocked && string.Equals(mode, "HeaderOnly", StringComparison.OrdinalIgnoreCase),
                () => SetFenceMovementMode("HeaderOnly", false)));

            moveRoot.Items.Add(CreateRadioOptionMenuItem(
                "🔒 Hold / Lock Fence in Place (No Accidental Drag)",
                isLocked,
                () =>
                {
                    ConfigManager.Current.LockLayout = false;
                    SetFenceMovementMode("Locked", true);
                }));

            moveRoot.Items.Add(new Separator());

            var lockAllItem = new MenuItem
            {
                Header = ConfigManager.Current.LockLayout
                    ? "🔓 Unlock Movement on All Fences"
                    : "🔒 Hold / Lock Movement on All Fences"
            };
            lockAllItem.Click += (_, __) =>
            {
                bool targetLock = !ConfigManager.Current.LockLayout;
                ConfigManager.Current.LockLayout = targetLock;
                if (!targetLock)
                {
                    foreach (var f in ConfigManager.Current.Fences)
                    {
                        f.HoldPosition = false;
                        if (string.Equals(f.MoveMode, "Locked", StringComparison.OrdinalIgnoreCase))
                            f.MoveMode = "Anywhere";
                    }
                }
                ConfigManager.SaveAsync();
            };
            moveRoot.Items.Add(lockAllItem);

            return moveRoot;
        }

        private ContextMenu BuildFenceHeaderContextMenu()
        {
            var menu = new ContextMenu();
            menu.Opened += (_, __) => { _isPopupMenuOpen = true; ApplyTransparentHoverChromeState(animate: false); };
            menu.Closed += (_, __) => { _isPopupMenuOpen = false; ApplyTransparentHoverChromeState(animate: true); };

            // Top-level Sort by + Hold Movement Controls right at the top when right-clicking inside a fence!
            menu.Items.Add(BuildSortBySubmenu());
            var sortDialogItem = new MenuItem { Header = "⚙ Auto-Sort Settings..." };
            sortDialogItem.Click += (_, __) => OpenAutoSortDialog();
            menu.Items.Add(sortDialogItem);

            menu.Items.Add(new Separator());

            bool isLocked = IsFenceMovementLocked();
            var quickHoldLockItem = new MenuItem
            {
                Header = "🔒 Hold / Lock Fence Position (Prevent Accidental Drag)",
                IsCheckable = true,
                IsChecked = isLocked,
                FontWeight = FontWeights.SemiBold
            };
            quickHoldLockItem.Click += (_, __) =>
            {
                if (isLocked)
                {
                    ConfigManager.Current.LockLayout = false;
                    SetFenceMovementMode("Anywhere", false);
                }
                else
                {
                    SetFenceMovementMode("Locked", true);
                }
            };
            menu.Items.Add(quickHoldLockItem);
            menu.Items.Add(BuildHoldMovementSubmenu());

            menu.Items.Add(new Separator());

            var newFenceItem = new MenuItem { Header = "＋ Create New Fence (Ctrl+Alt+N)", FontWeight = FontWeights.SemiBold };
            newFenceItem.Click += (_, __) => App.Instance.CreateNewFenceInteractive(Left + 40, Top + 40, 380, 260);
            menu.Items.Add(newFenceItem);

            var renameItem = new MenuItem { Header = "✎ Rename Fence (Double-Click Title)" };
            renameItem.Click += (_, __) => BeginInlineRename();
            menu.Items.Add(renameItem);

            var colorMenu = new MenuItem { Header = "🎨 Fence Color Theme (10 Themes)" };
            foreach (var t in ThemeCatalog.Themes)
            {
                var themeDef = t;
                var sub = new MenuItem
                {
                    Header = themeDef.Name,
                    IsCheckable = true,
                    IsChecked = string.Equals(Model.ThemeName, themeDef.Name, StringComparison.OrdinalIgnoreCase)
                };
                sub.Click += (_, __) =>
                {
                    Model.ThemeName = themeDef.Name;
                    Model.CustomColorHex = "";
                    UpdateThemeVisuals();
                    ConfigManager.Save();
                };
                colorMenu.Items.Add(sub);
            }
            menu.Items.Add(colorMenu);

            var darkLightItem = new MenuItem
            {
                Header = ConfigManager.Current.IsDarkMode ? "☀ Switch to Light Mode" : "🌙 Switch to Dark Mode"
            };
            darkLightItem.Click += (_, __) => App.Instance.ToggleDarkLightMode();
            menu.Items.Add(darkLightItem);

            menu.Items.Add(new Separator());

            var restoreFenceIconsItem = new MenuItem { Header = "↩ Restore All Icons in This Fence to Desktop" };
            restoreFenceIconsItem.Click += (_, __) =>
            {
                FenceFileTransferService.RestoreFenceFolderToDesktop(Model.Id);
                PopulateIcons();
            };
            menu.Items.Add(restoreFenceIconsItem);

            var settingsItem = new MenuItem { Header = "⚙ Settings & Control Center..." };
            settingsItem.Click += (_, __) => App.Instance.OpenSettingsWindow();
            menu.Items.Add(settingsItem);

            menu.Items.Add(new Separator());

            var deleteFenceItem = new MenuItem { Header = "🗑 Delete Fence (Auto-Restores Its Icons to Desktop)" };
            deleteFenceItem.Click += (_, __) => App.Instance.RemoveFence(this);
            menu.Items.Add(deleteFenceItem);

            return menu;
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            DesktopHostManager.AttachToDesktopLayer(this);
            WindowBackdropManager.ApplyFrostedGlass(this, ConfigManager.Current.IsDarkMode);
            PopulateIcons();
        }

        private void HeaderBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                BeginInlineRename();
                e.Handled = true;
                return;
            }

            if (e.ButtonState == MouseButtonState.Pressed && !IsFenceMovementLocked())
            {
                DragMove();
                ApplyMagneticSnap();
                ConfigManager.SaveAsync();
            }
        }

        private void ApplyMagneticSnap()
        {
            Rect workArea = SystemParameters.WorkArea;
            if (Math.Abs(Left - workArea.Left) < SnapThreshold) Left = workArea.Left + 8;
            if (Math.Abs(Top - workArea.Top) < SnapThreshold) Top = workArea.Top + 8;
            if (Math.Abs((Left + Width) - workArea.Right) < SnapThreshold) Left = workArea.Right - Width - 8;
            if (Math.Abs((Top + Height) - workArea.Bottom) < SnapThreshold) Top = workArea.Bottom - Height - 8;

            foreach (var other in App.Instance.ActiveWindows)
            {
                if (other == this || !other.IsVisible) continue;

                if (Math.Abs(Left - (other.Left + other.Width + 10)) < SnapThreshold)
                    Left = other.Left + other.Width + 10;
                if (Math.Abs((Left + Width + 10) - other.Left) < SnapThreshold)
                    Left = other.Left - Width - 10;
                if (Math.Abs(Top - other.Top) < SnapThreshold)
                    Top = other.Top;
            }
        }

        private void OnLocationChanged(object sender, EventArgs e)
        {
            Model.Left = Left;
            Model.Top = Top;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Model.Width = Width;
            if (!Model.IsRolledUp && Height > 50)
            {
                Model.Height = Height;
                Model.ExpandedHeight = Height;
            }
        }

        private void UpdateHeaderCornerRadius(bool collapsedStrip)
        {
            if (_headerBar != null)
            {
                _headerBar.CornerRadius = collapsedStrip
                    ? new CornerRadius(11.5)
                    : new CornerRadius(11.5, 11.5, 0, 0);
            }
        }

        public void ToggleRollUp()
        {
            Model.IsRolledUp = !Model.IsRolledUp;
            if (Model.IsRolledUp)
            {
                if (Height > 50) Model.ExpandedHeight = Height;
                Height = 40;
                _rollUpBtn.Content = "▸";
                UpdateHeaderCornerRadius(true);
            }
            else
            {
                Height = Math.Max(Model.ExpandedHeight, 180);
                _rollUpBtn.Content = "▾";
                UpdateHeaderCornerRadius(false);
            }
            ConfigManager.Save();
        }

        private void OnWindowMouseEnter(object sender, MouseEventArgs e)
        {
            if (Model.IsRolledUp)
            {
                Height = Math.Max(Model.ExpandedHeight, 180);
                UpdateHeaderCornerRadius(false);
            }

            ApplyTransparentHoverChromeState(animate: true);
        }

        private void OnWindowMouseLeave(object sender, MouseEventArgs e)
        {
            if (Model.IsRolledUp && !_isPopupMenuOpen)
            {
                Height = 40;
                UpdateHeaderCornerRadius(true);
            }

            ApplyTransparentHoverChromeState(animate: true);
        }

        private void BeginInlineRename()
        {
            _titleEditBox.Text = Model.Title;
            _titleText.Visibility = Visibility.Collapsed;
            _titleEditBox.Visibility = Visibility.Visible;
            ApplyTransparentHoverChromeState(animate: false);
            _titleEditBox.Focus();
            _titleEditBox.SelectAll();
        }

        private void CommitInlineRename()
        {
            if (_titleEditBox.Visibility != Visibility.Visible)
                return;

            string candidate = _titleEditBox.Text.Trim();
            if (!string.IsNullOrEmpty(candidate))
            {
                Model.Title = candidate;
                _titleText.Text = candidate;
                ConfigManager.Save();
            }

            _titleEditBox.Visibility = Visibility.Collapsed;
            _titleText.Visibility = Visibility.Visible;
            ApplyTransparentHoverChromeState(animate: true);
        }

        private void TitleEditBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommitInlineRename();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                _titleEditBox.Visibility = Visibility.Collapsed;
                _titleText.Visibility = Visibility.Visible;
                e.Handled = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try { if (_fenceFolderWatcher != null) _fenceFolderWatcher.Dispose(); }
            catch { }
            base.OnClosed(e);
        }

        #endregion
    }

    #endregion

    #region 6. Helper Dialogs & Settings Window with Top-Right Compact Dark/Light Pill Toggle

    public static class PromptDialog
    {
        public static string Show(string title, string label, string defaultValue)
        {
            var win = new Window
            {
                Title = title,
                Width = 400,
                Height = 175,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(22, 24, 33)),
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Poppins, Segoe UI Variable Display, Segoe UI")
            };

            var stack = new StackPanel { Margin = new Thickness(16) };
            stack.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 12.5,
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = new SolidColorBrush(Color.FromRgb(209, 213, 219))
            });

            var box = new TextBox
            {
                Text = defaultValue ?? "",
                FontSize = 13,
                Padding = new Thickness(6, 5, 6, 5),
                Background = new SolidColorBrush(Color.FromRgb(32, 36, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(75, 85, 99))
            };
            stack.Children.Add(box);

            var btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 14, 0, 0)
            };

            string result = null;
            var okBtn = new Button
            {
                Content = "Save",
                Width = 80,
                Height = 28,
                Margin = new Thickness(0, 0, 8, 0),
                IsDefault = true,
                Background = new SolidColorBrush(Color.FromRgb(30, 58, 138)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            okBtn.Click += (_, __) => { result = box.Text; win.Close(); };

            var cancelBtn = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 28,
                IsCancel = true,
                Background = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };

            btnRow.Children.Add(okBtn);
            btnRow.Children.Add(cancelBtn);
            stack.Children.Add(btnRow);
            win.Content = stack;

            win.Loaded += (_, __) => { box.Focus(); box.SelectAll(); };
            win.ShowDialog();
            return result;
        }
    }

    public class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            Title = "Desktop Fences v3.0 — Untouched Desktop & Color Themes";
            Width = 540;
            Height = 470;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(18, 20, 29));
            Foreground = Brushes.White;
            FontFamily = new FontFamily("Poppins, Segoe UI Variable Display, Segoe UI");

            var root = new StackPanel { Margin = new Thickness(22) };

            // Top row with Title and Small Dark/Light Pill Switch
            var topHeader = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            topHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            topHeader.Children.Add(new TextBlock
            {
                Text = "Desktop Fences v3.0",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            });

            var pillBtn = new Button
            {
                Content = ConfigManager.Current.IsDarkMode ? "🌙 Dark Mode" : "☀ Light Mode",
                Padding = new Thickness(12, 4, 12, 4),
                Background = new SolidColorBrush(Color.FromRgb(30, 58, 138)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            pillBtn.Click += (_, __) =>
            {
                App.Instance.ToggleDarkLightMode();
                pillBtn.Content = ConfigManager.Current.IsDarkMode ? "🌙 Dark Mode" : "☀ Light Mode";
            };
            Grid.SetColumn(pillBtn, 1);
            topHeader.Children.Add(pillBtn);
            root.Children.Add(topHeader);

            root.Children.Add(new TextBlock
            {
                Text = "Native Windows desktop is 100% untouched. Drag one or multiple selected icons into any fence to move them with zero duplicates.",
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                Margin = new Thickness(0, 0, 0, 14)
            });

            // Global Theme Dropdown (10 Themes including Navy Blue & Rose Pink)
            root.Children.Add(new TextBlock
            {
                Text = "Apply Color Theme to All Fences:",
                FontSize = 12.5,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var themeCombo = new ComboBox
            {
                Margin = new Thickness(0, 0, 0, 14),
                Padding = new Thickness(6, 4, 6, 4)
            };
            foreach (var t in ThemeCatalog.Themes)
                themeCombo.Items.Add(t.Name);
            themeCombo.SelectedIndex = 0;
            themeCombo.SelectionChanged += (_, __) =>
            {
                string chosen = themeCombo.SelectedItem as string;
                if (!string.IsNullOrEmpty(chosen))
                {
                    foreach (var f in ConfigManager.Current.Fences)
                    {
                        f.ThemeName = chosen;
                        f.CustomColorHex = "";
                    }
                    ConfigManager.Save();
                    App.Instance.RefreshAllFencesTheme();
                }
            };
            root.Children.Add(themeCombo);

            var lockBox = new CheckBox
            {
                Content = "Lock Fence Layout (prevent moving or resizing fences)",
                IsChecked = ConfigManager.Current.LockLayout,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            lockBox.Checked += (_, __) => { ConfigManager.Current.LockLayout = true; ConfigManager.Save(); };
            lockBox.Unchecked += (_, __) => { ConfigManager.Current.LockLayout = false; ConfigManager.Save(); };
            root.Children.Add(lockBox);

            var startupBox = new CheckBox
            {
                Content = "Start Desktop Fences automatically with Windows",
                IsChecked = IsStartupEnabled(),
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 14)
            };
            startupBox.Checked += (_, __) => SetStartup(true);
            startupBox.Unchecked += (_, __) => SetStartup(false);
            root.Children.Add(startupBox);

            root.Children.Add(new TextBlock { Text = "Frosted Glass Opacity:", FontSize = 12.5, Margin = new Thickness(0, 0, 0, 4) });
            var opacitySlider = new Slider
            {
                Minimum = 0.25,
                Maximum = 0.98,
                Value = ConfigManager.Current.FenceOpacity,
                Margin = new Thickness(0, 0, 0, 12)
            };
            opacitySlider.ValueChanged += (_, e) =>
            {
                ConfigManager.Current.FenceOpacity = e.NewValue;
                App.Instance.RefreshAllFencesTheme();
                ConfigManager.Save();
            };
            root.Children.Add(opacitySlider);

            root.Children.Add(new TextBlock { Text = "Icon Size (px):", FontSize = 12.5, Margin = new Thickness(0, 0, 0, 4) });
            var sizeSlider = new Slider
            {
                Minimum = 28,
                Maximum = 64,
                Value = ConfigManager.Current.IconSizePx,
                Margin = new Thickness(0, 0, 0, 12)
            };
            sizeSlider.ValueChanged += (_, e) =>
            {
                ConfigManager.Current.IconSizePx = (int)e.NewValue;
                App.Instance.RefreshAllFences();
                ConfigManager.Save();
            };
            root.Children.Add(sizeSlider);

            // Spotlight filter
            root.Children.Add(new TextBlock { Text = "Quick Spotlight Filter (highlight matching icons across fences):", FontSize = 12.5, Margin = new Thickness(0, 0, 0, 4) });
            var searchBox = new TextBox
            {
                FontSize = 12.5,
                Padding = new Thickness(6, 4, 6, 4),
                Background = new SolidColorBrush(Color.FromRgb(32, 36, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(75, 85, 99)),
                Margin = new Thickness(0, 0, 0, 14)
            };
            searchBox.TextChanged += (_, __) => App.Instance.FilterAllFences(searchBox.Text);
            root.Children.Add(searchBox);

            var actionsPanel = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Left };
            actionsPanel.Children.Add(CreateActionButton("＋ New Fence (Ctrl+Alt+N)", () => App.Instance.CreateNewFenceInteractive(440, 140, 380, 260)));
            actionsPanel.Children.Add(CreateActionButton("↩ Restore All Icons to Desktop", () =>
            {
                int count = FenceFileTransferService.RestoreAllFencesToDesktop();
                App.Instance.RefreshAllFences();
                MessageBox.Show("Restored " + count + " item(s) safely back to your native Windows Desktop.", "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }));

            root.Children.Add(actionsPanel);
            Content = root;
        }

        private Button CreateActionButton(string text, Action onClick)
        {
            var btn = new Button
            {
                Content = text,
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 8, 8),
                Background = new SolidColorBrush(Color.FromRgb(30, 58, 138)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btn.Click += (_, __) => onClick();
            return btn;
        }

        public static bool IsStartupEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key != null && key.GetValue("DesktopFences") != null;
                }
            }
            catch { return false; }
        }

        public static void SetStartup(bool enable)
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                            key.SetValue("DesktopFences", "\"" + exePath + "\"");
                        else
                            key.DeleteValue("DesktopFences", false);
                    }
                }

                // Eliminate Windows 10/11 Explorer 10-second Startup Delay & register zero-delay Logon Task + NGEN native image!
                ConfigureZeroDelayWindowsStartup(exePath, enable);
            }
            catch { }
        }

        public static void ConfigureZeroDelayWindowsStartup(string exePath, bool enable)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (enable)
                    {
                        // 1. Disable Windows Explorer's artificial 10-second StartupDelayInMSec so Desktop Fences
                        // launches the exact millisecond the Windows Desktop appears!
                        using (var serializeKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize"))
                        {
                            if (serializeKey != null)
                            {
                                serializeKey.SetValue("StartupDelayInMSec", 0, RegistryValueKind.DWord);
                                serializeKey.SetValue("WaitForIdleState", 0, RegistryValueKind.DWord);
                            }
                        }

                        // 2. Also ensure a shortcut exists in %APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
                        string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                        if (!string.IsNullOrEmpty(startupFolder) && Directory.Exists(startupFolder))
                        {
                            string startupLnk = System.IO.Path.Combine(startupFolder, "Desktop Fences.lnk");
                            CreateShellShortcut(startupLnk, exePath, "Desktop Fences Instant Startup");
                        }

                        // 3. Pre-compile DesktopFences.exe into Windows Native Image Cache (NGEN) so cold-boot JIT takes 0ms
                        string ngen64 = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework64\v4.0.30319\ngen.exe");
                        if (File.Exists(ngen64))
                            RunHiddenCommand(ngen64, "install \"" + exePath + "\" /nologo");
                    }
                    else
                    {
                        string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                        if (!string.IsNullOrEmpty(startupFolder))
                        {
                            string startupLnk = System.IO.Path.Combine(startupFolder, "Desktop Fences.lnk");
                            if (File.Exists(startupLnk)) File.Delete(startupLnk);
                        }
                    }
                }
                catch { }
            });
        }

        private static void CreateShellShortcut(string lnkPath, string targetExe, string description)
        {
            try
            {
                Type t = Type.GetTypeFromCLSID(new Guid("00021401-0000-0000-C000-000000000046"));
                var link = (NativeMethods.IShellLinkW)Activator.CreateInstance(t);
                link.SetPath(targetExe);
                link.SetWorkingDirectory(System.IO.Path.GetDirectoryName(targetExe));
                link.SetDescription(description ?? "Desktop Fences");
                ((NativeMethods.IPersistFile)link).Save(lnkPath, true);
            }
            catch { }
        }

        private static void RunHiddenCommand(string fileName, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo(fileName, arguments)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                var p = Process.Start(psi);
                if (p != null) p.WaitForExit(4000);
            }
            catch { }
        }
    }

    #endregion

    #region 7. Main Application Controller (Untouched Native Desktop, Tray & Hotkeys)

    /// <summary>
    /// Windows OS Explorer-style floating translucent icon drag ghost & action badge that follows the cursor
    /// smoothly during drag-and-drop sorting/moving without intercepting mouse hit-testing (WS_EX_TRANSPARENT).
    /// </summary>
    internal sealed class DragGhostCursorAdorner : Window
    {
        private static DragGhostCursorAdorner _instance;
        private readonly Image _iconImg;
        private readonly TextBlock _labelBlock;
        private readonly Border _countBadge;
        private readonly TextBlock _countText;
        private readonly Border _actionPill;
        private readonly TextBlock _actionText;

        private DragGhostCursorAdorner()
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = true;
            IsHitTestVisible = false;
            Focusable = false;
            SizeToContent = SizeToContent.WidthAndHeight;
            FontFamily = new FontFamily("Poppins, Segoe UI Variable Display, Segoe UI");

            var outerCard = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(190, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(225, 96, 165, 250)),
                BorderThickness = new Thickness(1.4),
                Padding = new Thickness(10, 8, 10, 7),
                Margin = new Thickness(6),
                Opacity = 0.90,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 12,
                    ShadowDepth = 3,
                    Opacity = 0.65
                }
            };

            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var iconGrid = new Grid
            {
                Width = 46,
                Height = 46,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            _iconImg = new Image
            {
                Width = 42,
                Height = 42,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(_iconImg, BitmapScalingMode.HighQuality);
            iconGrid.Children.Add(_iconImg);

            _countText = new TextBlock
            {
                Text = "2",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _countBadge = new Border
            {
                MinWidth = 18,
                Height = 18,
                Padding = new Thickness(4, 0, 4, 0),
                CornerRadius = new CornerRadius(9),
                Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1.2),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Visibility = Visibility.Collapsed,
                Child = _countText
            };
            iconGrid.Children.Add(_countBadge);
            stack.Children.Add(iconGrid);

            _labelBlock = new TextBlock
            {
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                MaxWidth = 115,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 3, 0, 0)
            };
            stack.Children.Add(_labelBlock);

            _actionText = new TextBlock
            {
                Text = "➔ Move",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(191, 219, 254)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _actionPill = new Border
            {
                CornerRadius = new CornerRadius(5),
                Background = new SolidColorBrush(Color.FromArgb(145, 30, 58, 138)),
                Padding = new Thickness(6, 1, 6, 1),
                Margin = new Thickness(0, 4, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = _actionText
            };
            stack.Children.Add(_actionPill);

            outerCard.Child = stack;
            Content = outerCard;

            SourceInitialized += (_, __) =>
            {
                try
                {
                    IntPtr hwnd = new WindowInteropHelper(this).Handle;
                    long ex = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
                    ex |= NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_LAYERED;
                    NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(ex));
                }
                catch { }
            };
        }

        public static void Show(DesktopIconItem item, int count)
        {
            try
            {
                if (_instance == null)
                    _instance = new DragGhostCursorAdorner();

                _instance._iconImg.Source = item != null ? item.IconSource : null;
                _instance._labelBlock.Text = item != null ? item.DisplayName : "Item";

                if (count > 1)
                {
                    _instance._countText.Text = count.ToString();
                    _instance._countBadge.Visibility = Visibility.Visible;
                }
                else
                {
                    _instance._countBadge.Visibility = Visibility.Collapsed;
                }

                _instance._actionText.Text = "➔ Move / Sort";
                UpdatePosition();
                if (!_instance.IsVisible)
                    _instance.Show();
            }
            catch { }
        }

        public static void UpdatePositionAndEffect(DragDropEffects effects)
        {
            if (_instance == null) return;
            try
            {
                if ((effects & DragDropEffects.Move) != 0)
                    _instance._actionText.Text = "➔ Move / Reorder";
                else if ((effects & DragDropEffects.Copy) != 0)
                    _instance._actionText.Text = "＋ Copy";
                else if ((effects & DragDropEffects.Link) != 0)
                    _instance._actionText.Text = "🔗 Link";
                UpdatePosition();
            }
            catch { }
        }

        public static void UpdatePosition()
        {
            if (_instance == null) return;
            try
            {
                NativeMethods.POINT pt;
                if (!NativeMethods.GetCursorPos(out pt)) return;

                double dpiX = 1.0, dpiY = 1.0;
                var src = PresentationSource.FromVisual(_instance);
                if (src != null && src.CompositionTarget != null)
                {
                    dpiX = src.CompositionTarget.TransformToDevice.M11;
                    dpiY = src.CompositionTarget.TransformToDevice.M22;
                }

                // Offset slightly below-right of the hardware pointer just like Windows Explorer drag preview
                _instance.Left = (pt.X / dpiX) + 14;
                _instance.Top = (pt.Y / dpiY) + 14;
            }
            catch { }
        }

        public static void HideGhost()
        {
            try
            {
                if (_instance != null && _instance.IsVisible)
                    _instance.Hide();
            }
            catch { }
        }
    }

    /// <summary>
    /// Bug Fix: selected icons stayed highlighted after clicking the desktop / refreshing outside the fence.
    /// While (and only while) any fence has a selection, this polls the physical mouse buttons. When a NEW
    /// click lands on a window that does not belong to Desktop Fences (wallpaper, desktop icons, desktop
    /// right-click menu, taskbar, other apps), every fence's selection is cleared, just like Windows Explorer.
    /// Clicks on our own fences, context menus and dialogs are ignored (those handle selection themselves).
    /// </summary>
    /// <summary>
    /// Automatically exposes all fenced files, archives, documents, and folders inside "This PC > Desktop"
    /// whenever ANY Web Browser File Upload dialog or "Open Workspace / Select Folder" dialog (#32770) is open,
    /// using zero-copy NTFS Hardlinks (for files) and Directory Junctions (for folders), and removes them
    /// the instant the dialog closes so the desktop wallpaper always remains 100% free of duplicate icons!
    /// </summary>
    internal static class FileDialogDesktopProjectionWatcher
    {
        private static DispatcherTimer _timer;
        private static bool _isProjected;
        private static readonly HashSet<string> _projectedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static void Start()
        {
            if (_timer != null) return;
            // Clean up any leftover projections from an unexpected shutdown before starting
            CleanupStaleMarkerProjections();

            _timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(220)
            };
            _timer.Tick += OnTick;
            _timer.Start();
        }

        public static void StopAndCleanup()
        {
            try
            {
                if (_timer != null) _timer.Stop();
                if (_isProjected)
                    UnprojectFromDesktop();
            }
            catch { }
        }

        private static string MarkerFilePath
        {
            get { return System.IO.Path.Combine(ConfigManager.ConfigDirectory, "dialog-projections.txt"); }
        }

        private static void CleanupStaleMarkerProjections()
        {
            try
            {
                if (!File.Exists(MarkerFilePath)) return;
                foreach (string p in File.ReadAllLines(MarkerFilePath, Encoding.UTF8))
                {
                    RemoveSingleProjectedPath(p);
                }
                File.Delete(MarkerFilePath);
            }
            catch { }
        }

        private static void OnTick(object sender, EventArgs e)
        {
            bool dialogOpen = IsCommonFileOrFolderDialogOpen();
            if (dialogOpen && !_isProjected)
            {
                ProjectFencedItemsToDesktop();
            }
            else if (!dialogOpen && _isProjected)
            {
                UnprojectFromDesktop();
            }
        }

        private static bool IsCommonFileOrFolderDialogOpen()
        {
            bool found = false;
            try
            {
                NativeMethods.EnumWindows((topHwnd, _) =>
                {
                    if (!NativeMethods.IsWindowVisible(topHwnd))
                        return true;

                    var sb = new StringBuilder(64);
                    NativeMethods.GetClassName(topHwnd, sb, 64);
                    if (sb.ToString() == "#32770")
                    {
                        // Common File Open / Save / Upload / Select Folder dialogs host DUIViewWndClassName, WorkerW, or SHBrowseForFolder Shell embedding
                        if (NativeMethods.FindWindowEx(topHwnd, IntPtr.Zero, "DUIViewWndClassName", null) != IntPtr.Zero ||
                            NativeMethods.FindWindowEx(topHwnd, IntPtr.Zero, "WorkerW", null) != IntPtr.Zero ||
                            NativeMethods.FindWindowEx(topHwnd, IntPtr.Zero, "SHBrowseForFolder ShellNameSpace Control", null) != IntPtr.Zero)
                        {
                            found = true;
                            return false;
                        }
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
            return found;
        }

        private static void ProjectFencedItemsToDesktop()
        {
            _isProjected = true;
            _projectedPaths.Clear();

            try
            {
                string userDesktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (string.IsNullOrEmpty(userDesktop) || !Directory.Exists(userDesktop))
                    return;

                if (ConfigManager.Current == null || ConfigManager.Current.Fences == null)
                    return;

                // 1. Project a master "Desktop Fences (All Desktop Files & Folders)" junction AND per-fence folder junctions
                foreach (var fence in ConfigManager.Current.Fences)
                {
                    string fenceDir = ConfigManager.GetFenceFolderPath(fence.Id);
                    if (!Directory.Exists(fenceDir)) continue;

                    // Expose the Fence itself as a folder on Desktop (e.g. "[Fence] Documents & Files") so even nested items are browsable
                    string safeTitle = string.Join("_", (fence.Title ?? "Fence").Split(System.IO.Path.GetInvalidFileNameChars())).Trim();
                    if (!string.IsNullOrEmpty(safeTitle))
                    {
                        string fenceJunction = System.IO.Path.Combine(userDesktop, "[Fence] " + safeTitle);
                        if (!Directory.Exists(fenceJunction) && !File.Exists(fenceJunction))
                        {
                            if (CreateDirectoryJunctionFast(fenceJunction, fenceDir))
                                _projectedPaths.Add(fenceJunction);
                        }
                    }

                    // 2. Project every real folder inside the fence directly onto This PC > Desktop via zero-copy NTFS Directory Junction
                    foreach (string subDir in Directory.GetDirectories(fenceDir))
                    {
                        string name = System.IO.Path.GetFileName(subDir);
                        if (string.IsNullOrEmpty(name)) continue;
                        string destLink = System.IO.Path.Combine(userDesktop, name);
                        if (Directory.Exists(destLink) || File.Exists(destLink))
                            continue;

                        if (CreateDirectoryJunctionFast(destLink, subDir))
                            _projectedPaths.Add(destLink);
                    }

                    // 3. Project every real file/document/zip inside the fence directly onto This PC > Desktop (skipping only .vfence virtual markers)
                    foreach (string file in Directory.GetFiles(fenceDir))
                    {
                        string name = System.IO.Path.GetFileName(file);
                        if (string.IsNullOrEmpty(name)) continue;
                        if (name.EndsWith("desktop.ini", StringComparison.OrdinalIgnoreCase) ||
                            name.EndsWith(VirtualDesktopShellHelper.VirtualExt, StringComparison.OrdinalIgnoreCase))
                            continue;

                        string destFile = System.IO.Path.Combine(userDesktop, name);
                        if (File.Exists(destFile) || Directory.Exists(destFile))
                            continue;

                        if (NativeMethods.CreateHardLink(destFile, file, IntPtr.Zero))
                            _projectedPaths.Add(destFile);
                    }
                }

                if (_projectedPaths.Count > 0)
                {
                    try { File.WriteAllLines(MarkerFilePath, _projectedPaths.ToArray(), Encoding.UTF8); } catch { }
                }
            }
            catch { }
        }

        private static void UnprojectFromDesktop()
        {
            _isProjected = false;
            try
            {
                foreach (string p in _projectedPaths.ToList())
                {
                    RemoveSingleProjectedPath(p);
                }
                _projectedPaths.Clear();
                if (File.Exists(MarkerFilePath))
                    File.Delete(MarkerFilePath);

                string userDesktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (!string.IsNullOrEmpty(userDesktop))
                {
                    NativeMethods.SHChangeNotify(
                        NativeMethods.SHCNE_UPDATEDIR,
                        NativeMethods.SHCNF_PATHW | NativeMethods.SHCNF_FLUSHNOWAIT,
                        userDesktop,
                        null);
                }
            }
            catch { }
        }

        private static void RemoveSingleProjectedPath(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return;
                if (Directory.Exists(path))
                {
                    var di = new DirectoryInfo(path);
                    // Only remove if it is a ReparsePoint (Directory Junction) so real folders are untouched
                    if ((di.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        Directory.Delete(path, false);
                    }
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch { }
        }

        private static bool CreateDirectoryJunctionFast(string linkPath, string targetPath)
        {
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", "/c mklink /J \"" + linkPath + "\" \"" + targetPath + "\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                var p = Process.Start(psi);
                if (p != null) p.WaitForExit(1500);
                return Directory.Exists(linkPath);
            }
            catch { return false; }
        }
    }

    internal static class OutsideClickSelectionWatcher
    {
        private const int VK_LBUTTON = 0x01;
        private const int VK_RBUTTON = 0x02;
        private const int VK_MBUTTON = 0x04;

        private static DispatcherTimer _timer;
        private static bool _wasLeftDown, _wasRightDown, _wasMiddleDown;
        private static uint _ownProcessId;

        public static void Start()
        {
            if (_timer == null)
            {
                _ownProcessId = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
                _timer = new DispatcherTimer(DispatcherPriority.Input) { Interval = TimeSpan.FromMilliseconds(25) };
                _timer.Tick += OnTick;
            }
            if (_timer.IsEnabled)
                return;

            // Snapshot current state (and reset the "pressed since last call" bits) so the click that
            // CREATED the selection is never mistaken for an outside click.
            _wasLeftDown = Poll(VK_LBUTTON);
            _wasRightDown = Poll(VK_RBUTTON);
            _wasMiddleDown = Poll(VK_MBUTTON);
            _timer.Start();
        }

        private static bool Poll(int vk)
        {
            return (NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0;
        }

        private static bool NewPress(int vk, ref bool wasDown)
        {
            short state = NativeMethods.GetAsyncKeyState(vk);
            bool down = (state & 0x8000) != 0;
            bool pressedSinceLast = (state & 0x0001) != 0; // catches very quick clicks between ticks
            bool isNew = (down && !wasDown) || (!down && pressedSinceLast);
            wasDown = down;
            return isNew;
        }

        private static void OnTick(object sender, EventArgs e)
        {
            var app = App.Instance;
            if (app == null || app.ActiveWindows == null || !app.ActiveWindows.Any(w => w.HasSelection))
            {
                _timer.Stop();
                return;
            }

            bool l = NewPress(VK_LBUTTON, ref _wasLeftDown);
            bool r = NewPress(VK_RBUTTON, ref _wasRightDown);
            bool m = NewPress(VK_MBUTTON, ref _wasMiddleDown);
            if (!(l || r || m))
                return;

            if (IsCursorOverOwnWindow())
                return;

            app.ClearSelectionInAllFences();
            _timer.Stop();
        }

        private static bool IsCursorOverOwnWindow()
        {
            try
            {
                NativeMethods.POINT pt;
                if (!NativeMethods.GetCursorPos(out pt))
                    return true; // fail safe: never clear on unknown state
                IntPtr hwnd = NativeMethods.WindowFromPoint(pt);
                if (hwnd == IntPtr.Zero)
                    return false;
                uint pid;
                NativeMethods.GetWindowThreadProcessId(hwnd, out pid);
                return pid == _ownProcessId;
            }
            catch
            {
                return true;
            }
        }
    }

    public class App : Application
    {
        public static App Instance { get; private set; }
        public List<FenceWindow> ActiveWindows { get; private set; }

        private Mutex _singleInstanceMutex;
        private Forms.NotifyIcon _trayIcon;
        private bool _isQuickHidden;
        private Window _hotkeyMessageWindow;
        private const int HOTKEY_ID_PEEK = 9001;
        private const int HOTKEY_ID_QUICKHIDE = 9002;
        private const int HOTKEY_ID_NEWFENCE = 9003;

        public App()
        {
            Instance = this;
            ActiveWindows = new List<FenceWindow>();
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        [STAThread]
        public static void Main(string[] args)
        {
            // Always restore native Windows desktop icons first thing on launch
            DesktopHostManager.EnsureNativeDesktopIconsVisible();

            if (args != null && args.Any(a => string.Equals(a, "--restore-icons", StringComparison.OrdinalIgnoreCase)))
            {
                FenceFileTransferService.RestoreAllFencesToDesktop();
                SettingsWindow.SetStartup(false);
                return;
            }

            var app = new App();
            app.Run();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            bool isNew;
            _singleInstanceMutex = new Mutex(true, @"Local\DesktopFences_SingleInstance_Mutex", out isNew);
            if (!isNew)
            {
                Shutdown(0);
                return;
            }

            // FAST-PATH STARTUP: Load config and render fence windows on the VERY FIRST frame (<150ms)
            // before initializing background tray icons, hotkeys, or desktop shell walks!
            try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal; } catch { }
            ShellIconExtractor.EnsureImageLists();
            ConfigManager.Load();
            SpawnFenceWindows();

            // Defer non-visual startup work until after all fences are visible on screen
            Dispatcher.BeginInvoke(new Action(() =>
            {
                DesktopHostManager.EnsureNativeDesktopIconsVisible();
                InitializeSystemTray();
                InitializeGlobalHotkeys();
                FileDialogDesktopProjectionWatcher.Start();
                if (SettingsWindow.IsStartupEnabled())
                {
                    SettingsWindow.ConfigureZeroDelayWindowsStartup(
                        Process.GetCurrentProcess().MainModule.FileName,
                        true);
                }
            }), DispatcherPriority.Background);
        }

        private void SpawnFenceWindows()
        {
            foreach (var win in ActiveWindows.ToList())
                win.Close();
            ActiveWindows.Clear();

            Rect workArea = SystemParameters.WorkArea;
            foreach (var fenceModel in ConfigManager.Current.Fences)
            {
                // Ensure every fence fits cleanly inside the current monitor's visible WorkArea
                if (fenceModel.Width > workArea.Width - 40)
                    fenceModel.Width = Math.Max(260, workArea.Width * 0.45);
                if (fenceModel.Height > workArea.Height - 40)
                    fenceModel.Height = Math.Max(200, workArea.Height * 0.45);
                if (fenceModel.Left < workArea.Left || fenceModel.Left + 100 > workArea.Right)
                    fenceModel.Left = Math.Max(workArea.Left + 40, workArea.Right - fenceModel.Width - 40);
                if (fenceModel.Top < workArea.Top || fenceModel.Top + 60 > workArea.Bottom)
                    fenceModel.Top = Math.Max(workArea.Top + 40, workArea.Bottom - fenceModel.Height - 40);

                var fw = new FenceWindow(fenceModel);
                ActiveWindows.Add(fw);
                fw.Show();
            }
            ConfigManager.Save();
        }

        public void CreateNewFenceInteractive(double left, double top, double width, double height)
        {
            string title = PromptDialog.Show("Create New Fence", "Enter a title for the new fence:", "New Fence");
            if (string.IsNullOrWhiteSpace(title))
                return;

            var theme = ThemeCatalog.Themes[ConfigManager.Current.Fences.Count % ThemeCatalog.Themes.Length];
            var model = new FenceModel
            {
                Title = title.Trim(),
                Left = left,
                Top = top,
                Width = Math.Max(220, width),
                Height = Math.Max(160, height),
                ExpandedHeight = Math.Max(160, height),
                ThemeName = theme.Name
            };
            ConfigManager.GetFenceFolderPath(model.Id);
            ConfigManager.Current.Fences.Add(model);
            ConfigManager.Save();

            var fw = new FenceWindow(model);
            ActiveWindows.Add(fw);
            fw.Show();
        }

        public void RemoveFence(FenceWindow win)
        {
            if (ConfigManager.Current.Fences.Count <= 1)
            {
                MessageBox.Show("Keep at least one fence active, or exit Desktop Fences from the system tray.", "Desktop Fences", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Fail-safe: return all files inside this fence back to %USERPROFILE%\Desktop before removing it
            FenceFileTransferService.RestoreFenceFolderToDesktop(win.Model.Id);

            ConfigManager.Current.Fences.Remove(win.Model);
            ActiveWindows.Remove(win);
            win.Close();

            ConfigManager.Save();
        }

        public void ToggleDarkLightMode()
        {
            ConfigManager.Current.IsDarkMode = !ConfigManager.Current.IsDarkMode;
            ConfigManager.Save();
            RefreshAllFencesTheme();
        }

        public void RefreshAllFences()
        {
            foreach (var win in ActiveWindows)
                win.PopulateIcons();
        }

        public void RefreshAllFencesTheme()
        {
            foreach (var win in ActiveWindows)
                win.UpdateThemeVisuals();
        }

        public void FilterAllFences(string query)
        {
            foreach (var win in ActiveWindows)
                win.ApplySearchFilter(query);
        }

        public void ClearSelectionInOtherFences(FenceWindow keep)
        {
            foreach (var win in ActiveWindows.ToList())
            {
                if (!ReferenceEquals(win, keep))
                    win.ClearSelectionFromOutside();
            }
        }

        public void ClearSelectionInAllFences()
        {
            foreach (var win in ActiveWindows.ToList())
                win.ClearSelectionFromOutside();
        }

        public void ToggleQuickHide()
        {
            _isQuickHidden = !_isQuickHidden;
            foreach (var win in ActiveWindows)
            {
                if (_isQuickHidden && !win.Model.ExcludeFromQuickHide)
                    win.Hide();
                else
                    win.Show();
            }
        }

        public void TogglePeekMode()
        {
            DesktopHostManager.IsPeekModeActive = !DesktopHostManager.IsPeekModeActive;
            foreach (var win in ActiveWindows)
            {
                IntPtr hwnd = new WindowInteropHelper(win).Handle;
                if (hwnd != IntPtr.Zero)
                    DesktopHostManager.UpdateZOrder(hwnd);
            }
        }

        private void InitializeGlobalHotkeys()
        {
            try
            {
                _hotkeyMessageWindow = new Window
                {
                    Width = 0,
                    Height = 0,
                    WindowStyle = WindowStyle.None,
                    ShowInTaskbar = false,
                    Visibility = Visibility.Hidden
                };
                IntPtr hwnd = new WindowInteropHelper(_hotkeyMessageWindow).EnsureHandle();

                NativeMethods.RegisterHotKey(hwnd, HOTKEY_ID_PEEK, NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT, 0x20);     // Ctrl+Alt+Space
                NativeMethods.RegisterHotKey(hwnd, HOTKEY_ID_QUICKHIDE, NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT, 0x48); // Ctrl+Alt+H
                NativeMethods.RegisterHotKey(hwnd, HOTKEY_ID_NEWFENCE, NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT, 0x4E);  // Ctrl+Alt+N

                HwndSource source = HwndSource.FromHwnd(hwnd);
                if (source != null)
                {
                    source.AddHook((IntPtr h, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
                    {
                        if (msg == NativeMethods.WM_HOTKEY)
                        {
                            int id = wParam.ToInt32();
                            if (id == HOTKEY_ID_PEEK) { TogglePeekMode(); handled = true; }
                            else if (id == HOTKEY_ID_QUICKHIDE) { ToggleQuickHide(); handled = true; }
                            else if (id == HOTKEY_ID_NEWFENCE) { CreateNewFenceInteractive(440, 140, 380, 260); handled = true; }
                        }
                        return IntPtr.Zero;
                    });
                }
            }
            catch { }
        }

        private void InitializeSystemTray()
        {
            Drawing.Icon appIcon = Drawing.SystemIcons.Application;
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                var extracted = Drawing.Icon.ExtractAssociatedIcon(exePath);
                if (extracted != null) appIcon = extracted;
            }
            catch { }

            _trayIcon = new Forms.NotifyIcon
            {
                Text = "Desktop Fences (HD Icons & Color Themes)",
                Icon = appIcon,
                Visible = true
            };

            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("＋ Create New Fence (Ctrl+Alt+N)", null, (_, __) => Dispatcher.Invoke(() => CreateNewFenceInteractive(440, 140, 380, 260)));
            menu.Items.Add("✨ Apply Transparent Hover Theme to All Fences", null, (_, __) => Dispatcher.Invoke(() =>
            {
                foreach (var f in ConfigManager.Current.Fences)
                {
                    f.ThemeName = "Transparent";
                    f.CustomColorHex = "";
                }
                ConfigManager.Save();
                RefreshAllFencesTheme();
            }));
            menu.Items.Add("🌙 / ☀ Toggle Dark / Light Mode", null, (_, __) => Dispatcher.Invoke(ToggleDarkLightMode));
            menu.Items.Add("👁 Quick-Hide / Show Fences (Ctrl+Alt+H)", null, (_, __) => Dispatcher.Invoke(ToggleQuickHide));
            menu.Items.Add("🔝 Peek Fences on Top (Ctrl+Alt+Space)", null, (_, __) => Dispatcher.Invoke(TogglePeekMode));
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("↩ Restore All Fenced Icons to Desktop", null, (_, __) => Dispatcher.Invoke(() =>
            {
                int count = FenceFileTransferService.RestoreAllFencesToDesktop();
                RefreshAllFences();
                MessageBox.Show("Restored " + count + " item(s) back to your native Windows Desktop.", "Desktop Fences", MessageBoxButton.OK, MessageBoxImage.Information);
            }));
            menu.Items.Add("⚙ Settings & Color Themes...", null, (_, __) => Dispatcher.Invoke(OpenSettingsWindow));
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("✖ Exit Desktop Fences", null, (_, __) => Dispatcher.Invoke(() => Shutdown(0)));

            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.DoubleClick += (_, __) => Dispatcher.Invoke(OpenSettingsWindow);
        }

        private SettingsWindow _settingsWin;
        public void OpenSettingsWindow()
        {
            if (_settingsWin == null || !_settingsWin.IsLoaded)
            {
                _settingsWin = new SettingsWindow();
                _settingsWin.Closed += (_, __) => _settingsWin = null;
                _settingsWin.Show();
            }
            else
            {
                _settingsWin.Activate();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            FileDialogDesktopProjectionWatcher.StopAndCleanup();
            DesktopHostManager.EnsureNativeDesktopIconsVisible();
            try
            {
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                }
                if (_singleInstanceMutex != null)
                {
                    _singleInstanceMutex.ReleaseMutex();
                    _singleInstanceMutex.Dispose();
                }
            }
            catch { }

            base.OnExit(e);
        }
    }

    #endregion
}
