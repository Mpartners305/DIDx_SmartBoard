using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Resources;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Update
{
    public partial class _update : Window
    {
        DispatcherTimer Timer_Update = new DispatcherTimer();

        public _update()
        {
            InitializeComponent();

            Timer_Update.Tick += new EventHandler(Fn_Timer_Update_Tick);
            Timer_Update.Interval = TimeSpan.FromSeconds(1);
            Timer_Update.Start();
        }

        void Fn_Timer_Update_Tick(object sender, EventArgs e)
        {
            Timer_Update.Stop();
            FnStart();
        }

        private void FnStart()
        {
            try
            {
                int nLastStartChangedVersion = 104; // 1.0.4 이전버전 리부팅
                int nVersion = FnString2Int(FnGetReg("Version").Replace(".", ""));
                if (nVersion < nLastStartChangedVersion)
                {
                    FnProcessKill("Start");
                    FnProcessKill("Player");
                    Thread.Sleep(TimeSpan.FromSeconds(1));

                    // 파일 밀어내기
                    FnFileOutput(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Start.exe"), true);
                    FnFileOutput(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Player.exe"), true);

                    FnProcessExec("shutdown", "/r /f /t 0");    // Reboot
                }
                else
                {
                    FnProcessKill("Player");
                    Thread.Sleep(TimeSpan.FromSeconds(1));

                    FnFileOutput(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Player.exe"), true);

                    // Player.exe 실행
                    string strStartPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Player.exe");
                    ProcessStartInfo psi = new ProcessStartInfo(strStartPath);
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    Process p = Process.Start(psi);
                    p.Close();
                }
                Environment.Exit(0);
            }
            catch (Exception)
            {
                FnProcessExec("shutdown", "/r /f /t 0");    // Reboot
            }
        }

        private void FnFileOutput(string strFullName, bool isForceUpdate)
        {
            try
            {
                if (File.Exists(strFullName) && !isForceUpdate) return;
                if (File.Exists(strFullName))
                {
                    try
                    {
                        File.Delete(strFullName);
                    }
                    catch (Exception)
                    {

                    }
                }
                Uri uri = new Uri(@"pack://application:,,,/Files/" + Path.GetFileName(strFullName));
                StreamResourceInfo info = Application.GetResourceStream(uri);
                UnmanagedMemoryStream st = (UnmanagedMemoryStream)info.Stream;
                long length = st.Length;
                byte[] data = new byte[length];
                st.Read(data, 0, (int)length);
                FileStream fs = new FileStream(strFullName, FileMode.Create);
                fs.Write(data, 0, (int)length);
                fs.Flush();
                fs.Close();
            }
            catch (Exception)
            {

            }
        }

        private void FnProcessKill(string pName)
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

        // 레지스트리 가져오기
        public string FnGetReg(string strValue)
        {
            try
            {
                RegistryKey rk;
                if (IntPtr.Size.Equals(8))  // 64bit
                    rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\DidMatePlayer", true);
                else    // 32bit
                    rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\DidMatePlayer", true);
                return rk.GetValue(strValue, string.Empty).ToString();
            }
            catch (Exception)
            {
                return string.Empty;
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

        // 문자("123") 를 숫자로 123, 숫자형 아니면 0
        public int FnString2Int(string s)
        {
            int i = 0;
            bool result = int.TryParse(s, out i);
            return i;
        }
    }
}
