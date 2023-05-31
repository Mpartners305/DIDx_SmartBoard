using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace Start
{
    public partial class _start : Window
    {
        #region 선언
        private DateTime _Receive_Time = DateTime.Now;
        #endregion

        #region 생성자 & Loaded
        public _start()
        {
            InitializeComponent();

            System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;
            this.Left = screens[0].Bounds.Left;
            this.Top = screens[0].Bounds.Top;
            this.Width = screens[0].Bounds.Width;
            this.Height = screens[0].Bounds.Height;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Update.exe 삭제
                FnProcessKill("update");
                string strUpdateExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Update.exe");
                if (File.Exists(strUpdateExePath))
                {
                    try { File.Delete(strUpdateExePath); }
                    catch (Exception) { }
                }

                IntPtr handle = new WindowInteropHelper(this).Handle;
                HwndSource.FromHwnd(handle).AddHook(new HwndSourceHook(WndProc));

                FnPlayProc();

                // Win 7, 8.0, 8.1, 10 일때만 실행 : 64bit에서 player.exe(32bit)로 실행안됨
                if (Environment.OSVersion.Version.Major >= 6)
                    FnProcessExec("REAgentC", "/disable");

                // ThreadingTimer 실행 : 1분
                System.Threading.Timer ThreadLiveCheckTimer = new System.Threading.Timer(FnThreadTimeCheck);
                ThreadLiveCheckTimer.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            }
            catch (Exception)
            {
                FnSystemReboot("Start Window_Loaded Error");
            }
        }
        #endregion

        #region WndProc
        IntPtr WndProc(IntPtr hWnd, int nMsg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (nMsg)
            {
                case 0x345:
                    if (!wParam.ToInt32().Equals(0x99999)) break;
                    if (lParam.ToInt32().Equals(0x88888)) _Receive_Time = DateTime.Now;
                    else if (lParam.ToInt32().Equals(0x77777)) FnPlayProc();
                    break;
                default:
                    break;
            }
            return IntPtr.Zero;
        }
        #endregion

        #region Play Check
        private void FnThreadTimeCheck(Object state)
        {
            try
            {
                DateTime now = DateTime.Now;
                TimeSpan ts = now.Subtract(_Receive_Time);
                if (ts.TotalSeconds > 60) FnSystemReboot("Player Request Error");
            }
            catch (Exception)
            {

            }
        }

        private void FnPlayProc()
        {
            try
            {
                _Receive_Time = DateTime.Now;

                FnProcessKill("Player");
                Thread.Sleep(TimeSpan.FromSeconds(1));

                string strUpdatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update.exe");
                string strStartPath = File.Exists(strUpdatePath) ? strUpdatePath : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Player.exe");

                ProcessStartInfo psi = new ProcessStartInfo(strStartPath);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                Process p = Process.Start(psi);
                p.Close();
            }
            catch (Exception)
            {

            }
        }
        #endregion

        #region 기타함수
        private void FnSystemReboot(string strMsg)
        {
            FnStatusWrite("System Reboot : " + strMsg);
            FnProcessKill("Player");
            FnProcessExec("shutdown", "/r /f /t 0");    // Reboot
        }

        public void FnProcessKill(string pName)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(pName);
                foreach (Process process in processes)
                {
                    process.Kill();
                }
            }
            catch (Exception)
            {

            }
        }

        private void FnProcessExec(string strExe, string strOption)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(strExe, strOption);
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.CreateNoWindow = true;
                Process p = Process.Start(psi);
                p.WaitForExit();
                p.Close();
            }
            catch (Exception)
            {

            }
        }

        public void FnStatusWrite(string strText)
        {
            try
            {
                string strLogFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "status", DateTime.Today.ToString("yyyyMMdd") + "_status.txt");
                using (StreamWriter SW = new StreamWriter(strLogFileName, true))
                {
                    SW.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " | " + strText);
                    SW.Close();
                }
            }
            catch (Exception)
            {

            }
        }
        #endregion
    }
}
