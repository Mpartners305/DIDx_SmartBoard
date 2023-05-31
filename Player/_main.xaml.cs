using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Xml;

namespace Player
{
    public partial class _main : Window
    {
        // ==============================
        private string _Version = "1.0.6";
        // ==============================

        #region 사용자 선언
        private int _Port = 60007;
        public int _Primary = 0, _Secondary = 1;
        private bool _SystemReboot = false;
        private bool _PlaylistXmlChanged = false;
        private DateTime _Today;
        private bool _Display = true;
        private string _WeekValue = string.Empty;
        private string _ClientID = string.Empty, _ProductName = string.Empty;
        public string _UploadURL = string.Empty;
        private IPAddress _ServerIP = null;
        private string _PlaylistXmlIdx = string.Empty;
        private string _PlaylistTempOld = string.Empty, _PlaylistTempNew = string.Empty;
        public string _CurrentPlayScheduleMode = string.Empty;
        public int _CurrentPlayScheduleNo = -2;
        private string _MacAddress = string.Empty;
        public IntPtr _MainHandle = IntPtr.Zero;
        private int _StartCheckCount = 0;
        private bool _PlaylistDelegateUsing = false;

        AESCipher AESCp = new AESCipher("666f7870726f322e6e6174652e636f6d", "666f7870726f322e6e6174652e636f6d");
        public XmlDocument _PlaylistXmlDoc = new XmlDocument();

        private List<StructSchedule> _ListStructSchedule = new List<StructSchedule>();
        public StructLive _StructLive = new StructLive();
        public StructDspConfig _StructDspConfig = new StructDspConfig();
        private StructDownload _StructDownload = new StructDownload();

        private List<string> _ListContents = new List<string>();
        private List<DateTime> _ListHoliday = new List<DateTime>();
        private List<StructPowerTime> _ListDisplay = new List<StructPowerTime>();

        public List<string> _ListMovie = new List<string>(new string[] { ".wmv", ".avi", ".mp4", ".mpg", ".mov", ".flv", ".m2v", ".ts", ".tp", ".mkv", ".vob" });
        public List<string> _ListImage = new List<string>(new string[] { ".jpg", ".png" });
        public List<string> _ListMusic = new List<string>(new string[] { ".mp3", ".wma" });
        public List<string> _ListFlash = new List<string>(new string[] { ".swf" });
        public List<string> _ListPpt = new List<string>(new string[] { ".ppt", ".pps", ".pptx", ".ppsx" });
        public List<string> _ListExe = new List<string>(new string[] { ".exe" });
        List<string> _ListKioskExt = new List<string>(new string[] { ".exe", ".swf", ".ppt", ".pptx" });
        public List<string> _ListDtv = new List<string>(new string[] { ".dtv" });

        public List<string> _ListWeb = new List<string>(new string[] { ".htm", ".html" });  // 2021 06 24 추가됨 by John
        public List<string> _ListCctv = new List<string>(new string[] { ".cctv" });   // 2021 08 09 추가됨 by John
        
        TcpClient tcpClient = new TcpClient();
        System.Threading.Timer DownPercentTimer;
        UserActivityHook actHook;
        _play _Play;
        _dual _Dual;

        delegate void DelegateTcpErrorAsync(string strMsg);
        delegate void DelegateLogLive(string strLayer);
        delegate void DelegatePlaylist();

        ConcurrentQueue<StructQueueMode> _Queue = new ConcurrentQueue<StructQueueMode>();
        System.Threading.Timer SocketConnectTimer;
        #endregion

        #region 생성자 or 초기화
        public _main()
        {
            InitializeComponent();

            FnWinInit();
            FnKeyXmlCheck();
        }

        private void FnWinInit()
        {
            System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;
            if (screens.Length >= 2)
            {
                if (screens[1].Primary)
                {
                    _Primary = 1;
                    _Secondary = 0;
                }
            }
            this.Left = screens[_Primary].Bounds.Left;
            this.Top = screens[_Primary].Bounds.Top;
            this.Width = screens[_Primary].Bounds.Width;
            this.Height = screens[_Primary].Bounds.Height;
        }
        
        private void FnKeyXmlCheck()
        {
            string strClientID, strUploadURL, strProductName;
            string strKeyXmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "key.xml");
            if (File.Exists(strKeyXmlPath))
            {
                // 레지스트리 삭제
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software", true))
                {
                    key.DeleteSubKey("DidMatePlayer", false);
                }
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(strKeyXmlPath);
                strClientID = xDoc.SelectSingleNode("//ClientID").InnerText;
                strUploadURL = xDoc.SelectSingleNode("//UploadURL").InnerText;
                strProductName = FnXmlNodeCheck(xDoc.SelectSingleNode("//ProductName"));
                xDoc = null;

                FnSetReg("ClientID", strClientID);
                FnSetReg("UploadURL", strUploadURL);
                FnSetReg("ProductName", strProductName);

                FnDeleteFile(strKeyXmlPath);
            }
            else
            {
                strClientID = FnGetReg("ClientID");
                strUploadURL = FnGetReg("UploadURL");
                strProductName = FnGetReg("ProductName");
            }

            _ClientID = AESCp.Decrypt(strClientID);
            _UploadURL = AESCp.Decrypt(strUploadURL);
            _ProductName = AESCp.Decrypt(strProductName);

            if (string.IsNullOrEmpty(_ClientID) || string.IsNullOrEmpty(_UploadURL) || string.IsNullOrEmpty(_ProductName))
            {
                FnProcessKill("Start");// Start.exe Exit
                System.Windows.MessageBox.Show("소프트웨어 Key 가 올바르지 않습니다.", "소프트웨어 Key 확인", MessageBoxButton.OK, MessageBoxImage.Error);
                FnExit();
            }
        }
        #endregion

        #region 시작
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // ============================================================================================
                actHook = new UserActivityHook();
                actHook.KeyDown += new System.Windows.Forms.KeyEventHandler(actHook_KeyDown);
                actHook.OnMouseActivity += new System.Windows.Forms.MouseEventHandler(actHook_OnMouseActivity);
                // ============================================================================================

                FnMainInit();
                FnFolderAndFileCheck();
                FnRegistryInit();
                if (_SystemReboot) { FnSystemReboot("Registry Changed"); return; }

