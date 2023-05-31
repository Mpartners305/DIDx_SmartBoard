using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Player
{
    public class UserActivityHook
    {
        #region 구조체
        [StructLayout(LayoutKind.Sequential)]
        private class POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class MouseHookStruct
        {
            public POINT pt;
            public int hwnd;
            public int wHitTestCode;
            public int dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class MouseLLHookStruct
        {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class KeyboardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }
        #endregion

        #region Win32
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern int SetWindowsHookEx(int idHook, HookProc lpFn, IntPtr hMod, int dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern int UnhookWindowsHookEx(int idHook);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
        #endregion

        #region 상수
        private const int WH_MOUSE_LL       = 14;
        private const int WH_KEYBOARD_LL    = 13;
        private const int WH_MOUSE          = 7;
        private const int WH_KEYBOARD       = 2;
        private const int WM_MOUSEMOVE      = 0x200;
        private const int WM_LBUTTONDOWN    = 0x201;
        private const int WM_RBUTTONDOWN    = 0x204;
        private const int WM_MBUTTONDOWN    = 0x207;
        private const int WM_LBUTTONUP      = 0x202;
        private const int WM_RBUTTONUP      = 0x205;
        private const int WM_MBUTTONUP      = 0x208;
        private const int WM_LBUTTONDBLCLK  = 0x203;
        private const int WM_RBUTTONDBLCLK  = 0x206;
        private const int WM_MBUTTONDBLCLK  = 0x209;
        private const int WM_MOUSEWHEEL     = 0x020A;
        private const int WM_KEYDOWN        = 0x100;
        private const int WM_KEYUP          = 0x101;
        private const int WM_SYSKEYDOWN     = 0x104;
        private const int WM_SYSKEYUP       = 0x105;
        private const byte VK_SHIFT         = 0x10;
        private const byte VK_CAPITAL       = 0x14;
        private const byte VK_NUMLOCK       = 0x90;
        #endregion

        #region 생성자 소멸자
        public UserActivityHook()
        {
            Start();
        }

        ~UserActivityHook()
        {
            Stop(true, true);
        } 
        #endregion

        #region 변수
        private delegate int HookProc(int nCode, int wParam, IntPtr lParam);

        public event MouseEventHandler OnMouseActivity;
        public event KeyEventHandler KeyDown;

        private int hMouseHook = 0;
        private int hKeyboardHook = 0;

        private static HookProc MouseHookProcedure;
        private static HookProc KeyboardHookProcedure; 
        #endregion

        #region Start, Stop
        public void Start()
        {
            this.Start(true, true);
        }

        public void Start(bool InstallMouseHook, bool InstallKeyboardHook)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                if (hMouseHook == 0 && InstallMouseHook)
                {
                    MouseHookProcedure = new HookProc(MouseHookProc);
                    hMouseHook = SetWindowsHookEx(WH_MOUSE_LL, MouseHookProcedure, GetModuleHandle(curModule.ModuleName), 0);
                    if (hMouseHook == 0)
                    {
                        Stop(true, false);
                    }
                }

                if (hKeyboardHook == 0 && InstallKeyboardHook)
                {
                    KeyboardHookProcedure = new HookProc(KeyboardHookProc);
                    hKeyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, KeyboardHookProcedure, GetModuleHandle(curModule.ModuleName), 0);
                    if (hKeyboardHook == 0)
                    {
                        Stop(false, true);
                    }
                }
            }
        }

        public void Stop(bool UninstallMouseHook, bool UninstallKeyboardHook)
        {
            if (hMouseHook != 0 && UninstallMouseHook)
            {
                int retMouse = UnhookWindowsHookEx(hMouseHook);
                hMouseHook = 0;
            }

            if (hKeyboardHook != 0 && UninstallKeyboardHook)
            {
                int retKeyboard = UnhookWindowsHookEx(hKeyboardHook);
                hKeyboardHook = 0;
            }
        } 
        #endregion

        #region Mouse, keyboard Hook Proc
        private int MouseHookProc(int nCode, int wParam, IntPtr lParam)
        {
            if (nCode >= 0 && OnMouseActivity != null)
            {
                MouseLLHookStruct mouseHookStruct = (MouseLLHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseLLHookStruct));

                MouseButtons button = MouseButtons.None;
                short mouseDelta = 0;
                switch (wParam)
                {
                    case WM_LBUTTONDOWN:
                        button = MouseButtons.Left;
                        break;
                    case WM_RBUTTONDOWN:
                        button = MouseButtons.Right;
                        break;
                    case WM_MOUSEWHEEL:
                        mouseDelta = (short)((mouseHookStruct.mouseData >> 16) & 0xffff);
                        break;
                    default:
                        break;
                }

                int clickCount = 0;
                if (button != MouseButtons.None)
                    if (wParam == WM_LBUTTONDBLCLK || wParam == WM_RBUTTONDBLCLK)
                        clickCount = 2;
                    else
                        clickCount = 1;

                MouseEventArgs e = new MouseEventArgs(button, clickCount, mouseHookStruct.pt.x, mouseHookStruct.pt.y, mouseDelta);
                OnMouseActivity(this, e);
            }
            return CallNextHookEx(hMouseHook, nCode, wParam, lParam);
        }

        private int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam)
        {
            bool handled = false;
            if (nCode >= 0 && KeyDown != null)
            {
                KeyboardHookStruct MyKeyboardHookStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));
                if (KeyDown != null && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
                {
                    Keys keyData = (Keys)MyKeyboardHookStruct.vkCode;
                    KeyEventArgs e = new KeyEventArgs(keyData);
                    KeyDown(this, e);
                    handled = handled || e.Handled;
                }
            }

            if (handled)
                return 1;
            else
                return CallNextHookEx(hKeyboardHook, nCode, wParam, lParam);
        } 
        #endregion
    }
}
