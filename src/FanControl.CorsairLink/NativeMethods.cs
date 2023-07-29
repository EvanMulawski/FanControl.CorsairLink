using System.Runtime.InteropServices;

namespace FanControl.CorsairLink
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);
    }

    internal static class MessageBoxFlags
    {
        public const uint MB_OK = 0x00000000;             // Display OK button
        public const uint MB_OKCANCEL = 0x00000001;       // Display OK and Cancel buttons
        public const uint MB_ABORTRETRYIGNORE = 0x00000002; // Display Abort, Retry, and Ignore buttons
        public const uint MB_YESNOCANCEL = 0x00000003;    // Display Yes, No, and Cancel buttons
        public const uint MB_YESNO = 0x00000004;          // Display Yes and No buttons
        public const uint MB_RETRYCANCEL = 0x00000005;    // Display Retry and Cancel buttons

        public const uint MB_ICONERROR = 0x00000010;      // Display Error (X) icon
        public const uint MB_ICONQUESTION = 0x00000020;   // Display Question (?) icon
        public const uint MB_ICONWARNING = 0x00000030;    // Display Warning (!) icon
        public const uint MB_ICONINFORMATION = 0x00000040; // Display Information (i) icon

        public const uint MB_DEFAULT_BUTTON1 = 0x00000000; // First button is default (default for MB_OK)
        public const uint MB_DEFAULT_BUTTON2 = 0x00000100; // Second button is default
        public const uint MB_DEFAULT_BUTTON3 = 0x00000200; // Third button is default
        public const uint MB_DEFAULT_BUTTON4 = 0x00000300; // Fourth button is default

        public const uint MB_APPLMODAL = 0x00000000;      // Application modal (blocks input to other windows in the same application)
        public const uint MB_SYSTEMMODAL = 0x00001000;    // System modal (blocks input to all windows)

        public const uint MB_SETFOREGROUND = 0x00010000;  // The message box becomes the foreground window
        public const uint MB_TOPMOST = 0x00040000;        // The message box is displayed as a topmost window
    }
}
