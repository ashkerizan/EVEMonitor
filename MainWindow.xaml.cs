using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MessageBox = System.Windows.MessageBox;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace EVEMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.ThisCall)]
        private static extern long GetClassName(IntPtr hwnd, StringBuilder lpClassName, long nMaxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

        private readonly List<IntPtr> _listHandles = new List<IntPtr>();

        //[StructLayout(LayoutKind.Sequential)]
        //public struct Rect
        //{
        //    public readonly int left;
        //    public readonly int top;
        //    public readonly int right;
        //    public readonly int bottom;
        //}

        private static string GetWindowText(IntPtr hWnd)
        {
            var len = GetWindowTextLength(hWnd) + 1;
            var sb = new StringBuilder(len);
            len = GetWindowText(hWnd, sb, len);
            return sb.ToString(0, len);
        }

        private void btFindEveWindows_Click(object sender, RoutedEventArgs e)
        {
            lbDebug.Items.Clear();
            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd) && GetWindowTextLength(hWnd) != 0 && GetWindowText(hWnd).StartsWith("EVE") && !GetWindowText(hWnd).StartsWith("EVEMon") && !GetClassNameOfWindow(hWnd).StartsWith("Qt5Q"))
                {
                    _listHandles.Add(hWnd);
                    lbDebug.Items.Add(hWnd.ToString() + ", " + GetWindowText(hWnd) + ", " + GetClassNameOfWindow(hWnd));
                }
                return true;
            }, IntPtr.Zero);

        }

        private static string GetClassNameOfWindow(IntPtr hWnd)
        {
            var className = "";
            try
            {
                const int clsMaxLength = 1000;
                var classText = new StringBuilder("", clsMaxLength + 5);
                GetClassName(hWnd, classText, clsMaxLength + 2);

                if (!string.IsNullOrEmpty(classText.ToString()) && !string.IsNullOrWhiteSpace(classText.ToString()))
                    className = classText.ToString();
            }
            catch (Exception ex)
            {
                className = ex.Message;
            }

            return className;
        }

        private void lbDebug_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get a handle to the Calculator application. The window class
            // and window name were obtained using the Spy++ tool.
            if (lbDebug.SelectedIndex == -1) return;
            var hndl = _listHandles[lbDebug.SelectedIndex];

            if (hndl == IntPtr.Zero)
            {
                MessageBox.Show("Eve is not running.");
                return;
            }

            var bmp = PrintWindow(hndl);
            var hBitmap = bmp.GetHbitmap();
            ImageSource wpfBitmap = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            imgScr.Source = wpfBitmap;

        }

        private static Bitmap PrintWindow(IntPtr hwnd)
        {
            var rc = new Rect();
            GetWindowRect(hwnd, ref rc);

            var bmp = new Bitmap(100, 100, PixelFormat.Format32bppArgb);
            var gfxBmp = Graphics.FromImage(bmp);
            var hdcBitmap = gfxBmp.GetHdc();

            PrintWindow(hwnd, hdcBitmap, 0);

            gfxBmp.ReleaseHdc(hdcBitmap);
            gfxBmp.Dispose();

            return bmp;
        }
    }

}