                FnCodeInit();
                FnThreadStart();
            }
            catch (Exception) { FnSystemReboot("Player Window_Loaded Error"); }
        }

        private void FnMainInit()
        {
            // Update.exe 처리
            FnProcessKill("Update");
            FnDeleteFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update.exe"));

            _MainHandle = new WindowInteropHelper(this).Handle;

            // 마우스커서 숨김
            Win32API.ShowCursor(false);

            // 시작버튼 Hidden
            if (Environment.OSVersion.Version.Major > 5)
                Win32API.ShowWindow(Win32API.FindWindow("Button", null), 0);

            // Taskbar Hidden
            Win32API.ShowWindow(Win32API.FindWindow("Shell_TrayWnd", null), 0);
        }

        private void FnFolderAndFileCheck()
        {
            FnDirectoryCheck(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents"));
            FnDirectoryCheck(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log"));
            FnDirectoryCheck(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo"));
            FnDirectoryCheck(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kiosk"));
            FnDirectoryCheck(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "status"));
            FnDirectoryCheck(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp"));
        }

        private void FnCodeInit()
        {
            _Today = DateTime.Today;
            _PlaylistXmlIdx = FnGetReg("PlaylistXmlIdx");
            FnSetReg("Version", _Version);

            // playlist.xml Load
            string strPlaylistXmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "playlist.xml");
            if (File.Exists(strPlaylistXmlPath))
            {
                _PlaylistXmlDoc.Load(strPlaylistXmlPath);

                foreach (XmlNode xNode in _PlaylistXmlDoc.SelectNodes("//playlist/schedule/layout/file"))
                {
                    _ListContents.Add(Path.GetFileName(xNode.InnerText.ToLower()));
                }

                FnPlaylistXmlReload();
            }

            //---------------------------------------------------------------------------------------------    
            
           
            
            //--------------------------------------------------------------------------------------------- 
        }

        private void FnThreadStart()
        {
            FnQueueAdd(eQueueMode.StatusWrite, "Player 시작");

            // Task : Queue
            Task.Factory.StartNew(() => { FnSendQueueStart(); });
            // Task : Tcp Socket
            Task.Factory.StartNew(() => { FnSocketStart(); });
            // Task : Schedule
            Task.Factory.StartNew(() => { FnScheduleMain(); });

            //// Download Percent
            DownPercentTimer = new System.Threading.Timer(FnDownPercent);

            // Socket Connect Timer : 1분마다
            SocketConnectTimer = new System.Threading.Timer(FnLiveThreadProc);
            SocketConnectTimer.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            // Start.exe Check Timer : 10초마다, Log and status Upload
            System.Threading.Timer StartCheckTimer = new System.Threading.Timer(FnStartThreadProc);
            StartCheckTimer.Change(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            // Task : NTP Time 설정
            //Task.Factory.StartNew(() => { FnNtpTimeSet(); });  // 2017.10. 18 by John
        }
        #endregion

        #region SystemThreading Timer : 10초마다, Log Upload
        private void FnStartThreadProc(Object state)
        {
            FnStartWinSend(0x88888);
            if (_StartCheckCount % 360 == 0)
            {
                FnLogUpload();      // Log Upload
                FnStatusUpload();   // Status Upload
            }
            _StartCheckCount++;
        }

        private void FnStartWinSend(int lParam)
        {
            try
            {
                IntPtr hnd = Win32API.FindWindow(null, "DidStartWindow");
                if (hnd.Equals(IntPtr.Zero)) throw new Exception();
                Win32API.SendMessage(hnd, 0x345, 0x99999, lParam);
            }
            catch (Exception)
            {
                FnQueueAdd(eQueueMode.StatusWrite, "Thread Timer Error : FnStartWinSend()");
            }
        }
        #endregion

        #region Socket Live Thread Timer : 소켓반응 없을때 1분마다
        private void FnLiveThreadProc(Object state)
        {
            try
            {
                if (string.IsNullOrEmpty(_MacAddress)) return;
                // liveall * MacAddress ? Version ? display ? package ? thumb_img_A ? thumb_img_B ? per_A ? per_B ? download ? error
                string strInitMsg = string.Format(@"{0}*{1}?{2}?{3}?{4}?{5}?{6}?{7}?{8}?{9}?{10}",
                        "liveall", _MacAddress, _Version, Convert.ToInt32(_Display), _StructLive.package,
                        _StructLive.thumb_img_A, _StructLive.thumb_img_B, _StructLive.per_A, _StructLive.per_B, string.Empty, string.Empty); 
                FnQueueAdd(eQueueMode.TcpSend, strInitMsg);
            }
            catch (Exception)
            {
                FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnLiveThreadProc()");
            }
        }
        #endregion

        #region Task : Tcp Send Queue
        private void FnSendQueueStart()
        {
            StructQueueMode _StructQueueMode;
            while (true)
            {
                if (_Queue.IsEmpty)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
                else
                {
                    _Queue.TryDequeue(out _StructQueueMode);
                    FnQueueProc(_StructQueueMode);
                }
            }
        }

        private void FnQueueProc(StructQueueMode _StructQueueMode)
        {
            switch (_StructQueueMode.Mode)
            {
                case eQueueMode.TcpSend:
                    FnTcpLiveSendServer(_StructQueueMode.Text);
                    break;

                case eQueueMode.LogWriteA:
                    FnLogWrite("A", _StructQueueMode.Text);
                    break;

                case eQueueMode.LogWriteB:
                    FnLogWrite("B", _StructQueueMode.Text);
                    break;

                case eQueueMode.StatusWrite:
                    FnStatusWrite(_StructQueueMode.Text);
                    break;

                default:
                    break;
            }
        }

        public void FnQueueAdd(eQueueMode Mode, string Text)
        {
            StructQueueMode _StructQueueMode = new StructQueueMode();
            _StructQueueMode.Mode = Mode;
            _StructQueueMode.Text = Text;
            _Queue.Enqueue(_StructQueueMode);
        }

        private void FnTcpLiveSendServer(string strMsg)
        {
            try
            {
                // Live Timer Restart
                SocketConnectTimer.Change(Timeout.Infinite, Timeout.Infinite);
                SocketConnectTimer.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

                // Live Socket Send
                byte[] byteSendMsg = Encoding.UTF8.GetBytes(_ClientID + "*" + strMsg + "|");
                tcpClient.Client.Send(byteSendMsg);
            }
            catch (Exception)
            {
                FnTcpClientClose();
            }
        }
        #endregion

        #region Task : Tcp Socket
        private void FnSocketStart()
        {
            string strRcvMsg = string.Empty;
            byte[] bytes = new byte[1024];
            int nRec;
            int nErrorCount = 0;

            while (true)
            {
                try
                {
                    // Server IP 가져오기
                    if (_ServerIP == null) _ServerIP = FnGetServerIP();
                    if (_ServerIP == null) throw new Exception();

                    // Socket Connect
                    tcpClient = new TcpClient();
                    tcpClient.Connect(_ServerIP, _Port);

                    // 서버와 연결성공후 MacAddress 가져오기
                    if (string.IsNullOrEmpty(_MacAddress)) _MacAddress = FnMacAddress();
                    if (string.IsNullOrEmpty(_MacAddress)) throw new Exception();

                    // Init * MacAddress ? Version ? display ? package ? thumb_img_A ? thumb_img_B ? per_A ? per_B ? download ? error
                    string strInitMsg = string.Format(@"{0}*{1}?{2}?{3}?{4}?{5}?{6}?{7}?{8}?{9}?{10}",
                            "init", _MacAddress, _Version, Convert.ToInt32(_Display), _StructLive.package,
                            _StructLive.thumb_img_A, _StructLive.thumb_img_B, _StructLive.per_A, _StructLive.per_B, string.Empty, string.Empty);
                    FnQueueAdd(eQueueMode.TcpSend, strInitMsg);

                    while (true)
                    {
                        nRec = tcpClient.Client.Receive(bytes);
                        if (nRec.Equals(0))
                            throw new Exception();
                        strRcvMsg = Encoding.UTF8.GetString(bytes, 0, nRec);

                        // Socket Validate Error 처리 (3번연속 에러일때)
                        if (strRcvMsg.Equals("PidError|"))
                        {
                            nErrorCount++;
                            if (nErrorCount > 3)
                            {
                                FnExit();
                                return;
                            }
                            throw new Exception();
                        }
                        if (nErrorCount > 0) nErrorCount = 0;

                        foreach (string strMsg in strRcvMsg.Split('|'))
                        {
                            if (string.IsNullOrEmpty(strMsg)) continue;
                            FnTcpSocketProcess(strMsg);
                        }
                    }
                }
                catch (Exception)
                {
                    FnTcpClientClose();
                }
                finally
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }

        private void FnTcpSocketProcess(string strRcvMsg)
        {
            try
            {
                string[] arrMsg = strRcvMsg.Split('*');
                string[] arrTmp;

                switch (arrMsg[0].ToLower())
                {
                    case "svr_init":
                        arrTmp = arrMsg[1].Split('?');

                        // Update
                        if (!_Version.Equals(arrTmp[0]) && !string.IsNullOrEmpty(arrTmp[0]))
                        {
                            FnMainUpdateProc(arrTmp[0]);
                        }

                        // City
                        if (!string.IsNullOrEmpty(arrTmp[2])) FnMainCityProc(arrTmp[2]);

                        // Logo
                        if (!string.IsNullOrEmpty(arrTmp[3])) FnMainLogoProc(arrTmp[3]);

                        // Playlist.xml
                        if (!_PlaylistXmlIdx.Equals(arrTmp[1]) && !string.IsNullOrEmpty(arrTmp[1]))
                        {
                            _PlaylistTempNew = arrTmp[1];

                            DelegatePlaylist delegatePlaylist = new DelegatePlaylist(FnPlaylistStart);
                            delegatePlaylist.BeginInvoke(new AsyncCallback(FnPlaylistComplete), null);
                        }
                        break;

                    case "svr_update":
                        if (!_Version.Equals(arrMsg[1]) && !string.IsNullOrEmpty(arrMsg[1]))
                            FnMainUpdateProc(arrMsg[1]);
                        break;

                    case "svr_playlist":
                        if (_PlaylistXmlIdx.Equals(arrMsg[1]) || string.IsNullOrEmpty(arrMsg[1]))
                            break;
                        _PlaylistTempNew = arrMsg[1];
                        if (!_PlaylistDelegateUsing)
                        {
                            DelegatePlaylist delegatePlaylist = new DelegatePlaylist(FnPlaylistStart);
                            delegatePlaylist.BeginInvoke(new AsyncCallback(FnPlaylistComplete), null);
                        }
                        break;

                    case "svr_citylogo":
                        arrTmp = arrMsg[1].Split('?');
                        FnMainCityProc(arrTmp[0]);
                        FnMainLogoProc(arrTmp[1]);
                        break;

                    case "svr_reboot":
                        FnSystemReboot("Socket Request");
                        break;

                    case "svr_replay":
                        FnSoftwareRestart();
                        break;

                    default:
                        break;
                }
            }
            catch (Exception)
            {
                _StructLive.error = "Socket 처리 실패";
                FnQueueAdd(eQueueMode.TcpSend, string.Format("{0}*{1}", "error", _StructLive.error));
                FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnTcpSocketProcess()");
            }
        }

        private void FnMainUpdateProc(string strVersion)
        {
            try
            {
                string strServerPath = string.Format("{0}/firmware/{1}/update_{2}.exe", _UploadURL, _ProductName, strVersion);
                string strLocalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update.exe");
                string strTempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "update_" + strVersion + ".exe");

                int i = 0;
                while (i < 10)
                {
                    FnDeleteFile(strLocalPath);
                    FnDeleteFile(strTempPath);
                    if (FnDownload(strServerPath, strTempPath))
                    {
                        File.Move(strTempPath, strLocalPath);
                        FnSoftwareRestart();
                        return;
                    }
                    i++;
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
                throw new Exception();
            }
            catch (Exception)
            {
                _StructLive.error = "Update 실패";
                FnQueueAdd(eQueueMode.TcpSend, string.Format("{0}*{1}", "error", _StructLive.error));
                FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnMainUpdateProc()");
            }
        }

        private void FnMainCityProc(string strCity)
        {
            try
            {
                if (FnGetReg("City").Equals(strCity)) return;
                FnSetReg("City", strCity);
                //Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { try { _Play.FnTemplateWeather(); } catch (Exception) { } }));
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { try { _Play.FnWeatherProc(); } catch (Exception) { } }));
            }
            catch (Exception)
            {

            }
        }

        private void FnMainLogoProc(string strLogo)
        {
            try
            {
                if (FnGetReg("Logo").Equals(strLogo) || string.IsNullOrEmpty(strLogo)) return;
                string strServerPath = string.Format("{0}/logo/{1}", _UploadURL, strLogo);
                string strLocalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo", strLogo);

                if (FnDownload(strServerPath, strLocalPath))
                {
                    FnSetReg("Logo", strLogo);
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { try { _Play.FnTemplateLogo(); } catch (Exception) { } }));
                }
            }
            catch (Exception)
            {

            }
        }
        #endregion

        #region Playlist
        private void FnPlaylistXmlReload()
        {
            try
            {
                if (_PlaylistXmlDoc == null) return;
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlayClose(); }));

                // WeekValue
                _WeekValue = FnXmlNodeCheck(_PlaylistXmlDoc.SelectSingleNode("//dspconfig/week_value"));

                // Holiday
                _ListHoliday.Clear();
                string strHolidayValue = FnXmlNodeCheck(_PlaylistXmlDoc.SelectSingleNode("//dspconfig/holiday_value"));
                foreach (string strValue in strHolidayValue.Split(','))
                {
                    if (string.IsNullOrEmpty(strValue)) continue;
                    _ListHoliday.Add(Convert.ToDateTime(strValue));
                }

                // Display On Off Time
                _ListDisplay.Clear();
                string strTimeValue = FnXmlNodeCheck(_PlaylistXmlDoc.SelectSingleNode("//dspconfig/time_value"));
                foreach (string strValue in strTimeValue.Split(','))
                {
                    if (string.IsNullOrEmpty(strValue)) continue;
                    string[] arrTime = strValue.Split('~');
                    StructPowerTime structPowerTime = new StructPowerTime();
                    structPowerTime.s_time = arrTime[0];
                    structPowerTime.e_time = arrTime[1];
                    _ListDisplay.Add(structPowerTime);
                }

                // dspconfig
                _StructDspConfig.duration = FnXmlNodeCheck(_PlaylistXmlDoc.SelectSingleNode("//dspconfig/duration"), "10");
                _StructDspConfig.transition = FnXmlNodeCheck(_PlaylistXmlDoc.SelectSingleNode("//dspconfig/transition"), "random");
                _StructDspConfig.ticker_speed = FnXmlNodeCheck(_PlaylistXmlDoc.SelectSingleNode("//dspconfig/ticker_speed"));
                _StructDspConfig.ticker_location = FnXmlNodeCheck(_PlaylistXmlDoc.SelectSingleNode("//dspconfig/ticker_location"));
                _StructDspConfig.ticker_font_color = FnXmlNodeCheck(_PlaylistXmlDoc.SelectSingleNode("//dspconfig/ticker_font_color"));
                _StructDspConfig.ticker_bg_color = FnXmlNodeCheck(_PlaylistXmlDoc.SelectSingleNode("//dspconfig/ticker_bg_color"));
                _StructDspConfig.ticker_font_size = FnXmlNodeCheck(_PlaylistXmlDoc.SelectSingleNode("//dspconfig/ticker_font_size"));
                _StructDspConfig.ticker_rss_use = FnXmlNodeCheck(_PlaylistXmlDoc.SelectSingleNode("//dspconfig/ticker_rss_use"), "N");
                _StructDspConfig.ticker_rss_url = FnXmlNodeCheck(_PlaylistXmlDoc.SelectSingleNode("//dspconfig/ticker_rss_url"));
                _StructDspConfig.volume = FnXmlNodeCheck(_PlaylistXmlDoc.SelectSingleNode("//dspconfig/volume"), "0");

                // Playlist
                _ListStructSchedule.Clear();
                foreach (XmlNode xNode in _PlaylistXmlDoc.SelectNodes("//playlist/schedule"))
                {
                    StructSchedule structSchedule = new StructSchedule();
                    structSchedule.mode = xNode.Attributes["mode"].InnerText;
                    structSchedule.no = FnString2Int(xNode.Attributes["no"].InnerText);
                    structSchedule.s_date = FnXmlNodeCheck(xNode.SelectSingleNode("config/s_date"));
                    structSchedule.e_date = FnXmlNodeCheck(xNode.SelectSingleNode("config/e_date"));
                    structSchedule.s_time = FnXmlNodeCheck(xNode.SelectSingleNode("config/s_time"));
                    structSchedule.e_time = FnXmlNodeCheck(xNode.SelectSingleNode("config/e_time"));
                    structSchedule.week_value = FnXmlNodeCheck(xNode.SelectSingleNode("config/week_value"));
                    _ListStructSchedule.Add(structSchedule);
                }

                // Schedule Change True
                _PlaylistXmlChanged = true;
            }
            catch (Exception)
            {
                FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnPlaylistXmlReload()");
            }
        }

        private void FnPlaylistStart()
        {
            _PlaylistDelegateUsing = true;
            _PlaylistTempOld = _PlaylistTempNew;

            try
            {
                string strPlaylistXmlUrl = string.Format("{0}/playlist/{1}/{2}/{3}", _UploadURL, _ProductName, _ClientID, "playlist.xml");
                string strLocalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "playlist.xml");

                int nCnt = 0;
                while (nCnt < 5)
                {
                    if (FnDownload(strPlaylistXmlUrl, strLocalPath))
                    {
                        XmlDocument xDoc = new XmlDocument();
                        xDoc.Load(strLocalPath);

                        // 다운로드할 컨텐츠 갯수 구하기
                        XmlNodeList xNodeList;
                        xNodeList = xDoc.SelectNodes("//playlist/schedule/layout/file");
                        int nDownCount = 0;
                        foreach (XmlNode oItem in xNodeList)
                        {
                            if (!_ListContents.Contains(Path.GetFileName(FnXmlNodeCheck(oItem)))) nDownCount++;
                        }
                        xNodeList = xDoc.SelectNodes("//playlist/schedule/config/kiosk_name");
                        foreach (XmlNode oItem in xNodeList)
                        {
                            if (!_ListKioskExt.Contains(Path.GetExtension(FnXmlNodeCheck(oItem)))) continue;
                            if (!_ListContents.Contains(Path.GetFileName(FnXmlNodeCheck(oItem)))) nDownCount++;
                        }
                        _StructDownload.total_count = nDownCount;
                        _StructDownload.current_no = 0;
                        _StructDownload.down_complete = true;

                        List<string> list = new List<string>();
                        // Contents Download
                        foreach (XmlNode oItem in xDoc.SelectNodes("//playlist/schedule/layout/file"))
                        {
                            _StructDownload.down_mode = eContentsDownMode.Contents;
                            _StructDownload.file_path = string.Format("{0}/contents/{1}/{2}", _UploadURL, _ProductName, oItem.InnerText);
                            _StructDownload.file_name = Path.GetFileName(_StructDownload.file_path);
                            _StructDownload.file_size = FnString2Long(FnXmlNodeCheck(oItem.Attributes["size"]));
                            if (_StructDownload.file_size > 0) list.Add(FnContentsDownload());
                        }

                        // Kiosk Contents Download
                        foreach (XmlNode oItem in xDoc.SelectNodes("//playlist/schedule/config/kiosk_name"))
                        {
                            if (!_ListKioskExt.Contains(Path.GetExtension(FnXmlNodeCheck(oItem)))) continue;
                            _StructDownload.down_mode = eContentsDownMode.Kiosk;
                            _StructDownload.file_path = string.Format("{0}/contents/{1}/{2}", _UploadURL, _ProductName, oItem.InnerText);
                            _StructDownload.file_name = Path.GetFileName(_StructDownload.file_path);
                            _StructDownload.file_size = FnString2Long(FnXmlNodeCheck(oItem.Attributes["size"]));
                            if (_StructDownload.file_size > 0) list.Add(FnContentsDownload());
                        }

                        // Download Complete
                        if (_StructDownload.down_complete)
                        {
                            if (!string.IsNullOrEmpty(_StructLive.error))
                            {
                                _StructLive.error = string.Empty;
                                FnQueueAdd(eQueueMode.TcpSend, string.Format("{0}*{1}", "error", _StructLive.error));
                            }
                            FnQueueAdd(eQueueMode.TcpSend, string.Format("{0}*{1}", "download", string.Empty));

                            _PlaylistXmlDoc = xDoc;     // Xml Change
                            File.Copy(strLocalPath, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "playlist.xml"), true);   // Xml Copy
                            _PlaylistXmlIdx = FnSetReg("PlaylistXmlIdx", _PlaylistTempOld);
                            FnPlaylistXmlReload();     // Schedule Restart
                            FnContentsDelete(list, eContentsDownMode.Contents, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents"));    // Contents Delete
                            FnContentsDelete(list, eContentsDownMode.Kiosk, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kiosk"));    // Kiosk Delete
                            _ListContents = list;       // 다음 컨텐츠 다운 갯수구하기 위해
                            FnQueueAdd(eQueueMode.StatusWrite, "Playlist.xml 변경");
                            return;                            
                        }
                    }
                    nCnt++;
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
                throw new Exception();
            }
            catch (Exception)
            {
                _StructLive.error = "컨텐츠 다운로드 실패";
                FnQueueAdd(eQueueMode.TcpSend, string.Format("{0}*{1}", "error", _StructLive.error));
                FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnPlaylistStart()");
            }
        }

        private void FnPlaylistComplete(IAsyncResult ar)
        {
            if (!_PlaylistTempOld.Equals(_PlaylistTempNew))
            {
                DelegatePlaylist delegatePlaylist = new DelegatePlaylist(FnPlaylistStart);
                delegatePlaylist.BeginInvoke(new AsyncCallback(FnPlaylistComplete), null);
            }
            _PlaylistDelegateUsing = false;
        }

        private string FnContentsDownload()
        {
            try
            {
                string strLocalSavePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, (_StructDownload.down_mode.Equals(eContentsDownMode.Contents)) ? "contents" : "kiosk", _StructDownload.file_name);
                FileInfo fileInfo = new FileInfo(strLocalSavePath);
                if (fileInfo.Exists) return _StructDownload.file_name;

                string strLocalTempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", _StructDownload.file_name);
                FnDeleteFile(strLocalTempPath);
                _StructDownload.current_no++;
                DownPercentTimer.Change(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

                if (FnDownload(_StructDownload.file_path, strLocalTempPath))
                {
                    File.Move(strLocalTempPath, strLocalSavePath);
                    FnQueueAdd(eQueueMode.StatusWrite, string.Format("File Download : {0}", _StructDownload.file_name));    // File Download Status
                    if (_ListMovie.Contains(Path.GetExtension(strLocalSavePath)))   // 동영상 일때 Live 100%
                    {
                        string strMsg = string.Format("{0}*{1}<br>({2}%, {3}/{4})"
                            , "download", _StructDownload.file_name, "100", _StructDownload.current_no, _StructDownload.total_count);
                        FnQueueAdd(eQueueMode.TcpSend, strMsg);
                    }
                }
                else
                    throw new Exception();

                DownPercentTimer.Change(Timeout.Infinite, Timeout.Infinite);
                return _StructDownload.file_name.ToLower();
            }
            catch (Exception)
            {
                DownPercentTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _StructDownload.down_complete = false;
                return string.Empty;
            }
        }

        private void FnDownPercent(Object state)
        {
            try
            {
                string strLocalTempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", _StructDownload.file_name);
                FileInfo fileInfo = new FileInfo(strLocalTempPath);
                int nPercent = (fileInfo.Exists) ? Convert.ToInt32((float)fileInfo.Length / _StructDownload.file_size * 100) : 0;
                string strMsg = string.Format("{0}*{1}<br>({2}%, {3}/{4})", "download", _StructDownload.file_name, nPercent, _StructDownload.current_no, _StructDownload.total_count);
                FnQueueAdd(eQueueMode.TcpSend, strMsg);
            }
            catch (Exception)
            {

            }
        }

        private void FnContentsDelete(List<string> list, eContentsDownMode eMode, string strDirPath)
        {
            DirectoryInfo dir = new DirectoryInfo(strDirPath);
            foreach (FileInfo f in dir.GetFiles())
            {
                if (eMode.Equals(eContentsDownMode.Kiosk) && !_ListKioskExt.Contains(f.Extension.ToLower())) continue;
                if (list.Contains(f.Name.ToLower())) continue;
                if (FnDeleteFile(f.FullName)) FnQueueAdd(eQueueMode.StatusWrite, string.Format("File Delete : {0}", f.Name));    // File Delete Status
            }
        }
        #endregion

        #region Schedule
        private void FnScheduleMain()
        {
            while (true)
            {
                FnSchedulePrecess();
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        private void FnSchedulePrecess()
        {
            try
            {
                if (_ListStructSchedule.Count.Equals(0))
                {
                    
                    //FnQueueAdd(eQueueMode.StatusWrite, "Display Off by Schedule Count"); //for test by John
                     
                    FnDisplayOff(false);
                    return;
                }

                // 요일 체크
                if (!string.IsNullOrEmpty(_WeekValue) && !_WeekValue.Contains(((int)DateTime.Today.DayOfWeek).ToString()))
                {

                    //FnQueueAdd(eQueueMode.StatusWrite, "Display Off by Week Value"); //for test by John

                    FnDisplayOff(false);
                    return;
                }

                // 공휴일이면 체크
                for (int i = 0; i < _ListHoliday.Count; i++)
                {
                    if (_ListHoliday[i].Equals(DateTime.Today))
                    {
                        //FnQueueAdd(eQueueMode.StatusWrite, "Display Off by Holiday"); //for test by John

                        FnDisplayOff(false);
                        return;
                    }
                }

                // Display On Off Time 체크
                if (_ListDisplay.Count > 0)
                {
                    bool isOnOff = false;
                    for (int i = 0; i < _ListDisplay.Count; i++)
                    {
                        DateTime dt_s_time = Convert.ToDateTime(_ListDisplay[i].s_time);
                        DateTime dt_e_time = Convert.ToDateTime(_ListDisplay[i].e_time);
                        if (dt_s_time > dt_e_time)
                        {
                            if (DateTime.Now.Hour < dt_e_time.Hour)         // 현재 < e_time
                                dt_s_time = dt_s_time.AddDays(-1);
                            else if (DateTime.Now.Hour > dt_s_time.Hour)    // 현재 > s_time
                                dt_e_time = dt_e_time.AddDays(1);
                        }
                        if (DateTime.Now >= dt_s_time && DateTime.Now <= dt_e_time) isOnOff = true;
                    }
                    if (!isOnOff)
                    {
                        //FnQueueAdd(eQueueMode.StatusWrite, "Display Off by On/Off Time"); //for test by John

                        FnDisplayOff(false);
                        return;
                    }
                }

                // 스케쥴처리 (이벤트부터 처리)
                for (int i = 0; i < _ListStructSchedule.Count; i++)
                {
                    if (_ListStructSchedule[i].mode.Equals("A"))        // 기본 스케쥴
                    { }
                    else if (_ListStructSchedule[i].mode.Equals("B"))   // 요일/시간별 스케쥴
                    {
                        // 요일 체크
                        if (!_ListStructSchedule[i].week_value.Contains(((int)DateTime.Today.DayOfWeek).ToString())) continue;
                        // 시간체크
                        DateTime dt_s_time = Convert.ToDateTime(_ListStructSchedule[i].s_time);
                        DateTime dt_e_time = Convert.ToDateTime(_ListStructSchedule[i].e_time);
                        if (dt_s_time > dt_e_time)
                        {
                            if (DateTime.Now.Hour < dt_e_time.Hour)         // 현재 < e_time
                                dt_s_time = dt_s_time.AddDays(-1);
                            else if (DateTime.Now.Hour > dt_s_time.Hour)    // 현재 > s_time
                                dt_e_time = dt_e_time.AddDays(1);
                        }
                        if (DateTime.Now < dt_s_time || DateTime.Now > dt_e_time) continue;
                    }
                    else if (_ListStructSchedule[i].mode.Equals("C"))   // 날짜별 스케쥴
                    {
                        DateTime dt_s_date = Convert.ToDateTime(_ListStructSchedule[i].s_date);
                        DateTime dt_e_date = Convert.ToDateTime(_ListStructSchedule[i].e_date);
                        if (DateTime.Today < dt_s_date || DateTime.Today > dt_e_date) continue;
                    }

                    if (FnPlayCheck(_ListStructSchedule[i].no, _ListStructSchedule[i].mode))
                    {
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlayShow(); }));
                    }
                    return;
                }

                //FnQueueAdd(eQueueMode.StatusWrite, "Display Off by Schedule Event"); //for test by John

                FnDisplayOff(false);    // Display OFF
            }
            catch (Exception)
            {

            }
        }

        private bool FnPlayCheck(int nNo, string strMode)
        {
            if (_PlaylistXmlChanged || !_CurrentPlayScheduleNo.Equals(nNo))
            {
                // 날짜 변경이고 Display Off 일때
                if (!_Today.Equals(DateTime.Today) && _CurrentPlayScheduleNo.Equals(-1))
                {
                    FnSystemReboot("DateTime Changed");
                    return false;
                }
                else
                {
                    _PlaylistXmlChanged = false;
                    _CurrentPlayScheduleNo = nNo;
                    _CurrentPlayScheduleMode = strMode;
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region 플레이어 관련
        private void FnPlayShow()
        {
            try
            {
                FnPlayClose();
                FnDisplayOn();

                _Play = new _play(this);
                _Play.Owner = this;
                _Play.Show();

                // playlist.xml Road
                if (System.Windows.Forms.Screen.AllScreens.Length > 1)
                {
                    string strXmlFormat = string.Format("//playlist/schedule[@mode='{0}'][@no='{1}']/config/package", _CurrentPlayScheduleMode, _CurrentPlayScheduleNo);
                    string strPackage = FnXmlNodeCheck(_PlaylistXmlDoc.SelectSingleNode(strXmlFormat));
                    if (strPackage.Equals("b2"))
                    {
                        _Dual = new _dual(this);
                        _Dual.Owner = this;
                        _Dual.Show();
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        public void FnPlayClose()
        {
            FnMonitorOnOff(false);
            try
            {
                foreach (Window win in OwnedWindows)
                {
                    win.Close();
                }
            }
            catch (Exception)
            {

            }
        }

        public void FnDisplayOn()
        {
            if (_Display) return;
            _Display = true;
            FnQueueAdd(eQueueMode.TcpSend, string.Format("{0}*{1}", "display", Convert.ToInt32(_Display)));
        }

        public void FnDisplayOff(bool isForce)
        {
            if (!_Display) return;

            _Display = false;
            if (!isForce) _CurrentPlayScheduleNo = -1; // 강제 Off가 아닐때 -1
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlayClose(); }));
            FnQueueAdd(eQueueMode.TcpSend, string.Format("{0}*{1}", "display", Convert.ToInt32(_Display)));
        }

        public void FnMonitorOnOff(bool _bool)
        {
            /*
            // 2: off, -1: on
            int nValue = (_bool) ? -1 : 2;
            Win32API.SendMessage(_MainHandle, Win32API.WM_SYSCOMMAND, Win32API.SC_MONITORPOWER, nValue);
            */

            // appended by John at 2017. 11. 22
            RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

            if (rk.GetValue("CurrentMajorVersionNumber", string.Empty).ToString().Equals("10")) return; // for Windows 10, Following code is not avaliable 

            // 2 : off,   -1 : on,   1 : low
            int nValue = (_bool) ? -1 : 2;  // false : ON, true : OFF
            Win32API.SendMessage(_MainHandle, Win32API.WM_SYSCOMMAND, Win32API.SC_MONITORPOWER, nValue);
        }
        #endregion

        #region 시스템 함수
        void FnExit()
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlayClose(); }));
            FnMonitorOnOff(true);
            FnTcpClientClose();
            FnProcessKill("Start");
            Win32API.ShowWindow(Win32API.FindWindow("Shell_TrayWnd", null), 1);
            Environment.Exit(0);
        }

        public void FnSystemReboot(string strMsg)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlayClose(); }));
            FnMonitorOnOff(true);
            FnTcpClientClose();
            FnStatusWrite("System Reboot : " + strMsg);
            FnProcessKill("Start");
            FnProcessExec("shutdown", "/r /f /t 0");
        }

        private void FnSoftwareRestart()
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlayClose(); }));
            FnTcpClientClose();
            FnStatusWrite("Software Reboot");
            FnStartWinSend(0x77777);
            Environment.Exit(0);
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
        #endregion

        #region Download, Upload
        public Boolean FnDownload(string server_path, string local_path)
        {
            try
            {
                MyWebClient mwc = new MyWebClient();
                mwc.DownloadFile(server_path, local_path);
                return true;
            }
            catch
            {
                FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnDownload()");
                return false;
            }
        }

        private Boolean FnUpload(string local_path, string server_path)
        {
            try
            {
                MyWebClient mwc = new MyWebClient();
                mwc.Credentials = new NetworkCredential("bcldkim", "bcld0101");
                mwc.UploadFile(new Uri(server_path), "PUT", local_path);
                return true;
            }
            catch
            {
                FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnUpload()");
                return false;
            }
        }
        #endregion

        #region Live & Log
        public void FnPlayerLiveLog(string strLayer)
        {
            string strTxt = string.Empty;
            string strFile = string.Empty;
            eQueueMode eMode = eQueueMode.LogWriteA;
            switch (strLayer)
            {
                case "A":
                    strTxt = string.Format("{0}*{1}?{2}?{3}", strLayer, _StructLive.package, _StructLive.thumb_img_A, _StructLive.per_A);
                    strFile = _StructLive.file_name_A;
                    eMode = eQueueMode.LogWriteA;
                    break;

                case "B":
                    strTxt = string.Format("{0}*{1}?{2}?{3}", strLayer, _StructLive.package, _StructLive.thumb_img_B, _StructLive.per_B);
                    strFile = _StructLive.file_name_B;
                    eMode = eQueueMode.LogWriteB;
                    break;

                default:
                    break;
            }
            FnQueueAdd(eQueueMode.TcpSend, strTxt);
            FnQueueAdd(eMode, strFile);
        }

        private void FnLogWrite(string strLayer, string strValue)
        {
            try
            {
                string strLogFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log", DateTime.Today.ToString("yyyyMMdd") + ".txt");
                using (StreamWriter SW = new StreamWriter(strLogFileName, true))
                {
                    SW.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " | " + strLayer + " | " + strValue);
                    SW.Close();
                }
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

        private void FnLogUpload()
        {
            try
            {
                string strServerPath = string.Empty;
                string strLogFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                DirectoryInfo dir = new DirectoryInfo(strLogFolder);

                foreach (FileInfo f in dir.GetFiles("*.txt"))
                {
                    strServerPath = string.Format("{0}/playlist/{1}/{2}/log/{3}", _UploadURL, _ProductName, _ClientID, f.Name);
                    if (FnUpload(f.FullName, strServerPath))
                    {
                        if (Path.GetFileNameWithoutExtension(f.Name).Equals(DateTime.Today.ToString("yyyyMMdd"))) continue;
                        FnDeleteFile(f.FullName);
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private void FnStatusUpload()
        {
            try
            {
                string strServerPath = string.Empty;
                string strLogFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "status");
                DirectoryInfo dir = new DirectoryInfo(strLogFolder);

                foreach (FileInfo f in dir.GetFiles("*.txt"))
                {
                    strServerPath = string.Format("{0}/playlist/{1}/{2}/log/{3}", _UploadURL, _ProductName, _ClientID, f.Name);
                    if (FnUpload(f.FullName, strServerPath))
                    {
                        if (Path.GetFileNameWithoutExtension(f.Name).Equals(DateTime.Today.ToString("yyyyMMdd") + "_status")) continue;
                        FnDeleteFile(f.FullName);
                    }
                }
            }
            catch (Exception)
            {

            }
        }
        #endregion

        #region 레지스트리
        private void FnRegistryInit()
        {
            // Win XP (5.1)
            if (Environment.OSVersion.Version.Major < 6)
            {
                // 모티터전원 : 사용안함
                FnRegistryMonitorPowerOff_XP();

                // 오류보고 사용안함
                FnRegistryErrorReport_XP();
            }
            // Win 7(6.1),  Win 8(6.2), Win 8.1(6.3), Win 10(10)
            else
            {
                // UxSms Off
                FnRegistryUxSmsOff_Win7();

                // UAC Off
                FnRegistryUACOff_Win7();

                // 모티터전원 : 사용안함 --> Monitor timeout 및 절전모드 설정
                FnRegistryMonitorPowerOff_Win7();
            }

            // Screen Saver 사용안함
            FnRegistryScreenSaver();

            // 레지스트리 - 팝업 방지
            FnRegistryPopup();

            // USB 자동 실행 방지
            FnNoDriveTypeAutoRun();

            // 자동업데이트 중지
            FnRegistryAutoUpdateDisable();

            // 자동로그인
            FnRegistryAutoLogon();

            //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            // "System.Windows.Forms.WebBrowser" default browser is changed IE7(default) to IE11 for HTML contents
            // This code is appended at 2021. 12. 24 by John
            // HtML 컨텐츠를 표출하는 System.Windows.Forms.WebBrowser 의 default brower 를 IE7(default) 에서 IE11 로 변경
            FnRegistryIE7toIE11();
        }

        // 레지스트리 가져오기
        public string FnGetReg(string strValue)
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\DidMatePlayer", true);
                return rk.GetValue(strValue, string.Empty).ToString();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        // 레지스트리 쓰기
        public string FnSetReg(string strValue, string strData)
        {
            string _value = string.Empty;
            RegistryKey rk = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\DidMatePlayer", RegistryKeyPermissionCheck.ReadWriteSubTree);
            _value = rk.GetValue(strValue, string.Empty).ToString();
            if (_value.Equals(strData)) return _value;
            rk.SetValue(strValue, strData, RegistryValueKind.String);
            _value = rk.GetValue(strValue, string.Empty).ToString();
            rk.Close();
            return _value;
        }

        private void FnNoDriveTypeAutoRun()
        {
            RegistryKey rk = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (rk.GetValue("NoDriveTypeAutoRun", string.Empty).ToString().Equals("221")) return;
            rk.SetValue("NoDriveTypeAutoRun", 221, RegistryValueKind.DWord);
            rk.Close();
        }

        void FnRegistryPopup()
        {
            RegistryKey rk = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (rk.GetValue("EnableBalloonTips", string.Empty).ToString().Equals("0")) return;
            rk.SetValue("EnableBalloonTips", 0, RegistryValueKind.DWord);
            rk.Close();
        }

        private void FnRegistryErrorReport_XP()
        {
            RegistryKey rk = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\PCHealth\ErrorReporting", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (rk.GetValue("DoReport", string.Empty).ToString().Equals("0")) return;
            rk.SetValue("DoReport", 0, RegistryValueKind.DWord);
            rk.SetValue("ShowUI", 0, RegistryValueKind.DWord);
            rk.Close();
        }

        private void FnRegistryScreenSaver()
        {
            RegistryKey rk = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (rk.GetValue("ScreenSaveActive", string.Empty).ToString().Equals("0")) return;
            rk.SetValue("ScreenSaveActive", 0, RegistryValueKind.String);
            rk.DeleteValue("SCRNSAVE.EXE", false);
            rk.Close();
        }

        private void FnRegistryMonitorPowerOff_XP()
        {
            RegistryKey rk = Registry.CurrentUser.CreateSubKey(@"Control Panel\PowerCfg", RegistryKeyPermissionCheck.ReadWriteSubTree);
            string strCurrentPowerPolicy = rk.GetValue("CurrentPowerPolicy", string.Empty).ToString();

            if (!strCurrentPowerPolicy.Equals("2"))
            {
                FnProcessExec("POWERCFG", "/SETACTIVE 프레젠테이션");
            }
            rk.Close();
        }

        private void FnRegistryMonitorPowerOff_Win7()
        {
            // if windows screen is displayed in black screen after few milli second, check below code alternatively.
            // for Windows 7, 8.0, 8.1, 10
            /*
            FnProcessExec("POWERCFG", "-x -monitor-timeout-ac 0");
            FnProcessExec("POWERCFG", "-x -standby-timeout-ac 0");
            FnProcessExec("POWERCFG", "-x -disk-timeout-ac 0");
            FnProcessExec("POWERCFG", "-x -hibernate-timeout-ac 0");
            FnProcessExec("POWERCFG", "-h off");
            FnProcessExec("POWERCFG", "-SETACVALUEINDEX SCHEME_BALANCED SUB_BUTTONS PBUTTONACTION 3");
            */

            // for Windows 10 by John 2017. 11. 16 // it needs to check more detail
            
            FnProcessExec("POWERCFG", "/x monitor-timeout-ac 0");
            FnProcessExec("POWERCFG", "/x standby-timeout-ac 0");
            FnProcessExec("POWERCFG", "/x disk-timeout-ac 0");
            FnProcessExec("POWERCFG", "/x hibernate-timeout-ac 0");
            FnProcessExec("POWERCFG", "/h off");
            FnProcessExec("POWERCFG", "/SETACVALUEINDEX SCHEME_BALANCED SUB_BUTTONS PBUTTONACTION 3");                         
        }

        private void FnRegistryUxSmsOff_Win7()
        {
            RegistryKey rk = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\UxSms", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (rk.GetValue("Start", string.Empty).ToString().Equals("4")) return;
            rk.SetValue("Start", 4, RegistryValueKind.DWord);
            rk.Close();

            // 서비스 중지
            FnProcessExec("sc", "stop UxSms");

            _SystemReboot = true;
        }

        private void FnRegistryUACOff_Win7()
        {
            RegistryKey rk = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (rk.GetValue("EnableLUA", string.Empty).ToString().Equals("0")) return;
            rk.SetValue("ConsentPromptBehaviorAdmin", 5, RegistryValueKind.DWord);
            rk.SetValue("EnableLUA", 0, RegistryValueKind.DWord);
            rk.SetValue("PromptOnSecureDesktop", 1, RegistryValueKind.DWord);
            rk.Close();
            _SystemReboot = true;
        }

        private void FnRegistryAutoUpdateDisable()
        {
            RegistryKey rk = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (rk.GetValue("AUOptions", string.Empty).ToString().Equals("1")) return;
            rk.SetValue("AUOptions", 1, RegistryValueKind.DWord);
            rk.Close();
        }

        private void FnRegistryAutoLogon()
        {
            RegistryKey rk = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (rk.GetValue("AutoAdminLogon", string.Empty).ToString().Equals("1")) return;
            rk.SetValue("AutoAdminLogon", 1, RegistryValueKind.String);
            rk.Close();
        }

        private bool FnRegistryFlashPlayerCheck()
        {
            RegistryKey rk = Registry.ClassesRoot.OpenSubKey(@"CLSID\{D27CDB6E-AE6D-11cf-96B8-444553540000}");
            return (rk == null) ? false : true;
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // "System.Windows.Forms.WebBrowser" default browser is changed IE7(default) to IE11 for HTML contents
        // This code is appended at 2021. 12. 24 by John
        private void FnRegistryIE7toIE11()
        {
            string filename = Process.GetCurrentProcess().MainModule.FileName;
            filename = filename.Substring(filename.LastIndexOf('\\') + 1, filename.Length - filename.LastIndexOf('\\') - 1);
            if (filename.Contains("vhost"))
                filename = filename.Substring(0, filename.IndexOf('.') + 1) + "exe";

            if (Environment.Is64BitOperatingSystem) // for 32bit program on 64bit O/S
            {
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true).SetValue(filename, 11001, RegistryValueKind.DWord);
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BEHAVIORS", true).SetValue(filename, 11001, RegistryValueKind.DWord);
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true).SetValue(filename, 11001, RegistryValueKind.DWord);
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BEHAVIORS", true).SetValue(filename, 11001, RegistryValueKind.DWord);
            }
            else // for 32bit program on 32bit O/S,  for 64bit program on 64bit O/S
            {
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true).SetValue(filename, 11001, RegistryValueKind.DWord);
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BEHAVIORS", true).SetValue(filename, 11001, RegistryValueKind.DWord);
            }
        }

        // "System.Windows.Forms.WebBrowser" default browser is changed IE11 to IE7(default) for HTML contents
        // This code is appended at 2021. 12. 24 by John
        private void FnRegistryIE11toIE7()
        {
            string filename = Process.GetCurrentProcess().MainModule.FileName;
            filename = filename.Substring(filename.LastIndexOf('\\') + 1, filename.Length - filename.LastIndexOf('\\') - 1);
            if (filename.Contains("vhost"))
                filename = filename.Substring(0, filename.IndexOf('.') + 1) + "exe";

            try
            {
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true).DeleteValue(filename);
            }
            catch (Exception)
            {

            }

            try
            {
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BEHAVIORS", true).DeleteValue(filename);
            }
            catch (Exception)
            {

            }

            try
            {
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true).DeleteValue(filename);
            }
            catch (Exception)
            {

            }

            try
            {
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BEHAVIORS", true).DeleteValue(filename);
            }
            catch (Exception)
            {

            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        

        #endregion

        #region 이벤트
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FnExit();
        }

        void actHook_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            switch (e.KeyData)
            {
                case Keys.Escape:
                    FnExit();
                    break;

                default:
                    break;
            }
        }

        void actHook_OnMouseActivity(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            try
            {
                if (e.Clicks < 1) return;

                if (!_Display)
                    FnMonitorOnOff(false);
                //Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnMonitorOnOff(false); }));

                if (_Play._StructKiosk.use && _Play._StructKiosk.dblTime > 0)
                {
                    if (_Play._StructKiosk.play)
                        _Play.FnKioskTimerStopStart();
                    else
                        _Play.FnKioskReplay();
                }
            }
            catch (Exception)
            {

            }
        }
        #endregion
              
        #region 기타함수
        private void FnNtpTimeSet()
        {
            FnProcessExec("net", "start w32time");
            FnProcessExec("w32tm", "/config /syncfromflags:manual /manualpeerlist:\"time.kriss.re.kr time-a.nist.gov\" /update");                                                        
            FnProcessExec("w32tm", "/resync");
            //time.kriss.re.kr : 한국표준과학연구원, time-a.nist.gov : ? 확인필요함
        }

        private IPAddress FnGetServerIP()
        {
            IPAddress ip = null;
            try
            {
                IPAddress[] ipAddress = Dns.GetHostAddresses(_UploadURL.ToLower().Replace("http://", ""));
                foreach (IPAddress addr in ipAddress)
                {
                    if (addr.AddressFamily.Equals(AddressFamily.InterNetwork))
                    {
                        ip = addr;
                    }
                }
            }
            catch (Exception) { ip = null; }
            return ip;
        }

        private void FnTcpClientClose()
        {
            try
            {
                if (tcpClient != null)
                {
                    tcpClient.Close();
                    tcpClient = null;
                } 
            }
            catch (Exception)
            {

            }
        }

        public string FnGetCityName()
        {
            string strCity = FnGetReg("City");
            if (string.IsNullOrEmpty(strCity)) strCity = "서울특별시";
            return strCity;
        }

        public int FnString2Int(string s)
        {
            int i = 0;
            bool result = int.TryParse(s, out i);
            return i;
        }

        public long FnString2Long(string s)
        {
            long i = 0;
            bool result = long.TryParse(s, out i);
            return i;
        }

        public string FnXmlNodeCheck(XmlNode xNode)
        {
            return (xNode == null) ? string.Empty : xNode.InnerText;
        }

        public string FnXmlNodeCheck(XmlNode xNode, string strDefaultValue)
        {
            string strValue = FnXmlNodeCheck(xNode);
            return (string.IsNullOrEmpty(strValue)) ? strDefaultValue : strValue;
        }

        private void FnDirectoryCheck(string strPath)
        {
            if (!Directory.Exists(strPath)) Directory.CreateDirectory(strPath);
        }

        public void FnPlayerErrorProc(string strMsg)
        {
            //FnQueueAdd(eQueueMode.StatusWrite, string.Format("{0}*{1}", "Display Off by", strMsg)); //for test by John 2016 03 14

            FnDisplayOff(true);
            _StructLive.error = strMsg;
            FnQueueAdd(eQueueMode.TcpSend, string.Format("{0}*{1}", "error", _StructLive.error));
        }

        private bool FnDeleteFile(string strPath)
        {
            bool _bool = false;
            if (File.Exists(strPath))
            {
                try
                {
                    File.Delete(strPath);
                    _bool = true;
                }
                catch (Exception) { }
            }
            return _bool;
        }

        private String FnMacAddress()
        {
            string strMacAddress = String.Empty;

            ObjectQuery objQuery = new ObjectQuery("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled='True'");
            ManagementObjectSearcher mobjSearcher = new ManagementObjectSearcher(objQuery);

            try
            {
                foreach (ManagementObject obj in mobjSearcher.Get())
                {
                    strMacAddress = obj["MACAddress"].ToString().Replace(":", "");
                    break;
                }
            }
            catch (Exception)
            {
                strMacAddress = String.Empty;
            }

            return strMacAddress;
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

        public string FnGetProgramFilesX86FolderName()
        {
            //return (IntPtr.Size.Equals(8)) ? Environment.GetEnvironmentVariable("ProgramFiles(x86)") : Environment.GetEnvironmentVariable("ProgramFiles");
            //return (System.Environment.Is64BitOperatingSystem) ? @"C:\Program Files (x86)" : @"C:\Program Files";
            return (System.Environment.Is64BitOperatingSystem) ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        }
        #endregion
    }

    #region 구조체
    public struct StructContents
    {
        public string file_name { get; set; }
        public int c_idx { get; set; }
    }

    public struct StructDspConfig
    {
        public string duration { get; set; }
        public string transition { get; set; }
        public string ticker_speed { get; set; }
        public string ticker_location { get; set; }
        public string ticker_font_color { get; set; }
        public string ticker_bg_color { get; set; }
        public string ticker_font_size { get; set; }
        public string ticker_rss_use { get; set; }
        public string ticker_rss_url { get; set; }
        public string volume { get; set; }
    }

    public struct StructSchedule
    {
        public string mode { get; set; }
        public int no { get; set; }
        public string s_date { get; set; }
        public string e_date { get; set; }
        public string s_time { get; set; }
        public string e_time { get; set; }
        public string week_value { get; set; }
    }

    public struct StructConfig
    {
        public string bgm { get; set; }
        public string ticker_use { get; set; }
        public string ticker_msg { get; set; }
        public string package { get; set; }
        public int perA { get; set; }
        public string kiosk_use { get; set; }
        public string kiosk_layout { get; set; }
        public string kiosk_name { get; set; }
        public string kiosk_time { get; set; }
    }

    public struct StructLive
    {
        public string package { get; set; }
        public string file_name_A { get; set; }
        public string file_name_B { get; set; }
        public string thumb_img_A { get; set; }
        public string thumb_img_B { get; set; }
        public int per_A { get; set; }
        public int per_B { get; set; }
        public string error { get; set; }
    }

    public struct StructKiosk
    {
        public bool use { get; set; }
        public bool play { get; set; }
        public bool first_execute { get; set; }
        public ePlayMode mode { get; set; }
        public string file_name { get; set; }
        public string layout { get; set; }
        public double dblTime { get; set; }
        public IntPtr handle { get; set; }
    }

    public struct StructDownload
    {
        public eContentsDownMode down_mode { get; set; }
        public string file_path { get; set; }
        public string file_name { get; set; }
        public long file_size { get; set; }
        public int total_count { get; set; }
        public int current_no { get; set; }
        public bool down_complete { get; set; }
    }

    public struct StructPowerTime
    {
        public string s_time { get; set; }
        public string e_time { get; set; }
    }

    public struct StructQueueMode
    {
        public eQueueMode Mode { get; set; }
        public string Text { get; set; }
    }

    public enum eQueueMode
    {
        TcpSend,
        LogWriteA,
        LogWriteB,
        StatusWrite
    }

    public enum eContentsDownMode
    {
        Contents,
        Kiosk
    }

    public enum ePlayMode
    {
        Movie,
        Music,
        Image,
        Flash,
        Powerpoint,
        Dtv,
        Html,
        Exe,
        Cctv,
        NULL
    }

    enum eKioskPlayStatus
    {
        Play,
        Pause,
        Stop
    }
    #endregion

    #region class MyWebClient : WebClient
    public class MyWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(System.Uri address)
        {
            WebRequest WR = base.GetWebRequest(address);
            WR.Timeout = 1000;
            return WR;
        }
    }
    #endregion

    #region Win32API
    public class Win32API
    {
        public const int WM_USER = 0x0400;
        public const int WM_CLOSE = 0x0010;
        public const int UM_REMOCON_RECEIVE = WM_USER + 99;
        public const int BM_CLICK = 0x00F5;
        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MONITORPOWER = 0xF170;

        [DllImport("user32.dll")]
        public static extern void ShowWindow(IntPtr hwnd, int cmd);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string cls, string wndwText);

        [DllImport("user32.dll")]
        public static extern int ShowCursor(bool bShow);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int hMsg, int wParam, int lParam);
       
        [DllImport("user32.dll")]
        public static extern int PostMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string className, string windowName);

        [DllImport("User32.dll")]
        public static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
        
        public const short SWP_NOMOVE = 0X2;
        public const short SWP_NOSIZE = 1;
        public const short SWP_NOZORDER = 0X4;
        public const int SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll")]
        public static extern void BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern void SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("User32")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndParent);

        [DllImport("kernel32.dll")]
        public static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        public const uint LBUTTONDOWN = 0x00000002; // 왼쪽 마우스 버튼 눌림
        public const uint LBUTTONUP = 0x00000004; // 왼쪽 마우스 버튼 떼어짐

        //mouse_event(LBUTTONDOWN, 0, 0, 0, 0);
        //mouse_event(LBUTTONUP, 0, 0, 0, 0);
    }
    #endregion
}
