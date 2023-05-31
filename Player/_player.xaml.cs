using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xaml;
using System.Xml;
// for JSON data format
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
// for Microsoft Edge browser
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace Player
{
    public partial class _play : System.Windows.Window
    {
        #region 변수 선언

        Random random = new Random();
        DispatcherTimer Timer_imageA = new DispatcherTimer();
        DispatcherTimer Timer_imageB = new DispatcherTimer();

        //DispatcherTimer Timer_FlashA = new DispatcherTimer();
        //DispatcherTimer Timer_FlashB = new DispatcherTimer();

        DispatcherTimer Timer_Kiosk = new DispatcherTimer();

        DispatcherTimer Timer_PPT = new DispatcherTimer();

        DispatcherTimer Timer_WEBA = new DispatcherTimer();
        DispatcherTimer Timer_WEBB = new DispatcherTimer();     

        //-------------------------------------------------------------------------------------------
        double _ScreenHeightRatio = 0, _ScreenWidthRatio = 0;
        double _DoubleTickerHeight = 0, _DoubleTickerFontSize = 0;
        string _PptPlayLayout = string.Empty;
        string _DtvPlayLayout = string.Empty;
        bool _IsFirstImage_A = true;
        bool _WindowsClosed = false;
        bool _TickerUse = false;

        //-------------------------------------------------------------------------------------------
        Grid playGrid_A, playGrid_B;      
        Grid movieGrid_A, movieGrid_B;        
        //Grid flashGrid_A, flashGrid_B;
        Grid kioskGrid;
        Grid WEBGrid_A, WEBGrid_B;
        Grid WEBGrid_E;

        //-------------------------------------------------------------------------------------------
        private Image imagePlayer_A = null; // image A
        private Image imagePlayer_B = null; // image A
        private MediaElement moviePlayer_A = new MediaElement();    // movie A
        private MediaElement moviePlayer_B = new MediaElement();    // movie B
        private MediaPlayer _BGMplayer = new MediaPlayer(); // Back Ground Music (BGM)

        //---------2017. 11.7 by John-------------------------------------------
        bool _OnExecution_BGM = false;
        public ePlayMode _Priv_File_Type = ePlayMode.NULL;

        //----------------------------------------------------------------------
        //AxShockwaveFlashObjects.AxShockwaveFlash flashPlayer_A = new AxShockwaveFlashObjects.AxShockwaveFlash();
        //AxShockwaveFlashObjects.AxShockwaveFlash flashPlayer_B = new AxShockwaveFlashObjects.AxShockwaveFlash();
        //AxShockwaveFlashObjects.AxShockwaveFlash flashPlayer_Kiosk = new AxShockwaveFlashObjects.AxShockwaveFlash();
        //AxShockwaveFlashObjects.AxShockwaveFlash flashPlayer_Weather = new AxShockwaveFlashObjects.AxShockwaveFlash();
        //AxShockwaveFlashObjects.AxShockwaveFlash flashPlayer_Clock = new AxShockwaveFlashObjects.AxShockwaveFlash();
        //WindowsFormsHost host_flashplayer_A = new WindowsFormsHost();
        //WindowsFormsHost host_flashplayer_B = new WindowsFormsHost();

        //-------------------------------------------------------------------------------------------
        // HTML contents for back compatible
        System.Windows.Forms.WebBrowser WEBBrowser_Weather = new System.Windows.Forms.WebBrowser();
        System.Windows.Forms.WebBrowser WEBBrowser_Clock = new System.Windows.Forms.WebBrowser();
        WindowsFormsHost host_WEBplayer_Weather = new WindowsFormsHost();
        WindowsFormsHost host_WEBplayer_Clock = new WindowsFormsHost();

        // HTML contents for A, B area schedules using IE7/IE11
        System.Windows.Forms.WebBrowser WEBBrowser_A = new System.Windows.Forms.WebBrowser();
        System.Windows.Forms.WebBrowser WEBBrowser_B = new System.Windows.Forms.WebBrowser();
        WindowsFormsHost host_WEBplayer_A = new WindowsFormsHost();
        WindowsFormsHost host_WEBplayer_B = new WindowsFormsHost();

        // HTML contents for A, B area schedules using Microsoft_Edge WebView2 API
        Microsoft.Web.WebView2.Wpf.WebView2 webView2Browser_A = new Microsoft.Web.WebView2.Wpf.WebView2();
        Microsoft.Web.WebView2.Wpf.WebView2 webView2Browser_B = new Microsoft.Web.WebView2.Wpf.WebView2();

        //-------------------------------------------------------------------------------------------
        // HTML contents for A, B area schedules for chrome API  
        ContentControl HtmlBrowser_A_Container = new ContentControl
        {
            HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch
            //Foreground = System.Windows.Media.Brushes.Black
        };
        //ContentControl HtmlBrowser_A_2_Container;        

        ContentControl HtmlBrowser_B_Container = new ContentControl
        {
            HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch
            //Foreground = System.Windows.Media.Brushes.Black
        };
        //ContentControl HtmlBrowser_B_2_Container;     


        int _HtmlDispTime = 0;

        // HTML contents for Single Line News
        System.Windows.Forms.WebBrowser WEBBrowser_E = new System.Windows.Forms.WebBrowser();
        WindowsFormsHost host_WEBplayer_E = new WindowsFormsHost();

        //-------------------------------------------------------------------------------------------
        System.Threading.Timer TempleteThreadingTimer = null;
        //System.Threading.Timer KimsProjectRSSContentsTimer = null;
        //System.Threading.Timer KimsProjectStoryTimer = null;

        //-------------------------------------------------------------------------------------------        
        List<StructContents> _ListStructContents_A = new List<StructContents>();
        List<StructContents> _ListStructContents_B = new List<StructContents>();
        List<string> _ListStructBGM = new List<string>();
        StructConfig _StructConfig = new StructConfig();
        public StructKiosk _StructKiosk = new StructKiosk();

        IntPtr _PlayerHandle = IntPtr.Zero;
        int _LayoutCount = 0, _CurrentBgmNo = 0;
        int _CurrentPlayNo_A = 0, _CurrentPlayNo_B = 0;
        ePlayMode _CurrentPlayMode_A = ePlayMode.NULL, _CurrentPlayMode_B = ePlayMode.NULL;
        int _CyclePlayCount_A = 0, _CyclePlayCount_B = 0, _CyclePlayCount_BGM = 0;
        int _PlayLeft_A = 0, _PlayTop_A = 0, _PlayWidth_A = 0, _PlayHeight_A = 0;
        int _PlayLeft_B = 0, _PlayTop_B = 0, _PlayWidth_B = 0, _PlayHeight_B = 0;
        int _nTempleteCount = 0;

        //-------------------------------------------------------------------------------------------   
        // for Fade In/OutTicker
        Storyboard storyboard;
        Storyboard fade_storyboard;

        List<string> listTickerText = new List<string>();
        List<string> tempTickerText = new List<string>();
        int listcounter = 0;
                
        //***********************************************************************************************************************************************************
        string[] WeatherAreas = new string[19] { "서울특별시", "인천광역시", "경기도", "충청북도", "대전광역시", "충청남도", "세종특별자치시", "강원도 영서",
                                                 "강원도 영동", "전라북도", "광주광역시", "전라남도", "대구광역시", "경상북도", "부산광역시", "울산광역시", 
                                                 "경상남도", "제주특별자치도 제주", "제주특별자치도 서귀포" };

        string[] MpartnersWeatherAreaCode = new string[19] {"11B10101", "11B20201", "11B20601", "11C10301", "11C20401", "11C20104", "11C20404", "11D10301",
                                                            "11D20501", "11F10201", "11F20501", "21F20801", "11H10701", "11H10501", "11H20201", "11H20101",
                                                            "11H20301", "11G00201", "11G00401" };
                                                            // Mpartners 서버, 19개 시도

        string[] MpartnersLifeAreaCode = new string[19] {"11B00000", "11A00000", "11B00000", "11C10000", "11C20000", "11C20000", "11C20000", "11D10000",
                                                         "11D20000", "11F10000", "11F20000", "11F20000", "11H10000", "11H10000", "11H20000", "11H20000",
                                                         "11H20000", "11G00000", "11G00000" };
                                                          // Mpartners 서버, 19개 시도, 생활지수   // 빠진 지역코드가 있음 확인 할것

        string[] KmaWeatherAreaCode = new string[21] {"1114052000", "2820053000", "4111566000", "4311154500", "3017063000", "4480031000", "3611025000", "4211053000",
                                                   "4215051000", "4511171100", "2914074500", "4684025600", "2711051700", "2723063100", "2647065000", "3114051000",
                                                   "4812155000", "5011065000", "5013058000", "4794025000", "2872035000" };  
                                                   // 실시간 지역날씨를 위해 울릉도, 서해5도 추가 2016.9.26 

        string[] WeatherCityCode = new string[19] { "서울", "인천", "수원", "청주", "대전", "서산", "세종", "원주",
                                                    "강릉", "전주", "광주", "목포", "대구", "대구", "부산", "울산", 
                                                    "창원", "제주", "서귀포" };
        
        string[] WeatherKorText = new string[7] {"맑음", "구름 조금", "구름 많음", "흐림", "비", "눈/비", "눈"};
        string[] WeatherFlashCode = new string[7] { "w01", "w02", "w03", "w04", "w11", "w41", "w21" };

        string[] WeatherCityText = new string[12] { "맑음", "구름조금", "구름많음", "구름많고 비", "구름많고 비/눈", "구름많고 눈/비", "구름많고 눈",
                                                   "흐림", "흐리고 비", "흐리고 비/눈", "흐리고 눈/비", "흐리고 눈" };

        string[] WeatherCityFlashCode = new string[12] { "w01", "w02", "w03", "w11", "w41", "w41", "w21", "w04", "w11", "w41", "w41", "w21" };

        string[] WeatheriCurrentMent = new string[23] { "  ", "맑음", "구름조금", "흐림", "비", "눈", "눈비", "소나기", "소낙눈", "안개", "뇌우", "차차 흐려짐", "흐려져 뇌우",
                                                        "흐려져 비", "흐려져 눈", "흐려져 눈비", "흐린 후 갬","뇌우 후 갬", "비 후 갬", "눈 후 갬", "눈비 후 갬", "구름많음", "황사"};

        //int _weather_tomorrow = 0;
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        DateTime CurrentDateTime;

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //int _nD5_Stock_chart_first_execute = 0;  // for NH투자증권 주식 차트 출력

        //Grid chart_NH, chart_KOSPI, chart_KOSDAQ, chart_KOSPI200, chart_SHANG, chart_SPX, chart_WonDollar;
        //System.Windows.Controls.WebBrowser NHwebBrowser, KOSPIwebBrowser, KOSDAQwebBrowser, KOSPI200webBrowser, SHANGwebBrowser, SPXwebBrowser, WonDollarwebBrowser;
        //------------------------------------------------------------------------------------------------------------------------------------------------------------
        
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        // for NH투자증권 1x3 multi 용 RSS 월드 주가 출력
        string[] Stock_nhqv_KoreanCode = new string[25] {"코스피", "코스닥", "코스피200", "선물", "다우", "나스닥", "닛케이", "항셍",
                                                          "회사채", "국고채", "콜금리", "엔/달러", "원/달러", "WTI", "금(달러/온스)", "싱가폴지수",
                                                          "상하이B지수", "상해종합지수", "영국지수", "CD(91일)", "원/엔", "상하이A지수", "홍콩H지수", "EuroStoxx50", 
                                                          "S&P500" };
        string[] Stock_nhqv_EnglishCode = new string[25] {"코스피", "코스닥", "코스피200", "선물", "DOW JONES", "NASDAQ", "NIKKEI 225", "HANG SENG",
                                                          "회사채", "국고채", "콜금리", "엔/달러", "원/달러", "WTI", "금(달러/온스)", "싱가폴지수",
                                                          "상하이B지수", "상해종합지수", "영국지수", "CD(91일)", "원/엔", "상하이A지수", "홍콩H지수", "EuroStoxx50", 
                                                          "S&P500" };  
        //------------------------------------------------------------------------------------------------------------------------------------------------------------        
        #endregion
        
        #region 생성자 & 시작
        public _main _Main;
        public _play(_main win)
        {
            InitializeComponent();
            _Main = win;
            FnFormInit();
        }
        private void FnFormInit()
        {
            System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;
            this.Left = screens[_Main._Primary].Bounds.Left;
            this.Top = screens[_Main._Primary].Bounds.Top;
            this.Width = screens[_Main._Primary].Bounds.Width;
            this.Height = screens[_Main._Primary].Bounds.Height;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FnGlobalHook();
            FnPlayerStart();
        }
        #endregion

        #region WndProc
        private void FnGlobalHook()
        {
            _PlayerHandle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(_PlayerHandle).AddHook(new HwndSourceHook(WndProc));
        }
        IntPtr WndProc(IntPtr hWnd, int nMsg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (nMsg)
            {
                case 0x500: // PPT Viewer 2010
                    if ((wParam.ToInt32() == 0x12345) && (lParam.ToInt32() == 0x12345))
                    {
                        _Main.FnProcessKill("PPTVIEW");

                        switch (_PptPlayLayout)
                        {
                            case "B":
                                _CyclePlayCount_B++;
                                FnPlay_B(++_CurrentPlayNo_B);
                                break;
                            default:
                                _CyclePlayCount_A++;
                                FnPlay_A(++_CurrentPlayNo_A);
                                break;
                        }
                    }
                    break;
                
                default:
                    break;
            }
           
            return IntPtr.Zero;
        }
        #endregion
                        
        // ==========================================================================================
        #region Player Start
        private void FnPlayerStart()
        {
            try
            {
                FnPlayerStop();
                FnCodeInit();
                FnTickerInit();
                FnGridInit();
                FnLayoutStructSet();
                FnPlay();
                if (_TickerUse) FnTickerPlay();               
            }
            catch (Exception) 
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnPlayerErrorProc("Player Error"); 
            }
        }
        #endregion

        #region CodeInit
        private void FnCodeInit()
        {
            try
            {
                _Main.FnMonitorOnOff(true);
                FnPlayerTopMost();
                Win32API.SetCursorPos(Convert.ToInt32(this.Width), 0); // 마우스 위치조정           

                // initialization for each parts
                FnWebVew2_Initialization();  // for Microsoft Edge browser package                

                // timers 초기화
                Timer_imageA.Tick += new EventHandler(Fn_Timer_ImageA_Tick);
                Timer_imageB.Tick += new EventHandler(Fn_Timer_ImageB_Tick);
                //Timer_FlashA.Interval = TimeSpan.FromMilliseconds(100);
                //Timer_FlashA.Tick += new EventHandler(Timer_FlashA_Tick);
                //Timer_FlashB.Interval = TimeSpan.FromMilliseconds(100);
                //Timer_FlashB.Tick += new EventHandler(Timer_FlashB_Tick);
                Timer_Kiosk.Tick += new EventHandler(Fn_Timer_Kiosk_Tick);
                Timer_PPT.Tick += new EventHandler(Fn_Timer_PPT_Tick);

                Timer_WEBA.Tick += new EventHandler(Fn_Timer_WEBA_Tick);
                Timer_WEBB.Tick += new EventHandler(Fn_Timer_WEBB_Tick);

                // playlist.xml Road
                string strXmlFormat = string.Format("//playlist/schedule[@mode='{0}'][@no='{1}']/config", _Main._CurrentPlayScheduleMode, _Main._CurrentPlayScheduleNo);
                XmlNode xNode = _Main._PlaylistXmlDoc.SelectSingleNode(strXmlFormat);
                _StructConfig.bgm = _Main.FnXmlNodeCheck(xNode.SelectSingleNode("bgm"));
                _StructConfig.package = _Main.FnXmlNodeCheck(xNode.SelectSingleNode("package"));
                _StructConfig.perA = _Main.FnString2Int(_Main.FnXmlNodeCheck(xNode.SelectSingleNode("perA")));
                _StructConfig.ticker_use = _Main.FnXmlNodeCheck(xNode.SelectSingleNode("ticker_use"));
                _StructConfig.ticker_msg = _Main.FnXmlNodeCheck(xNode.SelectSingleNode("ticker_msg"));

                string kiosk_use = _Main.FnXmlNodeCheck(xNode.SelectSingleNode("kiosk_use"));
                string kiosk_layout = _Main.FnXmlNodeCheck(xNode.SelectSingleNode("kiosk_layout"));
                string kiosk_name = _Main.FnXmlNodeCheck(xNode.SelectSingleNode("kiosk_name"));
                int kiosk_time = _Main.FnString2Int(_Main.FnXmlNodeCheck(xNode.SelectSingleNode("kiosk_time")));

                // 키오스크 초기화
                _StructKiosk.use = kiosk_use.Equals("Y") ? true : false;
                _StructKiosk.play = false;
                _StructKiosk.first_execute = false;
                _StructKiosk.mode = ePlayMode.NULL;
                _StructKiosk.file_name = kiosk_name;
                _StructKiosk.layout = kiosk_layout;
                _StructKiosk.dblTime = kiosk_time;
                _StructKiosk.handle = IntPtr.Zero;

                // Live Package
                _Main._StructLive.package = _StructConfig.package;
                _Main._StructLive.per_A = _StructConfig.perA;
                _Main._StructLive.per_B = (_Main._StructLive.package.Equals("b0") || _Main._StructLive.package.Equals("b1")) ? 100 - _StructConfig.perA : 0;

                // layout 갯수
                _LayoutCount = FnLayoutCount(_StructConfig.package);

                // 모니터 가로, 세로 사이즈 비율
                switch (_StructConfig.package)
                {
                    case "a0":
                    case "b0":
                    case "b1":
                    case "b2":
                    case "e0":
                    case "e1":
                    case "e2":
                    case "e4":
                        rootGrid.Width = 1920;
                        rootGrid.Height = 1080;
                        break;

                    case "t0":
                        rootGrid.Width = 1920;
                        rootGrid.Height = 1080;
                        break;

                    case "f0":
                    case "f1":
                        rootGrid.Width = 540;
                        rootGrid.Height = 1920;
                        break;

                    default:
                        rootGrid.Width = 1920;
                        rootGrid.Height = 1080;
                        break;

                } //switch (_StructConfig.package)

                _ScreenWidthRatio = this.Width / rootGrid.Width;
                _ScreenHeightRatio = this.Height / rootGrid.Height;

                // Ticker 사용 유무
                if ((_StructConfig.ticker_use.Equals("1") ||
                     _Main._StructDspConfig.ticker_rss_use.Equals("1")) &&
                     !_StructConfig.package.Equals("t0") &&
                     !_StructConfig.package.Equals("f1")) _TickerUse = true;

                // 템플릿 Threading Timer : 10초
                TempleteThreadingTimer = new System.Threading.Timer(FnTempleteTimerMain);
                TempleteThreadingTimer.Change(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10)); // Change(TimeSpan dueTime, TimeSpan period);
                FnTempleteTimerMain();
            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnCodeInit()");
            }
        }  // end of FnCodeInit()    
        
        #endregion

        #region 템플릿 Threading Timer : 10초
        private void FnTempleteTimerMain(Object state)
        {
            FnTempleteTimerMain();
        }

        private void FnTempleteTimerMain()
        {
            try
            {
                string strServer;
                string strLocal;
                WebClient myWebClient;

                //=================================================================
                switch (_StructConfig.package)
                {
                    case "e0":
                    case "e1":
                    case "e2":
                        FnStockProc();  // 주식 : 10초
                        if (_nTempleteCount % 180 == 0)
                            FnWeatherProc(); // 날씨 : 30분마다
                        break;

                    case "e4":
                        if (_nTempleteCount % 180 == 0)
                            FnWeatherProc(); // 날씨 : 30분마다
                        break;

                    case "t0":
                        if (_nTempleteCount % 2 == 0)
                            FnDateTimeProc(); // 날짜, 시간 : 20초마다    
                        if (_nTempleteCount % 120 == 0)
                            FnWeatherProc(); // 날씨 : 20분마다                              
                        break;

                    case "f0":
                        FnStockProc();  // 주식 : 10초
                        if (_nTempleteCount % 2 == 0)
                            FnDateTimeProc(); // 날짜, 시간 : 20초마다
                        if (_nTempleteCount % 120 == 0)
                            FnWeatherProc(); // 날씨 : 20분마다
                        break;

                    case "f1":
                        if (_nTempleteCount % 2 == 0)
                            FnDateTimeProc(); // 날짜, 시간 : 20초마다    
                        if (_nTempleteCount % 120 == 0)
                            FnWeatherProc(); // 날씨 : 20분마다
                        if (_nTempleteCount % 180 == 0)
                            FnFinedustProc(); // 미세먼지 : 30분마다                     
                        break;

                    default:    // a0, b0, b1, b2                    
                        break;

                }  //switch (_StructConfig.package)

                //=================================================================
                // Bottom 부분 Headline news 를 위한 RSS data 
                if (_nTempleteCount % 30 == 0) // RSS : 5분마다  //테스트용
                                               //if (_nTempleteCount % 180 == 0) // RSS : 30분마다
                {
                    switch (_StructConfig.package)
                    {
                        case "t0":
                        case "f1":
                            break;

                        default:   //a0, b0, b1, b2, e0, e1, e2, e4, f0 
                            if (_Main._StructDspConfig.ticker_rss_use.Equals("1"))
                            {
                                
                                // Mpartners 기본 news data
                                if (string.Equals(_Main._StructDspConfig.ticker_rss_url, "default.xml"))
                                {
                                    //************************************************************************
                                    strServer = string.Format("{0}/xml/{1}", _Main._UploadURL, "data_mnews_all.txt");
                                    strLocal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "data_mnews_all.txt");
                                    myWebClient = new WebClient();
                                    myWebClient.DownloadFile(strServer, strLocal);

                                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () { FnTickerFlowPlay(); }));
                                }
                                else  // 고객 지정 RSS 뉴스, 미리 검토되어야 함                  
                                {
                                    strServer = _Main._StructDspConfig.ticker_rss_url;
                                    strLocal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "rss_news_all.xml");

                                    if (FnXmlDown(strServer, strLocal))
                                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () { FnTickerFlowPlay(); }));
                                }
                               
                            }
                            else
                            {
                                
                                //************************************************************************
                                strServer = string.Format("{0}/xml/{1}", _Main._UploadURL, "data_mnews_all.txt");
                                strLocal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "data_mnews_all.txt");
                                myWebClient = new WebClient();
                                myWebClient.DownloadFile(strServer, strLocal);
                                
                            }

                            break;
                    }   //switch (_StructConfig.package)

                }   //if (_nTempleteCount % 180 == 0)

                //=================================================================
                _nTempleteCount++;

            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnTempleteTimerMain()");
            }
        }

        private void FnStockProc()
        {
            string strServer = null;
            string strLocal = null;

            try
            {
                switch (_StructConfig.package)
                {
                    case "f0":  //환율 정보만 표출                       
                        strServer = string.Format("{0}/xml/{1}", _Main._UploadURL, "rss_exchange_mpartners.xml");
                        strLocal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "rss_exchange_mpartners.xml");

                        if (FnXmlDown(strServer, strLocal))
                            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnTemplateStock(); }));
                     
                        break;                 

                    default:                   
                        break;
                }
            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnStockProc()");
            }
        }

        public void FnWeatherProc()
        {
            try
            {
                string strServer;
                string strLocal;

                string strCity = _Main.FnGetCityName();

                //--------------------------------------------------------------------------------------------------
                // 현재 날씨, 전체 지역
                strServer = string.Format("{0}/xml/{1}", _Main._UploadURL, "rss_weatheri_current.xml");
                strLocal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "rss_weatheri_current.xml");
                WebClient myWebClient = new WebClient();
                myWebClient.DownloadFile(strServer, strLocal);                                            

                //--------------------------------------------------------------------------------------------------
                // 날씨 예보, 해당 지역
                strServer = string.Format("{0}/xml/data_mweather_today_{1}.txt", _Main._UploadURL, MpartnersWeatherAreaCode[Array.IndexOf(WeatherAreas, strCity)]);
                strLocal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "data_mweather_today.txt");
                myWebClient = new WebClient();
                myWebClient.DownloadFile(strServer, strLocal);

                //--------------------------------------------------------------------------------------------------
                // 생활지수, 해당 지역
                strServer = string.Format("{0}/xml/data_mweather_life_{1}.txt", _Main._UploadURL, MpartnersLifeAreaCode[Array.IndexOf(WeatherAreas, strCity)]);
                strLocal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "data_mweather_life.txt");
                myWebClient = new WebClient();
                myWebClient.DownloadFile(strServer, strLocal);

                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnTemplateWeather(); }));
            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnWeatherProc()");
            }
        }

        private void FnFinedustProc()
        {
            try
            {
                string strServer;
                string strLocal;

                switch (_StructConfig.package)
                {
                    case "t0":
                    case "f1":
                        strServer = string.Format("{0}/xml/rss_finedust.xml", _Main._UploadURL);
                        strLocal = string.Format("{0}/{1}/rss_finedust.xml", AppDomain.CurrentDomain.BaseDirectory, "temp");
                        
                        if (FnXmlDown(strServer, strLocal) == false)
                            break;  

                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnTemplateFinedust(); }));
                        break;

                    default:
                        break;
                }
            }
            catch(Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnFinedustProc()");
            }
        }

        public void FnDateTimeProc()
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnTemplateDateTime(); }));
        }           
       
        private bool FnXmlDown(string strXmlUrl, string strSavePath)
        {
            try
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(strXmlUrl);
                xDoc.Save(strSavePath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion
        
        #region Ticker Init
        private void FnTickerInit()
        {
            try
            {
                if (!_TickerUse ||
                    _StructConfig.package.Equals("t0") ||
                    _StructConfig.package.Equals("f1")) return;

                // 크기
                double dblTempTickerPer = 0.6;
                double dblTickerHeightPercent;
                double dblTickerFontSize;

                // 가로, 세로
                if (this.Width > this.Height)
                {
                    dblTickerHeightPercent = 15; //10
                    dblTickerFontSize = 88;
                }
                else
                {
                    dblTickerHeightPercent = 8;
                    dblTickerFontSize = 76;
                }

                // 크게, 보통, 작게
                switch (_Main._StructDspConfig.ticker_font_size)
                {
                    case "big":
                        dblTempTickerPer = 1.0;
                        break;

                    case "normal":
                        dblTempTickerPer = 0.8;
                        break;

                    case "small":
                        dblTempTickerPer = 0.6;
                        break;

                    default:
                        dblTempTickerPer = 0.8;
                        break;
                }

                // Ticker Height
                _DoubleTickerHeight = rootGrid.Height * dblTickerHeightPercent * dblTempTickerPer / 100;

                // Font Size
                _DoubleTickerFontSize = dblTickerFontSize * dblTempTickerPer;

            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnCodeInit()");
            }

        } 
        #endregion

        #region PlayGird, Control Init
        private void FnGridInit()
        {
            try
            {
                double mainGridRowHeight0 = rootGrid.Height;
                double mainGridRowHeight1 = 0;

                // Ticker 사용 for templete a0, b0, b1, b2
                if (_TickerUse && (_StructConfig.package.Equals("a0") ||
                                   _StructConfig.package.Equals("b0") ||
                                   _StructConfig.package.Equals("b1") ||
                                   _StructConfig.package.Equals("b2")))

                {
                    mainGridRowHeight0 = mainGridRowHeight0 - _DoubleTickerHeight;
                    mainGridRowHeight1 = _DoubleTickerHeight;
                }

                // playGrid Row Set
                playGrid.RowDefinitions[0].Height = new GridLength(mainGridRowHeight0, GridUnitType.Pixel);
                playGrid.RowDefinitions[1].Height = new GridLength(mainGridRowHeight1, GridUnitType.Pixel);

                double dblTickerExpHeight = rootGrid.Height - _DoubleTickerHeight;    // Ticker 제외한 높이
                double dblWidth0, dblWidth1, dblHeight0, dblHeight1;
                double dblLeft_A, dblTop_A, dblWidth_A, dblHeight_A;
                double dblLeft_B, dblTop_B, dblWidth_B, dblHeight_B;

                switch (_StructConfig.package)
                {
                    case "b0":
                        dblWidth0 = _StructConfig.perA;
                        dblWidth1 = 100 - dblWidth0;
                        layoutGrid.ColumnDefinitions[0].Width = new GridLength(dblWidth0, GridUnitType.Star);
                        layoutGrid.ColumnDefinitions[1].Width = new GridLength(dblWidth1, GridUnitType.Star);
                        layoutGrid.RowDefinitions[0].Height = new GridLength(100, GridUnitType.Star);
                        layoutGrid.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Star);

                        // Play Grid Size
                        dblLeft_A = 0;
                        dblTop_A = 0;
                        dblWidth_A = rootGrid.Width * (dblWidth0 / 100);
                        dblHeight_A = dblTickerExpHeight;

                        // Screen 가로, 세로 비율 적용
                        _PlayLeft_A = (int)(dblLeft_A * _ScreenWidthRatio);
                        _PlayTop_A = (int)(dblTop_A * _ScreenHeightRatio);
                        _PlayWidth_A = (int)Math.Ceiling(dblWidth_A * _ScreenWidthRatio);
                        _PlayHeight_A = (int)Math.Ceiling(dblHeight_A * _ScreenHeightRatio);

                        // playGrid_A
                        movieGrid_A = new Grid();
                        movieGrid_A.SetValue(Grid.ColumnProperty, 0);
                        layoutGrid.Children.Add(movieGrid_A);
                        movieGrid_A.SetValue(Grid.RowSpanProperty, 2);

                        // Image, Flash Grid                    
                        //flashGrid_A = new Grid();
                        //flashGrid_A.SetValue(Grid.ColumnProperty, 0);
                        //layoutGrid.Children.Add(flashGrid_A);
                        //flashGrid_A.SetValue(Grid.RowSpanProperty, 2);

                        // WEB Grid                    
                        //WEBGrid_A = new Grid();
                        //WEBGrid_A.SetValue(Grid.ColumnProperty, 0);
                        //layoutGrid.Children.Add(WEBGrid_A);
                        //WEBGrid_A.SetValue(Grid.RowSpanProperty, 2);                   
                        layoutGrid.Children.Add(HtmlBrowser_A_Container);

                        // Play Grid Size
                        dblLeft_B = dblWidth_A;
                        dblTop_B = dblTop_A;
                        dblWidth_B = rootGrid.Width - dblWidth_A;
                        dblHeight_B = dblTickerExpHeight;

                        // Screen 가로, 세로 비율 적용
                        _PlayLeft_B = (int)(dblLeft_B * _ScreenWidthRatio);
                        _PlayTop_B = (int)(dblTop_B * _ScreenHeightRatio);
                        _PlayWidth_B = (int)Math.Ceiling(dblWidth_B * _ScreenWidthRatio);
                        _PlayHeight_B = (int)Math.Ceiling(dblHeight_B * _ScreenHeightRatio);

                        // Movie Grid_B
                        movieGrid_B = new Grid();
                        movieGrid_B.SetValue(Grid.ColumnProperty, 1);
                        layoutGrid.Children.Add(movieGrid_B);
                        movieGrid_B.SetValue(Grid.RowSpanProperty, 2);

                        // Flash Grid_B
                        //flashGrid_B = new Grid();
                        //flashGrid_B.SetValue(Grid.ColumnProperty, 1);
                        //layoutGrid.Children.Add(flashGrid_B);
                        //flashGrid_B.SetValue(Grid.RowSpanProperty, 2);

                        // Image Grid_B
                        imagePlayer_B = new Image
                        {
                            Stretch = Stretch.Fill
                        };
                        imagePlayer_B.SetValue(Grid.ColumnProperty, 1);
                        layoutGrid.Children.Add(imagePlayer_B);
                        imagePlayer_B.SetValue(Grid.RowSpanProperty, 2);

                        // WEB Grid_B
                        //WEBGrid_B = new Grid();
                        //WEBGrid_B.SetValue(Grid.ColumnProperty, 1);
                        //layoutGrid.Children.Add(WEBGrid_B);
                        //WEBGrid_B.SetValue(Grid.RowSpanProperty, 2);
                        layoutGrid.Children.Add(HtmlBrowser_B_Container);

                        break;

                    case "b1":
                        dblHeight0 = _StructConfig.perA;
                        dblHeight1 = 100 - dblHeight0;
                        layoutGrid.ColumnDefinitions[0].Width = new GridLength(100, GridUnitType.Star);
                        layoutGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Star);
                        layoutGrid.RowDefinitions[0].Height = new GridLength(dblHeight0, GridUnitType.Star);
                        layoutGrid.RowDefinitions[1].Height = new GridLength(dblHeight1, GridUnitType.Star);

                        // Screen_A Movie, Play Grid Size
                        dblLeft_A = 0;
                        dblTop_A = 0;
                        dblWidth_A = rootGrid.Width;
                        dblHeight_A = dblTickerExpHeight * (dblHeight0 / 100);

                        // Screen_A 가로, 세로 비율 적용
                        _PlayLeft_A = (int)(dblLeft_A * _ScreenWidthRatio);
                        _PlayTop_A = (int)(dblTop_A * _ScreenHeightRatio);
                        _PlayWidth_A = (int)Math.Ceiling(dblWidth_A * _ScreenWidthRatio);
                        _PlayHeight_A = (int)Math.Ceiling(dblHeight_A * _ScreenHeightRatio);

                        // Movie Grid_A
                        movieGrid_A = new Grid();
                        movieGrid_A.SetValue(Grid.RowProperty, 0);
                        layoutGrid.Children.Add(movieGrid_A);
                        movieGrid_A.SetValue(Grid.ColumnSpanProperty, 2);

                        // Flash Grid_A                    
                        //flashGrid_A = new Grid();
                        //flashGrid_A.SetValue(Grid.RowProperty, 0);
                        //layoutGrid.Children.Add(flashGrid_A);
                        //flashGrid_A.SetValue(Grid.ColumnSpanProperty, 2);

                        // WEB Grid                    
                        //WEBGrid_A = new Grid();
                        //WEBGrid_A.SetValue(Grid.RowProperty, 0);
                        //layoutGrid.Children.Add(WEBGrid_A);
                        //WEBGrid_A.SetValue(Grid.ColumnSpanProperty, 2);
                        layoutGrid.Children.Add(HtmlBrowser_A_Container);

                        // Screen_B Movie, Play Grid Size
                        dblLeft_B = 0;
                        dblTop_B = dblTop_A + dblHeight_A;
                        dblWidth_B = dblWidth_A;
                        dblHeight_B = dblTickerExpHeight - dblHeight_A;

                        // Screen_B 가로, 세로 비율 적용
                        _PlayLeft_B = (int)(dblLeft_B * _ScreenWidthRatio);
                        _PlayTop_B = (int)(dblTop_B * _ScreenHeightRatio);
                        _PlayWidth_B = (int)Math.Ceiling(dblWidth_B * _ScreenWidthRatio);
                        _PlayHeight_B = (int)Math.Ceiling(dblHeight_B * _ScreenHeightRatio);

                        // Movie Grid_B
                        movieGrid_B = new Grid();
                        movieGrid_B.SetValue(Grid.RowProperty, 1);
                        layoutGrid.Children.Add(movieGrid_B);
                        movieGrid_B.SetValue(Grid.ColumnSpanProperty, 2);

                        // Flash Grid_B                    
                        //flashGrid_B = new Grid();
                        //flashGrid_B.SetValue(Grid.RowProperty, 1);
                        //layoutGrid.Children.Add(flashGrid_B);
                        //flashGrid_B.SetValue(Grid.ColumnSpanProperty, 2);

                        // Image Grid_B
                        imagePlayer_B = new Image
                        {
                            Stretch = Stretch.Fill
                        };
                        imagePlayer_B.SetValue(Grid.RowProperty, 1);
                        layoutGrid.Children.Add(imagePlayer_B);
                        imagePlayer_B.SetValue(Grid.ColumnSpanProperty, 2);

                        // WEB Grid_B
                        //WEBGrid_B = new Grid();
                        //WEBGrid_B.SetValue(Grid.RowProperty, 1);
                        //layoutGrid.Children.Add(WEBGrid_B);
                        //WEBGrid_B.SetValue(Grid.ColumnSpanProperty, 2);
                        layoutGrid.Children.Add(HtmlBrowser_B_Container);

                        break;

                    case "e0":
                    case "e1":
                    case "e2":
                    case "e4":
                        layoutGrid.ColumnDefinitions[0].Width = new GridLength(100, GridUnitType.Star);
                        layoutGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Star);
                        layoutGrid.RowDefinitions[0].Height = new GridLength(100, GridUnitType.Star);
                        layoutGrid.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Star);

                        // Play Grid Size
                        dblLeft_A = 0;
                        dblTop_A = 0;
                        dblWidth_A = rootGrid.Width;
                        dblHeight_A = dblTickerExpHeight;

                        // Screen 가로, 세로 비율 적용
                        _PlayLeft_A = (int)(dblLeft_A * _ScreenWidthRatio);
                        _PlayTop_A = (int)(dblTop_A * _ScreenHeightRatio);
                        _PlayWidth_A = (int)Math.Ceiling(dblWidth_A * _ScreenWidthRatio);
                        _PlayHeight_A = (int)Math.Ceiling(dblHeight_A * _ScreenHeightRatio);

                        // movie Grid
                        movieGrid_A = new Grid();
                        movieGrid_A.SetValue(Grid.ColumnProperty, 0);
                        layoutGrid.Children.Add(movieGrid_A);
                        movieGrid_A.SetValue(Grid.ColumnSpanProperty, 2);
                        movieGrid_A.SetValue(Grid.RowSpanProperty, 2);

                        // Flash Grid                    
                        //flashGrid_A = new Grid();
                        //flashGrid_A.SetValue(Grid.ColumnProperty, 0);
                        //layoutGrid.Children.Add(flashGrid_A);
                        //flashGrid_A.SetValue(Grid.ColumnSpanProperty, 2);
                        //flashGrid_A.SetValue(Grid.RowSpanProperty, 2);                                                                    

                        // WEB Grid                    
                        //WEBGrid_A = new Grid();
                        //WEBGrid_A.SetValue(Grid.ColumnProperty, 0);
                        //layoutGrid.Children.Add(WEBGrid_A);
                        //WEBGrid_A.SetValue(Grid.RowSpanProperty, 2); 
                        layoutGrid.Children.Add(HtmlBrowser_A_Container);

                        break;

                    case "t0":
                        // image Grid
                        imagePlayer_A = new Image
                        {
                            Stretch = Stretch.Fill
                        };
                        t0_templete_player1_grid.Children.Add(imagePlayer_A);

                        // movie Grid
                        movieGrid_A = new Grid
                        {
                            Background = System.Windows.Media.Brushes.Black
                        };
                        t0_templete_player1_grid.Children.Add(movieGrid_A);

                        // WEB Grid  A for Chromium API
                        //HtmlBrowser_A_1_Container = new ContentControl();
                        //HtmlBrowser_A_1_Container.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch;
                        //t0_templete_player1_grid.Children.Add(HtmlBrowser_A_1_Container);
                        //HtmlBrowser_A_2_Container = new ContentControl();
                        //HtmlBrowser_A_2_Container.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch;
                        //t0_templete_player1_grid.Children.Add(HtmlBrowser_A_2_Container);


                        // WEB Grid for WebView2 API         
                        //WEBGrid_A = new Grid();
                        //WEBGrid_A.Background = System.Windows.Media.Brushes.Black;
                        //t0_templete_player1_grid.Children.Add(WEBGrid_A);
                        t0_templete_player1_grid.Children.Add(HtmlBrowser_A_Container);

                        //WEBGrid_A_2 = new Grid();
                        //WEBGrid_A_2.Background = System.Windows.Media.Brushes.Black;
                        //t0_templete_player1_grid.Children.Add(WEBGrid_A_2);                        

                        break;

                    case "f0":
                        // image Grid A
                        imagePlayer_A = new Image
                        {
                            Stretch = Stretch.Fill
                        };
                        f0_templete_player1_grid.Children.Add(imagePlayer_A);

                        // movie Grid A
                        movieGrid_A = new Grid
                        {
                            Background = System.Windows.Media.Brushes.Black
                        };
                        f0_templete_player1_grid.Children.Add(movieGrid_A);

                        // WEB Grid  A for Chromium API
                        //HtmlBrowser_A_1_Container = new ContentControl();
                        //HtmlBrowser_A_1_Container.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch;
                        //f0_templete_player1_grid.Children.Add(HtmlBrowser_A_1_Container);
                        //HtmlBrowser_A_2_Container = new ContentControl();
                        //HtmlBrowser_A_2_Container.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch;
                        //f0_templete_player1_grid.Children.Add(HtmlBrowser_A_2_Container);

                        // WEB Grid for WebView2 API          
                        //WEBGrid_A = new Grid
                        //{
                        //    Background = System.Windows.Media.Brushes.Black
                        //};
                        //f0_templete_player1_grid.Children.Add(WEBGrid_A);
                        f0_templete_player1_grid.Children.Add(HtmlBrowser_A_Container);

                        //WEBGrid_A_2 = new Grid();
                        //WEBGrid_A_2.Background = System.Windows.Media.Brushes.Black;
                        //f0_templete_player1_grid.Children.Add(WEBGrid_A_2);                        

                        // image Grid B
                        imagePlayer_B = new Image
                        {
                            Stretch = Stretch.Fill
                        };
                        f0_templete_player2_grid.Children.Add(imagePlayer_B);

                        // movie Grid B
                        movieGrid_B = new Grid
                        {
                            Background = System.Windows.Media.Brushes.Black
                        };
                        f0_templete_player2_grid.Children.Add(movieGrid_B);

                        // WEB Grid B
                        //HtmlBrowser_B_1_Container = new ContentControl();
                        //HtmlBrowser_B_1_Container.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch;
                        //f0_templete_player2_grid.Children.Add(HtmlBrowser_B_1_Container);
                        //HtmlBrowser_B_2_Container = new ContentControl();
                        //HtmlBrowser_B_2_Container.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch;
                        //f0_templete_player2_grid.Children.Add(HtmlBrowser_B_2_Container);


                        // WEB Grid          
                        //WEBGrid_B = new Grid
                        //{
                        //    Background = System.Windows.Media.Brushes.Black
                        //};
                        f0_templete_player2_grid.Children.Add(HtmlBrowser_B_Container);

                        //WEBGrid_A_2 = new Grid();
                        //WEBGrid_A_2.Background = System.Windows.Media.Brushes.Black;
                        //f0_templete_player1_grid.Children.Add(WEBGrid_B_2);                        

                        break;


                    case "f1":
                        layoutGrid.ColumnDefinitions[0].Width = new GridLength(100, GridUnitType.Star);
                        layoutGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Star);
                        layoutGrid.RowDefinitions[0].Height = new GridLength(100, GridUnitType.Star);
                        layoutGrid.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Star);

                        // Screen_A Movie, Play Grid Size
                        dblLeft_A = 0;
                        dblTop_A = 0;
                        dblWidth_A = rootGrid.Width;
                        dblHeight_A = f1_templete_player1.Height;//dblTickerExpHeight;

                        // Screen_A 가로, 세로 비율 적용
                        _PlayLeft_A = (int)(dblLeft_A * _ScreenWidthRatio);
                        _PlayTop_A = (int)(dblTop_A * _ScreenHeightRatio);
                        _PlayWidth_A = (int)Math.Ceiling(dblWidth_A * _ScreenWidthRatio);
                        _PlayHeight_A = (int)Math.Ceiling(dblHeight_A * _ScreenHeightRatio);

                        // Movie Grid_A
                        movieGrid_A = new Grid();
                        movieGrid_A.SetValue(Grid.ColumnProperty, 0);
                        layoutGrid.Children.Add(movieGrid_A);
                        movieGrid_A.SetValue(Grid.ColumnSpanProperty, 2);
                        movieGrid_A.SetValue(Grid.RowSpanProperty, 2);

                        // Flash Grid_A                    
                        //flashGrid_A = new Grid();
                        //flashGrid_A.SetValue(Grid.RowProperty, 0);
                        //layoutGrid.Children.Add(flashGrid_A);
                        //flashGrid_A.SetValue(Grid.ColumnSpanProperty, 2);
                        //flashGrid_A.SetValue(Grid.RowSpanProperty, 2);

                        // WEB Grid                    
                        //WEBGrid_A = new Grid();
                        //WEBGrid_A.SetValue(Grid.ColumnProperty, 0);
                       //layoutGrid.Children.Add(WEBGrid_A);
                        //WEBGrid_A.SetValue(Grid.ColumnSpanProperty, 2);
                        //WEBGrid_A.SetValue(Grid.RowSpanProperty, 2);                        
                        layoutGrid.Children.Add(HtmlBrowser_A_Container);

                        // Grid definition for playGrid_B
                        _PlayLeft_B = (int)(f1_templete_player2.Margin.Left * _ScreenWidthRatio);
                        _PlayTop_B = (int)(f1_templete_player2.Margin.Top * _ScreenHeightRatio);
                        _PlayWidth_B = (int)(f1_templete_player2.Width * _ScreenWidthRatio);
                        _PlayHeight_B = (int)(f1_templete_player2.Height * _ScreenHeightRatio);

                        // Movie Grid_B
                        movieGrid_B = new Grid();
                        f1_templete_player2.Children.Add(movieGrid_B);

                        // Flash Grid_B                    
                        //flashGrid_B = new Grid();
                        //flashGrid_B.SetValue(Grid.RowProperty, 1);
                        //layoutGrid.Children.Add(flashGrid_B);
                        //flashGrid_B.SetValue(Grid.ColumnSpanProperty, 2);

                        // Image Grid_B
                        imagePlayer_B = new Image
                        {
                            Stretch = Stretch.Fill
                        };
                        f1_templete_player2.Children.Add(imagePlayer_B);

                        // WEB Grid_B
                        //WEBGrid_B = new Grid();
                        f1_templete_player2.Children.Add(HtmlBrowser_B_Container);
                    
                        break;

                    default: //for package a0
                        layoutGrid.ColumnDefinitions[0].Width = new GridLength(100, GridUnitType.Star);
                        layoutGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Star);
                        layoutGrid.RowDefinitions[0].Height = new GridLength(100, GridUnitType.Star);
                        layoutGrid.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Star);

                        // Play Grid Size
                        dblLeft_A = 0;
                        dblTop_A = 0;
                        dblWidth_A = rootGrid.Width;
                        dblHeight_A = dblTickerExpHeight;

                        // Screen 가로, 세로 비율 적용
                        _PlayLeft_A = (int)(dblLeft_A * _ScreenWidthRatio);
                        _PlayTop_A = (int)(dblTop_A * _ScreenHeightRatio);
                        _PlayWidth_A = (int)Math.Ceiling(dblWidth_A * _ScreenWidthRatio);
                        _PlayHeight_A = (int)Math.Ceiling(dblHeight_A * _ScreenHeightRatio);

                        // movie Grid
                        movieGrid_A = new Grid();
                        movieGrid_A.SetValue(Grid.ColumnProperty, 0);
                        layoutGrid.Children.Add(movieGrid_A);
                        movieGrid_A.SetValue(Grid.ColumnSpanProperty, 2);
                        movieGrid_A.SetValue(Grid.RowSpanProperty, 2);

                        // Flash Grid                    
                        //flashGrid_A = new Grid();
                        //flashGrid_A.SetValue(Grid.ColumnProperty, 0);
                        //layoutGrid.Children.Add(flashGrid_A);
                        //flashGrid_A.SetValue(Grid.ColumnSpanProperty, 2);
                        //flashGrid_A.SetValue(Grid.RowSpanProperty, 2);                       
                                                
                        // WEB Grid                    
                        //WEBGrid_A = new Grid();
                        //WEBGrid_A.SetValue(Grid.ColumnProperty, 0);
                        //layoutGrid.Children.Add(WEBGrid_A);
                        //WEBGrid_A.SetValue(Grid.ColumnSpanProperty, 2);
                        //WEBGrid_A.SetValue(Grid.RowSpanProperty, 2);
                        layoutGrid.Children.Add(HtmlBrowser_A_Container);

                        break;
                }
            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnGridInit()");
            }
        }
        #endregion

        #region Playlist 구조체 Set
        private void FnLayoutStructSet()
        {
            try
            {
                List<object> _ListData = new List<object>();
                for (int i = 0; i < _LayoutCount; i++)
                {
                    string strXmlFormat = string.Format("//playlist/schedule[@mode='{0}'][@no='{1}']/layout[@no='{2}']/file", _Main._CurrentPlayScheduleMode, _Main._CurrentPlayScheduleNo, i);
                    foreach (XmlNode xNode in _Main._PlaylistXmlDoc.SelectNodes(strXmlFormat))
                    {
                        string strFileName = Path.GetFileName(_Main.FnXmlNodeCheck(xNode));
                        int nCIdx = _Main.FnString2Int(_Main.FnXmlNodeCheck(xNode.Attributes["id"]));

                        if (_Main._ListMusic.Contains(Path.GetExtension(strFileName).ToLower()))  // BGM
                        {
                            _ListStructBGM.Add(strFileName);
                            continue;
                        }

                        if (_Main._ListPpt.Contains(Path.GetExtension(strFileName).ToLower()))    // PPT
                        {
                            _PptPlayLayout = (i.Equals(0)) ? "A" : "B";
                        }

                        if (_Main._ListDtv.Contains(Path.GetExtension(strFileName).ToLower()))    // Dtv
                        {
                            _DtvPlayLayout = (i.Equals(0)) ? "A" : "B";
                        }

                        StructContents structContents = new StructContents
                        {
                            file_name = strFileName,
                            c_idx = nCIdx
                        };

                        switch (i)
                        {
                            case 0:
                                _ListStructContents_A.Add(structContents);
                                _ListData.Add(new Picture(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", strFileName), "Fill"));
                                break;
                            case 1:
                                _ListStructContents_B.Add(structContents);
                                break;
                            default:
                                break;
                        }
                    }
                }
                if (_ListData.Count > 0) this.DataContext = _data.ItemsSource = _ListData;
            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnLayoutStructSet()");
            }
        }
        #endregion

        //=====================================

        #region Play Start
        private void FnPlay()
        {
            try
            {
                // BGM
                if (_StructConfig.bgm.Equals("1") && _ListStructBGM.Count > 0)
                {
                    FnBgmPlay(0);
                }

                // 템플릿 재생
                if (!_StructConfig.package.Equals("a0") &&
                    !_StructConfig.package.Equals("b0") &&
                    !_StructConfig.package.Equals("b1") &&
                    !_StructConfig.package.Equals("b2"))
                    FnTemplatePlay();

                // 키오스크 재생
                if (_StructKiosk.use)
                {
                    _StructKiosk.mode = FnPlayMode(Path.GetExtension(_StructKiosk.file_name));
                    if (_StructKiosk.mode.Equals(ePlayMode.NULL))
                        _StructKiosk.mode = FnPlayMode(_StructKiosk.file_name);
                    if (_StructKiosk.mode.Equals(ePlayMode.NULL))
                        _StructKiosk.mode = FnPlayMode(System.IO.Path.GetExtension(_StructKiosk.file_name).ToLower());
                    if (!_StructKiosk.mode.Equals(ePlayMode.NULL))
                    {
                        Task.Factory.StartNew(() => { FnThreadKiosk(); });
                    }
                }

                // 레이아웃 컨텐츠 재생
                switch (_LayoutCount)
                {
                    case 1:
                        if (!(_StructKiosk.use && _StructKiosk.layout.Equals("A")))
                        {

                            movieGrid_A.Children.Add(moviePlayer_A);
                            //moviePlayer_A.UnloadedBehavior = MediaState.Stop; //default is MediaState.Close
                            moviePlayer_A.MediaEnded += Fn_MoviePlayer_A_MediaEnded;
                            moviePlayer_A.MediaFailed += Fn_MoviePlayer_A_MediaFailed;

                            //host_flashplayer_A.Background = Brushes.Yellow;
                            //host_flashplayer_A.Child = flashPlayer_A;
                            //flashGrid_A.Children.Add(host_flashplayer_A);     

                            HtmlBrowser_A_Container.Content = webView2Browser_A;  // a case of Microsoft_Edge WebView2 API

                            try
                            {
                                XmlDocument xDoc = new XmlDocument();
                                xDoc.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", "common", "contents_config.xml"));

                                _HtmlDispTime = int.Parse(xDoc.SelectSingleNode("//html_common/op_time").InnerText);
                            }
                            catch (Exception)
                            {
                                _HtmlDispTime = 20;
                            }

                            FnPlay_A(0);
                        }
                        break;

                    case 2:
                        if (!(_StructKiosk.use && _StructKiosk.layout.Equals("A")))
                        {
                            movieGrid_A.Children.Add(moviePlayer_A);
                            //moviePlayer_A.UnloadedBehavior = MediaState.Stop; //default is MediaState.Close
                            moviePlayer_A.MediaEnded += Fn_MoviePlayer_A_MediaEnded;
                            moviePlayer_A.MediaFailed += Fn_MoviePlayer_A_MediaFailed;

                            //host_flashplayer_A.Background = Brushes.Yellow;
                            //host_flashplayer_A.Child = flashPlayer_A;
                            //flashGrid_A.Children.Add(host_flashplayer_A);                                                                            

                            HtmlBrowser_A_Container.Content = webView2Browser_A;  // a case of Microsoft_Edge WebView2 API

                            try
                            {
                                XmlDocument xDoc = new XmlDocument();
                                xDoc.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", "common", "contents_config.xml"));

                                _HtmlDispTime = int.Parse(xDoc.SelectSingleNode("//html_common/op_time").InnerText);
                            }
                            catch (Exception)
                            {
                                _HtmlDispTime = 20;
                            }

                            FnPlay_A(0);
                        }

                        if (!(_StructKiosk.use && _StructKiosk.layout.Equals("B")))
                        {

                            movieGrid_B.Children.Add(moviePlayer_B);
                            //moviePlayer_B.UnloadedBehavior = MediaState.Stop; //default is MediaState.Close
                            moviePlayer_B.MediaEnded += Fn_MoviePlayer_B_MediaEnded;
                            moviePlayer_B.MediaFailed += Fn_MoviePlayer_B_MediaFailed;

                            //host_flashplayer_B.Background = Brushes.Yellow;
                            //host_flashplayer_B.Child = flashPlayer_B;
                            //flashGrid_B.Children.Add(host_flashplayer_B);

                            // a case of Microsoft_Edge WebView2 API
                            HtmlBrowser_B_Container.Content = webView2Browser_B;  // a case of Microsoft_Edge WebView2 API

                            if ( _HtmlDispTime == 0x00)
                            {
                                try
                                {
                                    XmlDocument xDoc = new XmlDocument();
                                    xDoc.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", "common", "contents_config.xml"));

                                    _HtmlDispTime = int.Parse(xDoc.SelectSingleNode("//html_common/op_time").InnerText);
                                }
                                catch (Exception)
                                {
                                    _HtmlDispTime = 20;
                                }
                            }

                            FnPlay_B(0);
                        }
                        break;

                    default:
                        break;
                }   //switch (_LayoutCount)
            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnPlay()");
            }
        }
        #endregion

        #region Template Play
        private void FnTemplatePlay()
        {
            try
            {
                // Background Image
                switch (_StructConfig.package)
                {
                    case "e0":
                    case "e1":
                    case "e2":
                        templete_bg_img.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", _StructConfig.package + ".jpg")));
                        break;

                    case "e4":
                        e4_templete_bg_img.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", _StructConfig.package + ".jpg")));
                        break;

                    case "t0":
                        t0_templete_bg_img.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", _StructConfig.package + ".png")));
                        break;

                    case "f0":
                        f0_templete_bg_img.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", _StructConfig.package + ".jpg")));
                        break;

                    case "f1":
                        f1_templete_bg_img.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", _StructConfig.package + ".jpg")));
                        break;

                    default:
                        break;
                }

                // playGrid Margin
                switch (_StructConfig.package)
                {
                    case "e0":
                    case "e1":
                    case "e2":
                        layoutGrid.Margin = new Thickness(templete_player.Margin.Left, templete_player.Margin.Top, 0, 0);
                        layoutGrid.Width = templete_player.Width;
                        layoutGrid.Height = templete_player.Height;
                        layoutGrid.HorizontalAlignment = templete_player.HorizontalAlignment;
                        layoutGrid.VerticalAlignment = templete_player.VerticalAlignment;
                        break;

                    case "e4":
                        layoutGrid.Margin = new Thickness(e4_templete_player.Margin.Left, e4_templete_player.Margin.Top, 0, 0);
                        layoutGrid.Width = e4_templete_player.Width;
                        layoutGrid.Height = e4_templete_player.Height;
                        layoutGrid.HorizontalAlignment = e4_templete_player.HorizontalAlignment;
                        layoutGrid.VerticalAlignment = e4_templete_player.VerticalAlignment;
                        break;

                    /*
                    case "t0":
                        layoutGrid.Margin = new Thickness(t0_templete_player1.Margin.Left, t0_templete_player1.Margin.Top, 0, 0);
                        layoutGrid.Width = t0_templete_player1.Width;
                        layoutGrid.Height = t0_templete_player1.Height;
                        layoutGrid.HorizontalAlignment = t0_templete_player1.HorizontalAlignment;
                        layoutGrid.VerticalAlignment = t0_templete_player1.VerticalAlignment;
                        break;
                    */

                    case "f1":
                        layoutGrid.Margin = new Thickness(f1_templete_player1.Margin.Left, f1_templete_player1.Margin.Top, 0, 0);
                        layoutGrid.Width = f1_templete_player1.Width;
                        layoutGrid.Height = f1_templete_player1.Height;
                        layoutGrid.HorizontalAlignment = f1_templete_player1.HorizontalAlignment;
                        layoutGrid.VerticalAlignment = f1_templete_player1.VerticalAlignment;
                        break;

                    default:
                        break;
                }
                
                //templete_player 해상도별 사이즈
                _PlayLeft_A = (int)(layoutGrid.Margin.Left * _ScreenWidthRatio);
                _PlayTop_A = (int)(layoutGrid.Margin.Top * _ScreenHeightRatio);
                _PlayWidth_A = (int)(layoutGrid.Width * _ScreenWidthRatio);
                _PlayHeight_A = (int)(layoutGrid.Height * _ScreenHeightRatio);
                
                //template procedures
                FnTemplateLogo();       // 로고
                FnTemplateClock();      // 시계
                //FnTemplateWeather();  // 날씨
                //FnWeatherProc();      //날씨  2017.07.07
                //FnTemplateStock();    // 주식 2017.07.07
                             
                //uniq procedure for each template,  한번만 수행한다. 
                FnTemplateUnique();

                // Visibility
                switch (_StructConfig.package)
                {
                    case "e0":
                    case "e1":
                    case "e2":
                        e4_templete_Main.Visibility = System.Windows.Visibility.Collapsed;
                        t0_templete_Main.Visibility = System.Windows.Visibility.Collapsed;
                        f0_templete_Main.Visibility = System.Windows.Visibility.Collapsed;
                        f1_templete_Main.Visibility = System.Windows.Visibility.Collapsed;
                        templete_Main.Visibility = System.Windows.Visibility.Visible;
                        break;

                    case "e4":
                        templete_Main.Visibility = System.Windows.Visibility.Collapsed;
                        t0_templete_Main.Visibility = System.Windows.Visibility.Collapsed;
                        f0_templete_Main.Visibility = System.Windows.Visibility.Collapsed;
                        f1_templete_Main.Visibility = System.Windows.Visibility.Collapsed;
                        e4_templete_Main.Visibility = System.Windows.Visibility.Visible;
                        break;

                    case "t0":
                        templete_Main.Visibility = System.Windows.Visibility.Collapsed;
                        e4_templete_Main.Visibility = System.Windows.Visibility.Collapsed;
                        f0_templete_Main.Visibility = System.Windows.Visibility.Collapsed;
                        f1_templete_Main.Visibility = System.Windows.Visibility.Collapsed;
                        t0_templete_Main.Visibility = System.Windows.Visibility.Visible;
                        break;

                    case "f0":
                        templete_Main.Visibility = System.Windows.Visibility.Collapsed;
                        e4_templete_Main.Visibility = System.Windows.Visibility.Collapsed;
                        t0_templete_Main.Visibility = System.Windows.Visibility.Collapsed;
                        f1_templete_Main.Visibility = System.Windows.Visibility.Collapsed;
                        f0_templete_Main.Visibility = System.Windows.Visibility.Visible;
                        break;

                    case "f1":
                        templete_Main.Visibility = System.Windows.Visibility.Collapsed;
                        e4_templete_Main.Visibility = System.Windows.Visibility.Collapsed;
                        t0_templete_Main.Visibility = System.Windows.Visibility.Collapsed;
                        f0_templete_Main.Visibility = System.Windows.Visibility.Collapsed;
                        f1_templete_Main.Visibility = System.Windows.Visibility.Visible;
                        break;

                    default:
                        break;
                }
                
            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnTemplatePlay()");
            }
        }

        public void FnTemplateLogo()
        {
            try
            {
                if (!(_StructConfig.package.Equals("e0") || 
                      _StructConfig.package.Equals("e1") || 
                      _StructConfig.package.Equals("e2") || 
                      _StructConfig.package.Equals("e4") )) return;

                string strLocalLogoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo", _Main.FnGetReg("Logo"));
                if (!File.Exists(strLocalLogoPath)) return;

                switch (_StructConfig.package)
                {
                    case "e0":
                    case "e1":
                    case "e2":
                        templete_logo_img.Source = new BitmapImage(new Uri(strLocalLogoPath, UriKind.RelativeOrAbsolute));
                        break;                

                    case "e4":
                        e4_templete_logo_img.Source = new BitmapImage(new Uri(strLocalLogoPath, UriKind.RelativeOrAbsolute));
                        break;
                
                    default:
                        break;
                }
                /*
                if (_StructConfig.package.Equals("e0") || _StructConfig.package.Equals("e1") || _StructConfig.package.Equals("e2"))
                    templete_logo_img.Source = new BitmapImage(new Uri(strLocalLogoPath, UriKind.RelativeOrAbsolute));
                else if (_StructConfig.package.Equals("e3"))
                    e3_templete_logo_img.Source = new BitmapImage(new Uri(strLocalLogoPath, UriKind.RelativeOrAbsolute));
                else  // for "e4"
                    e4_templete_logo_img.Source = new BitmapImage(new Uri(strLocalLogoPath, UriKind.RelativeOrAbsolute));
                */
            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnTemplateLogo()");
            }
        }

        private void FnTemplateClock()
        {
            try
            {
                if (!(_StructConfig.package.Equals("e0") ||
                      _StructConfig.package.Equals("e1") ||
                      _StructConfig.package.Equals("e2") ||                      
                      _StructConfig.package.Equals("e4") )) return;

                string strClockPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "weather_swf", "clock.swf");
                if (!File.Exists(strClockPath)) return;
                
                host_WEBplayer_Clock.Child = WEBBrowser_Clock;

                switch (_StructConfig.package)
                {
                    case "e0":
                    case "e1":
                    case "e2":
                        templete_clock.Children.Add(host_WEBplayer_Clock);
                        break;            

                    case "e4":
                        e4_templete_clock.Children.Add(host_WEBplayer_Clock);
                        break;

                    default:
                        break;
                }

                WEBBrowser_Clock.ScriptErrorsSuppressed = true;
                WEBBrowser_Clock.Navigate(strClockPath);
            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnTemplateClock()");
            }
        }

        private void FnTemplateDateTime()
        {
            try
            {
                string StrDayOfWeek = string.Empty;
                CurrentDateTime = DateTime.Now;

                switch (_StructConfig.package)
                {
                    case "t0":
                        t0_templete_date1.Text = CurrentDateTime.ToString("M.d dddd", new CultureInfo("ko-KR"));  // this show  2016.02.29 MON
                        t0_templete_time1.Text = CurrentDateTime.ToString("tt", new CultureInfo("en-US"));  // this show  AM/PM       
                        t0_templete_time2.Text = CurrentDateTime.ToString("h:mm", new CultureInfo("en-US"));  // this show  01:30       
                        break;

                    case "f0":
                        f0_templete_date1.Text = CurrentDateTime.ToString("yyyy.MM.dd ddd", new CultureInfo("en-US")).ToUpper();  // this show  2016.02.29 MON
                        f0_templete_time1.Text = CurrentDateTime.ToString("tt", new CultureInfo("en-US"));  // this show  AM/PM       
                        f0_templete_time2.Text = CurrentDateTime.ToString("h:mm", new CultureInfo("en-US"));  // this show  01:30       
                        break;

                    case "f1":
                        f1_templete_date1.Text = CurrentDateTime.ToString("yyyy.MM.dd ddd", new CultureInfo("en-US")).ToUpper();  // this show  2016.02.29 MON
                        f1_templete_time1.Text = CurrentDateTime.ToString("h:mm", new CultureInfo("en-US"));  // this show  01:30     
                        f1_templete_time2.Text = CurrentDateTime.ToString("tt", new CultureInfo("en-US"));  // this show  AM/PM                                 
                        break;

                    default:
                        break;

                }  // end of switch (_StructConfig.package)
            } 
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnTemplateDateTime()");
            }
        }

        public void FnTemplateWeather()
        {
            try
            {
                if (_StructConfig.package.Equals("a0") ||
                    _StructConfig.package.Equals("b0") ||
                    _StructConfig.package.Equals("b1") ||
                    _StructConfig.package.Equals("b2")) return;

                string StrDayOfWeek = string.Empty;                
                string strCity = _Main.FnGetCityName();
                string strCode = string.Empty;
                string strTemp = string.Empty;

                //-------------------------------------------------------------------------------------------
                // 웨더아이 현재 날씨 데이터 로드
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "rss_weatheri_current.xml"));
                
                //-------------------------------------------------------------------------------------------
                // 엠파트너스 날씨 예보 데이터 로드
                string loadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "data_mweather_today.txt");
                string weather_file_string = System.IO.File.ReadAllText(loadPath, Encoding.Default);
                char[] weather_list_delimiter_chars = { '|' };
                string[] weather_value_list = weather_file_string.Split(weather_list_delimiter_chars);
                //MessageBox.Show(Convert.ToString(weather_value_list[4]));
                
                //-------------------------------------------------------------------------------------------
                switch (_StructConfig.package)
                {
                    // 기상청 RSS 데이터를 사용한 코드 수정 필요함
                    case "e0":
                    case "e1":
                    case "e2":
                    case "e4":
                        foreach (XmlNode xmlNode in xDoc.SelectNodes("//channel/item/description/body/data"))
                        {
                            if (_StructConfig.package.Equals("e0") || _StructConfig.package.Equals("e1") || _StructConfig.package.Equals("e2"))
                            {
                                templete_weather_city.Text = strCity;
                                templete_weather_ment.Text = xmlNode.SelectSingleNode("wfKor").InnerText; // 설명
                                templete_weather_temp.Text = xmlNode.SelectSingleNode("temp").InnerText;  // 현재온도
                                templete_weather_hm.Text = xmlNode.SelectSingleNode("reh").InnerText;  // 습도
                            }
                            else  // for "e4"
                            {
                                e4_templete_weather_city.Text = strCity;

                                //strCode = string.Format("w0{0}",xmlNode.SelectSingleNode("sky").InnerText);  // 코드
                                e4_templete_weather_temp.Text = string.Format("{0}\u2103", xmlNode.SelectSingleNode("temp").InnerText);  // 현재온도
                                e4_templete_weather_ment.Text = xmlNode.SelectSingleNode("wfKor").InnerText; // 설명

                                if (xmlNode.SelectSingleNode("tmn").InnerText.Equals("-999.0"))  // 최저온도
                                    e4_templete_weather_tamin.Text = "NA";
                                else
                                    e4_templete_weather_tamin.Text = string.Format("{0} \u2103", xmlNode.SelectSingleNode("tmn").InnerText);

                                if (xmlNode.SelectSingleNode("tmx").InnerText.Equals("-999.0"))  // 최고온도
                                    e4_templete_weather_tamax.Text = "NA";
                                else
                                    e4_templete_weather_tamax.Text = string.Format("{0} \u2103", xmlNode.SelectSingleNode("tmx").InnerText);

                                strTemp = xmlNode.SelectSingleNode("ws").InnerText;
                                e4_templete_weather_ws.Text = string.Format("{0}", strTemp.Substring(0, 3)); // 풍속

                                e4_templete_weather_hm.Text = string.Format("{0}", xmlNode.SelectSingleNode("reh").InnerText);  // 습도
                                e4_templete_weather_pop.Text = string.Format("{0}", xmlNode.SelectSingleNode("pop").InnerText);  // 강수확률
                            }

                            strCode = WeatherFlashCode[Array.IndexOf(WeatherKorText, xmlNode.SelectSingleNode("wfKor").InnerText)];
                            //strCode = string.Format("w0{0}",xmlNode.SelectSingleNode("sky").InnerText);  // 코드
                            break;
                        }

                        if (string.IsNullOrEmpty(strCode)) return;

                        host_WEBplayer_Weather.Child = WEBBrowser_Weather;

                        if (_StructConfig.package.Equals("e0") || _StructConfig.package.Equals("e1") || _StructConfig.package.Equals("e2"))
                            templete_weather_flash.Children.Add(host_WEBplayer_Weather);
                        else  // for "e4"
                            e4_templete_weather_flash.Children.Add(host_WEBplayer_Weather);

                        string strWeatherPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "weather_swf", strCode + ".swf");
                        if (!File.Exists(strWeatherPath)) return;

                        WEBBrowser_Weather.ScriptErrorsSuppressed = true;
                        WEBBrowser_Weather.Navigate(strWeatherPath);

                        break; // for case "e0","e1","e2","e3","e4"              
                        
                    case "t0":
                      
                        // 웨더아이 현재 하늘상태와 온도
                        foreach (XmlNode xmlNode in xDoc.SelectNodes("//WX_CURRENT/RECORD"))
                        {
                            if ( xmlNode.SelectSingleNode("POINT_NAME").InnerText.Equals(WeatherCityCode[Array.IndexOf(WeatherAreas, strCity)]) )
                            {
                                t0_templete_weather_bg_img.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\weather_t0", xmlNode.SelectSingleNode("WX_CODE").InnerText + ".png"))); // 현재 하늘상태 아이콘
                                                                
                                double temp_temp = double.Parse(xmlNode.SelectSingleNode("TEMPERATURE").InnerText);                                
                                t0_templete_weather_temp1.Text = string.Format("{0}\u2103", Convert.ToString(Math.Round(temp_temp)));  // 현재온도

                                t0_templete_weather_ment1.Text = string.Format("{0}   {1}\u2103/{2}\u2103", WeatheriCurrentMent[Convert.ToInt32(xmlNode.SelectSingleNode("WX_CODE").InnerText)], Convert.ToString(weather_value_list[8]), Convert.ToString(weather_value_list[7]));  //현재날씨문구  최저온도/최고온도 

                                break;
                            }                            
                        }
                        
                        break; // for case "t0"

                    case "f0":
                        if (int.Parse(weather_value_list[12]) > 0 & int.Parse(weather_value_list[12]) < 22)
                            f0_templete_weather_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\weather_f0", Convert.ToString(weather_value_list[12]) + ".png"))); // 하늘상태 아이콘

                        // 오늘 하늘상태와 온도                        
                        f0_templete_weather_temp1.Text = string.Format("{0}/{1}°C", Convert.ToString(weather_value_list[8]), Convert.ToString(weather_value_list[7]));  //최저온도/최고온도
                        
                        break; // for case "f0"

                    case "f1":

                        // 웨더아이 현재 하늘상태와 온도
                        foreach (XmlNode xmlNode in xDoc.SelectNodes("//WX_CURRENT/RECORD"))
                        {
                            if (xmlNode.SelectSingleNode("POINT_NAME").InnerText.Equals(WeatherCityCode[Array.IndexOf(WeatherAreas, strCity)]))
                            {
                                f1_templete_weather_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\weather_f1", xmlNode.SelectSingleNode("WX_CODE").InnerText + ".png"))); // 현재 하늘상태 아이콘

                                double temp_temp = double.Parse(xmlNode.SelectSingleNode("TEMPERATURE").InnerText);
                                f1_templete_weather_temp1.Text = string.Format("{0}\u2103 {1}", Convert.ToString(Math.Round(temp_temp)), xmlNode.SelectSingleNode("WX").InnerText);  // 현재온도, 하늘상태
                                                             
                                break;
                            }
                        }

                        break; // for case "f1"

                    default:
                        break;

                } //switch (_StructConfig.package)
                
            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnTemplateWeather()");
            }
        }

        private void FnTemplateStock()
        {
            try
            {
                string strMsg = string.Empty;
                XmlDocument xDoc = new XmlDocument();

                switch (_StructConfig.package)
                {
                    case "e0":
                    case "e1":
                    case "e2":
                    case "e4":
                        
                        xDoc.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "rss_stock_citybank.xml"));

                        foreach (XmlNode xmlNode in xDoc.SelectNodes("//item"))
                        {
                            if (xmlNode.SelectSingleNode("name").InnerText.Equals("KOSPI"))
                            {
                                strMsg = xmlNode.SelectSingleNode("last").InnerText + "\r\n";
                                strMsg += xmlNode.SelectSingleNode("mark").InnerText + " ";
                                strMsg += xmlNode.SelectSingleNode("change1").InnerText.Replace("+", "").Replace("-", "");

                                if (_StructConfig.package.Equals("e0") || _StructConfig.package.Equals("e1") || _StructConfig.package.Equals("e2"))
                                {
                                    templete_kospi.Text = strMsg;
                                    templete_kospi.Foreground = (xmlNode.SelectSingleNode("mark").InnerText.Equals("▲")) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Blue;
                                }
                                else
                                {
                                    e4_templete_kospi.Text = strMsg;
                                    e4_templete_kospi.Foreground = (xmlNode.SelectSingleNode("mark").InnerText.Equals("▲")) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Blue;
                                }
                            }
                            else if (xmlNode.SelectSingleNode("name").InnerText.Equals("KOSDAQ"))
                            {
                                strMsg = xmlNode.SelectSingleNode("last").InnerText + "\r\n";
                                strMsg += xmlNode.SelectSingleNode("mark").InnerText + " ";
                                strMsg += xmlNode.SelectSingleNode("change1").InnerText.Replace("+", "").Replace("-", "");

                                if (_StructConfig.package.Equals("e0") || _StructConfig.package.Equals("e1") || _StructConfig.package.Equals("e2"))
                                {
                                    templete_kosdaq.Text = strMsg;
                                    templete_kosdaq.Foreground = (xmlNode.SelectSingleNode("mark").InnerText.Equals("▲")) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Blue;
                                }
                                else  // for "e4"
                                {
                                    e4_templete_kosdaq.Text = strMsg;
                                    e4_templete_kosdaq.Foreground = (xmlNode.SelectSingleNode("mark").InnerText.Equals("▲")) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Blue;
                                }
                            }

                        }  //end of foreach (XmlNode xmlNode in xDoc.SelectNodes("//item"))
                       
                        break;

                    case "f0":
                       
                        xDoc.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "rss_exchange_mpartners.xml"));

                        foreach (XmlNode xmlNode in xDoc.SelectNodes("//data"))
                        {

                            if (xmlNode.SelectSingleNode("type").InnerText.Equals("FX_USDKRW"))  // 미국 USD
                            {
                                f0_exchange_USD_text1.Text = xmlNode.SelectSingleNode("now").InnerText;
                                if (xmlNode.SelectSingleNode("change_up_down").InnerText.Equals("U"))
                                {
                                    f0_exchange_USD_text2.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0xD6, 0x2B, 0x2B));
                                    f0_exchange_USD_text2.Text = xmlNode.SelectSingleNode("change_value").InnerText;
                                    f0_exchange_USD_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\stock_f0", "up.png"))); // up, 빨간색                                    
                                }
                                else if (xmlNode.SelectSingleNode("change_up_down").InnerText.Equals("D"))
                                {
                                    f0_exchange_USD_text2.Foreground = System.Windows.Media.Brushes.SkyBlue;
                                    f0_exchange_USD_text2.Text = xmlNode.SelectSingleNode("change_value").InnerText.Replace('-', ' ');
                                    f0_exchange_USD_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\stock_f0", "down.png"))); // down,파란색                                   
                                }
                                else
                                {
                                    f0_exchange_USD_text2.Foreground = System.Windows.Media.Brushes.Black;
                                    f0_exchange_USD_text2.Text = xmlNode.SelectSingleNode("change_value").InnerText;
                                    f0_exchange_USD_ment1_img1.Source = null;
                                }
                            }
                            else if (xmlNode.SelectSingleNode("type").InnerText.Equals("FX_EURKRW"))  // 유럽 EUR
                            {
                                f0_exchange_EUR_text1.Text = xmlNode.SelectSingleNode("now").InnerText;
                                if (xmlNode.SelectSingleNode("change_up_down").InnerText.Equals("U"))
                                {
                                    f0_exchange_EUR_text2.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0xD6, 0x2B, 0x2B));
                                    f0_exchange_EUR_text2.Text = xmlNode.SelectSingleNode("change_value").InnerText;
                                    f0_exchange_EUR_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\stock_f0", "up.png"))); // up, 빨간색                                    
                                }
                                else if (xmlNode.SelectSingleNode("change_up_down").InnerText.Equals("D"))
                                {
                                    f0_exchange_EUR_text2.Foreground = System.Windows.Media.Brushes.SkyBlue;
                                    f0_exchange_EUR_text2.Text = xmlNode.SelectSingleNode("change_value").InnerText.Replace('-', ' ');
                                    f0_exchange_EUR_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\stock_f0", "down.png"))); // down,파란색                                   
                                }
                                else
                                {
                                    f0_exchange_EUR_text2.Foreground = System.Windows.Media.Brushes.Black;
                                    f0_exchange_EUR_text2.Text = xmlNode.SelectSingleNode("change_value").InnerText;
                                    f0_exchange_EUR_ment1_img1.Source = null;
                                }
                            }
                            else if (xmlNode.SelectSingleNode("type").InnerText.Equals("FX_JPYKRW"))  // 일본 JPY
                            {
                                f0_exchange_JPY_text1.Text = xmlNode.SelectSingleNode("now").InnerText;
                                if (xmlNode.SelectSingleNode("change_up_down").InnerText.Equals("U"))
                                {
                                    f0_exchange_JPY_text2.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0xD6, 0x2B, 0x2B));
                                    f0_exchange_JPY_text2.Text = xmlNode.SelectSingleNode("change_value").InnerText;
                                    f0_exchange_JPY_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\stock_f0", "up.png"))); // up, 빨간색                                    
                                }
                                else if (xmlNode.SelectSingleNode("change_up_down").InnerText.Equals("D"))
                                {
                                    f0_exchange_JPY_text2.Foreground = System.Windows.Media.Brushes.SkyBlue;
                                    f0_exchange_JPY_text2.Text = xmlNode.SelectSingleNode("change_value").InnerText.Replace('-', ' ');
                                    f0_exchange_JPY_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\stock_f0", "down.png"))); // down,파란색                                   
                                }
                                else
                                {
                                    f0_exchange_JPY_text2.Foreground = System.Windows.Media.Brushes.Black;
                                    f0_exchange_JPY_text2.Text = xmlNode.SelectSingleNode("change_value").InnerText;
                                    f0_exchange_JPY_ment1_img1.Source = null;
                                }
                            }
                            else if (xmlNode.SelectSingleNode("type").InnerText.Equals("FX_CNYKRW"))  // 중국 CNY
                            {
                                f0_exchange_CNY_text1.Text = xmlNode.SelectSingleNode("now").InnerText;
                                if (xmlNode.SelectSingleNode("change_up_down").InnerText.Equals("U"))
                                {
                                    f0_exchange_CNY_text2.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0xD6, 0x2B, 0x2B));
                                    f0_exchange_CNY_text2.Text = xmlNode.SelectSingleNode("change_value").InnerText;
                                    f0_exchange_CNY_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\stock_f0", "up.png"))); // up, 빨간색                                    
                                }
                                else if (xmlNode.SelectSingleNode("change_up_down").InnerText.Equals("D"))
                                {
                                    f0_exchange_CNY_text2.Foreground = System.Windows.Media.Brushes.SkyBlue;
                                    f0_exchange_CNY_text2.Text = xmlNode.SelectSingleNode("change_value").InnerText.Replace('-', ' ');
                                    f0_exchange_CNY_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\stock_f0", "down.png"))); // down,파란색                                   
                                }
                                else
                                {
                                    f0_exchange_CNY_text2.Foreground = System.Windows.Media.Brushes.Black;
                                    f0_exchange_CNY_text2.Text = xmlNode.SelectSingleNode("change_value").InnerText;
                                    f0_exchange_CNY_ment1_img1.Source = null;
                                }

                                break;
                            }

                        }  //end of foreach (XmlNode xmlNode in xDoc.SelectNodes("//data"))
                        
                        break;

                    default:
                        break;

                } // end of switch (_StructConfig.package)
            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnTemplateStock()");
            }
        
        }           

        // 미세먼지
        private void FnTemplateFinedust()
        {
            try
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "rss_finedust.xml"));

                switch (_StructConfig.package)
                {
                    case "dh":  
                        /*
                        dh_templete_dust_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\finedust", "icon_dust.png")));

                        foreach (XmlNode xmlNode in xDoc.SelectNodes("//xml/data"))
                        {
                            if (xmlNode.SelectSingleNode("sido").InnerText.Equals("서울"))
                            {
                                dh_templete_dust_temp2.Text = xmlNode.SelectSingleNode("pm10Value").InnerText + " \u338D\u002F\u33A5";
                                int pm10Value = Convert.ToInt32(xmlNode.SelectSingleNode("pm10Value").InnerText);

                                if (pm10Value >= 0 && pm10Value < 31)
                                {
                                    dh_templete_dust_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\finedust", "1.png")));
                                    dh_templete_dust_temp3.Text = "좋음";
                                }
                                else if (pm10Value >= 31 && pm10Value < 81)
                                {
                                    dh_templete_dust_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\finedust", "2.png")));
                                    dh_templete_dust_temp3.Text = "보통";
                                }
                                else if (pm10Value >= 81 && pm10Value < 151)
                                {
                                    dh_templete_dust_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\finedust", "3.png")));
                                    dh_templete_dust_temp3.Text = "나쁨";
                                }
                                else if (pm10Value >= 151)
                                {
                                    dh_templete_dust_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\finedust", "4.png")));
                                    dh_templete_dust_temp3.Text = "매우나쁨";
                                }
                                
                                break;
                            }
                        }
                        */ 
                        break;

                    case "dl":  
                        /*
                        dl_templete_dust_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\finedust", "icon_dust.png")));

                        foreach (XmlNode xmlNode in xDoc.SelectNodes("//xml/data"))
                        {
                            if (xmlNode.SelectSingleNode("sido").InnerText.Equals("경북"))
                            {
                                dl_templete_dust_temp3.Text = xmlNode.SelectSingleNode("pm10Value").InnerText + " \u338D\u002F";
                                int pm10Value = Convert.ToInt32(xmlNode.SelectSingleNode("pm10Value").InnerText);

                                if (pm10Value >= 0 && pm10Value < 31)
                                {
                                    dl_templete_dust_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\finedust", "1.png")));
                                    dl_templete_dust_temp4.Text = "좋음";
                                }
                                else if (pm10Value >= 31 && pm10Value < 81)
                                {
                                    dl_templete_dust_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\finedust", "2.png")));
                                    dl_templete_dust_temp4.Text = "보통";
                                }
                                else if (pm10Value >= 81 && pm10Value < 151)
                                {
                                    dl_templete_dust_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\finedust", "3.png")));
                                    dl_templete_dust_temp4.Text = "나쁨";
                                }
                                else if (pm10Value >= 151)
                                {
                                    dl_templete_dust_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\finedust", "4.png")));
                                    dl_templete_dust_temp4.Text = "매우나쁨";
                                }

                                break;
                            }
                        }
                        */ 
                        break;

                    case "f1":
                        
                        f1_templete_dust_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\finedust", "icon_dust.png")));

                        foreach (XmlNode xmlNode in xDoc.SelectNodes("//xml/data"))
                        {
                            if (xmlNode.SelectSingleNode("sido").InnerText.Equals("서울"))
                            {
                                int pm10Value = Convert.ToInt32(xmlNode.SelectSingleNode("pm10Value").InnerText);

                                if (pm10Value >= 0 && pm10Value < 31)
                                {
                                    f1_templete_dust_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\finedust", "1.png")));
                                    f1_templete_dust_ment2.Text = "좋음";
                                }
                                else if (pm10Value >= 31 && pm10Value < 81)
                                {
                                    f1_templete_dust_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\finedust", "2.png")));
                                    f1_templete_dust_ment2.Text = "보통";
                                }
                                else if (pm10Value >= 81 && pm10Value < 151)
                                {
                                    f1_templete_dust_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\finedust", "3.png")));
                                    f1_templete_dust_ment2.Text = "나쁨";
                                }
                                else if (pm10Value >= 151)
                                {
                                    f1_templete_dust_ment1_img1.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp\\finedust", "4.png")));
                                    f1_templete_dust_ment2.Text = "매우나쁨";
                                }

                                break;
                            }
                        }
                        
                        break;

                    default:
                        break;
                }   //switch (_StructConfig.package)
            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnTemplateFinedust()");
            }
        }
                
        //unique function for each packages(templates)
        private void FnTemplateUnique()
        {
            try
            {
                switch (_StructConfig.package)
                {
                    /*
                    case "s1":  // news ticker, repeat continuously
                        
                        string strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", "common", "s1_news_ticker.htm");

                        host_WEBplayer_E.Child = WEBBrowser_E;
                        WEBGrid_E = new Grid();
                        WEBGrid_E.Children.Add(host_WEBplayer_E);
                        s1_templete_ticker.Children.Add(WEBGrid_E);

                        WEBBrowser_E.ScriptErrorsSuppressed = true;
                        WEBBrowser_E.Navigate(strFileFullPath);
                        
                        break;

                    case "s3":  // news ticker, repeat continuously
                        
                        string strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", "common", "s3_news_ticker.htm");

                        host_WEBplayer_E.Child = WEBBrowser_E;
                        WEBGrid_E = new Grid();
                        WEBGrid_E.Children.Add(host_WEBplayer_E);
                        s3_templete_ticker.Children.Add(WEBGrid_E);

                        WEBBrowser_E.ScriptErrorsSuppressed = true;
                        WEBBrowser_E.Navigate(strFileFullPath);
                        
                        break;
                    */

                    case "t0":
                        
                        // 미세먼지, HTML 코드에서 일정 간격으로 업데이트 함
                        string strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", "common", "mise_mini_link.htm");
                        
                        break;

                    default:
                        break;

                }  // switch (_StructConfig.package)    
            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnTemplateUnique()");
            }
 
        }   //  private void FnTemplateUnique()       
    
        #endregion

        //---------------------------------------------------------------------------------------------------------------------
        #region Play A
        private void FnPlay_A(int p)
        {
            try
            {
                _CurrentPlayNo_A = p;
                if (_CurrentPlayNo_A >= _ListStructContents_A.Count)
                {
                    if (_CyclePlayCount_A.Equals(0))
                    {                                                                
                        _Main.FnQueueAdd(eQueueMode.StatusWrite, string.Format("Function FnPlay_A error {0}", Convert.ToString(_ListStructContents_A.Count)));

                        _Main.FnPlayerErrorProc("재생할 컨텐츠 없슴");
                        return;
                    }
                    _CyclePlayCount_A = 0;
                    FnPlay_A(0);
                    return;
                }

                string strFileName = _ListStructContents_A[_CurrentPlayNo_A].file_name;                                

                // File Check
                string strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", strFileName);
                if (!File.Exists(strFileFullPath))
                {
                    FnPlay_A(++_CurrentPlayNo_A);
                    return;
                }

                switch (FnPlayMode(Path.GetExtension(strFileName).ToLower()))
                {
                    case ePlayMode.Movie:
                        _IsFirstImage_A = true;
                        _CurrentPlayMode_A = ePlayMode.Movie;
                        FnMoviePlay_A();
                        break;

                    case ePlayMode.Image:
                        _CurrentPlayMode_A = ePlayMode.Image;
                        FnImagePlay_A();
                        break;

                    case ePlayMode.Flash:
                        //_IsFirstImage_A = true;
                        //_CurrentPlayMode_A = ePlayMode.Flash;
                        //FnFlashPlay_A();
                        FnPlay_A(++_CurrentPlayNo_A);
                        break;

                    case ePlayMode.Powerpoint:
                        if (!_PptPlayLayout.Equals("A")) { FnPlay_A(++_CurrentPlayNo_A); break; }
                        _IsFirstImage_A = true;
                        _CurrentPlayMode_A = ePlayMode.Powerpoint;
                        FnPPTPlay_A();
                        break;      
             
                    case ePlayMode.Dtv:
                        if (!_DtvPlayLayout.Equals("A")) { FnPlay_A(++_CurrentPlayNo_A); break; }
                        _IsFirstImage_A = true;
                        _CurrentPlayMode_A = ePlayMode.Dtv;
                        FnDTVPlay_A();
                        break;

                    case ePlayMode.Html:
                        _IsFirstImage_A = true;
                        _CurrentPlayMode_A = ePlayMode.Html;
                        FnHtmlPlay_A();
                        break;
                    
                    default:
                        FnPlay_A(++_CurrentPlayNo_A);
                        break;
                }
            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnPlay_A()");

                FnPlay_A(++_CurrentPlayNo_A);
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------
        private void FnMoviePlay_A()
        {
            Task.Factory.StartNew(() => { FnThreadMovieStart_A(); });
            //If you want to use Task(), You can not use the MediaElement object.
            //If you want the thread, You should use a "Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { codes .... }));"
        }
        private void FnThreadMovieStart_A()
        {            
            try
            {
                string strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", _ListStructContents_A[_CurrentPlayNo_A].file_name);
                              
                // Code using MediaElement in the XAML  
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() {
                    
                    //movieElement.StretchDirection = StretchDirection.UpOnly;
                    moviePlayer_A.Volume = (double)_Main.FnString2Int(_Main._StructDspConfig.volume) / 31;
                    moviePlayer_A.Stretch = Stretch.Fill;
                    moviePlayer_A.Source = new Uri(strFileFullPath); // When set the Source, It will be played automatically

                    if (_OnExecution_BGM == true) _BGMplayer.Pause();
                    
                    while (moviePlayer_A.Position.Milliseconds < 300) ; // waiting for exchanging the screen for loading time 
                
                    movieGrid_A.Visibility = Visibility.Visible;
                    //flashGrid_A.Visibility = Visibility.Hidden;
                    if (_StructConfig.package.Equals("t0") || _StructConfig.package.Equals("f0"))
                    {
                        imagePlayer_A.Visibility = Visibility.Hidden;
                        
                        //WEBGrid_A.Visibility = Visibility.Hidden;
                        HtmlBrowser_A_Container.Visibility = Visibility.Hidden;
                        //HtmlBrowser_A_2_Container.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        grid_transition.Visibility = Visibility.Hidden;

                        //WEBGrid_A.Visibility = Visibility.Hidden;                        
                        HtmlBrowser_A_Container.Visibility = Visibility.Hidden;
                    }                    
                }));
                
                // Live, Log
                _Main._StructLive.file_name_A = _ListStructContents_A[_CurrentPlayNo_A].file_name;
                _Main._StructLive.thumb_img_A = _ListStructContents_A[_CurrentPlayNo_A].c_idx + ".jpg";
                _Main.FnPlayerLiveLog("A");
            }
            catch (Exception)
            {
                FnMovieStop_A();
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlay_A(++_CurrentPlayNo_A); }));
            }
        }
        // The case of MediaElement for playing Movie
        // When the media playback is finished. Stop() the media to seek to media start.
        private void Fn_MoviePlayer_A_MediaEnded(object sender, EventArgs e)
        {
            try
            {
                //moviePlayer_A.Stop(); // Because it is stopped while the last frame is displied, This code does not need, 
                //moviePlayer_A.Close();

                //---------2017. 11.7 by John----------------------------------------------------------------
                if (_OnExecution_BGM == true)
                {
                    int temp = _CurrentPlayNo_A + 1;
                    if (temp >= _ListStructContents_A.Count)
                        temp = 0;

                    string strFileName = _ListStructContents_A[(temp)].file_name;
                    if ((FnPlayMode(Path.GetExtension(strFileName).ToLower()) != ePlayMode.Movie) && (_CurrentPlayMode_B != ePlayMode.Movie))
                    {
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { _BGMplayer.Play(); }));
                    }
                }
                //-------------------------------------------------------------------------------------------

                _CyclePlayCount_A++;
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlay_A(++_CurrentPlayNo_A); }));
            }
            catch (Exception) 
            {
                _CyclePlayCount_A++;
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlay_A(++_CurrentPlayNo_A); }));
            }
        }
        private void Fn_MoviePlayer_A_MediaFailed(object sender, EventArgs e)
        {
            try
            {
                moviePlayer_A.Stop(); 
                moviePlayer_A.Close();

                //---------2017. 11.7 by John----------------------------------------------------------------
                if (_OnExecution_BGM == true)
                {
                    int temp = _CurrentPlayNo_A + 1;
                    if (temp >= _ListStructContents_A.Count)
                        temp = 0;

                    string strFileName = _ListStructContents_A[(temp)].file_name;
                    if ((FnPlayMode(Path.GetExtension(strFileName).ToLower()) != ePlayMode.Movie) && (_CurrentPlayMode_B != ePlayMode.Movie))
                    {
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { _BGMplayer.Play(); }));
                    }
                }
                //-------------------------------------------------------------------------------------------
                                
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Fn_MoviePlayer_A_MediaFailed()");

                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlay_A(++_CurrentPlayNo_A); }));
            }
            catch (Exception)
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlay_A(++_CurrentPlayNo_A); }));
            }
        }
        private void FnMovieStop_A()
        {
            try
            {
                moviePlayer_A.Stop();
                moviePlayer_A.Close();                
            }
            catch (Exception) { }
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------
        private void FnImagePlay_A()
        {
            try
            {
                if (_StructConfig.package.Equals("t0") || _StructConfig.package.Equals("f0"))
                {
                    imagePlayer_A.Visibility = Visibility.Visible;
                    //flashGrid_A.Visibility = Visibility.Hidden;
                    movieGrid_A.Visibility = Visibility.Hidden;

                    HtmlBrowser_A_Container.Visibility = Visibility.Hidden;
                    //WEBGrid_A.Visibility = Visibility.Hidden;
                    //HtmlBrowser_A_1_Container.Visibility = Visibility.Hidden;
                    //HtmlBrowser_A_2_Container.Visibility = Visibility.Hidden;

                    string strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", _ListStructContents_A[_CurrentPlayNo_A].file_name);
                    BitmapImage bi = new BitmapImage(new Uri(strFileFullPath, UriKind.RelativeOrAbsolute));
                    bi.Freeze();
                    imagePlayer_A.Source = bi;
                }
                else
                {                
                    
                    grid_transition.Visibility = Visibility.Visible;
                    //flashGrid_A.Visibility = Visibility.Hidden;
                    movieGrid_A.Visibility = Visibility.Hidden;
                    HtmlBrowser_A_Container.Visibility = Visibility.Hidden;
                    //WEBGrid_A.Visibility = Visibility.Hidden;                   

                    // Win XP 일때 0 ~ 11
                    int nTransitionCount = (Environment.OSVersion.Version.Major < 6) ? 12 : _transitions.Items.Count;                    
                    int img_no = (_IsFirstImage_A) ? -1 : (_Main._StructDspConfig.transition.Equals("disable")) ? -1 : random.Next(0, nTransitionCount);
                    _transitions.SelectedIndex = img_no;
                    _data.SelectedIndex = _CurrentPlayNo_A;                    
                }
  
                Timer_imageA.Interval = TimeSpan.FromSeconds(_Main.FnString2Int(_Main._StructDspConfig.duration));
                Timer_imageA.Start();              

                // Live, Log
                _Main._StructLive.file_name_A = _ListStructContents_A[_CurrentPlayNo_A].file_name;
                _Main._StructLive.thumb_img_A = _ListStructContents_A[_CurrentPlayNo_A].c_idx + ".jpg";
                _Main.FnPlayerLiveLog("A");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnImagePlay_A()");

                Timer_imageA.Stop();                          
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () { FnPlay_A(++_CurrentPlayNo_A); }));
            }
        }
        void Fn_Timer_ImageA_Tick(object sender, EventArgs e)
        {
            Timer_imageA.Stop();
            _IsFirstImage_A = false;

            _CyclePlayCount_A++;
            FnPlay_A(++_CurrentPlayNo_A);
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------
        private void FnHtmlPlay_A()
        {
            //System.Windows.MessageBox.Show(WEBBrowser_A.Version.ToString());
            //Task.Factory.StartNew(() => { FnThreadHtmlAasync(); });
            FnThreadHtmlAasync();
        }
        private void FnThreadHtmlAasync()        
        {
            try
            {
                string strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", _ListStructContents_A[_CurrentPlayNo_A].file_name);

                if (webView2Browser_A.Source == new Uri(strFileFullPath, UriKind.Absolute))
                    webView2Browser_A.Reload();
                else
                    webView2Browser_A.Source = new Uri(strFileFullPath, UriKind.Absolute);

                // Live, Log
                _Main._StructLive.file_name_A = _ListStructContents_A[_CurrentPlayNo_A].file_name;
                _Main._StructLive.thumb_img_A = _ListStructContents_A[_CurrentPlayNo_A].c_idx + ".jpg";
                _Main.FnPlayerLiveLog("A");
            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnThreadHtmlAasync()");

                webView2Browser_A.Stop();
                //FnPlay_A(++_CurrentPlayNo_A);
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlay_A(++_CurrentPlayNo_A); }));
            }
        }
        private async void WebView2Browser_A_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            if (_StructConfig.package.Equals("t0") || _StructConfig.package.Equals("f0"))
            {
                HtmlBrowser_A_Container.Visibility = Visibility.Visible;
                //WEBGrid_A.Visibility = Visibility.Visible;
                webView2Browser_A.UpdateWindowPos();    // Parent Control (WEBGrid_A) Visible 이후 HTML 화면이 안나오는 경우를 피하기 위하여 추가함 
                                                        // WebView2 windows are blank when created in the background
                                                        //flashGrid_A.Visibility = Visibility.Hidden;
                imagePlayer_A.Visibility = Visibility.Hidden;
                movieGrid_A.Visibility = Visibility.Hidden;
            }
            else
            {
                HtmlBrowser_A_Container.Visibility = Visibility.Visible;
                webView2Browser_A.UpdateWindowPos();    // Parent Control (WEBGrid_A) Visible 이후 HTML 화면이 안나오는 경우를 피하기 위하여 추가함 
                                                        // WebView2 windows are blank when created in the background
                                                        //flashGrid_A.Visibility = Visibility.Hidden;
                grid_transition.Visibility = Visibility.Hidden;
                movieGrid_A.Visibility = Visibility.Hidden;
            }


            try
            {
                var result = await webView2Browser_A.CoreWebView2.ExecuteScriptAsync("play_time()");
                Timer_WEBA.Interval = TimeSpan.FromSeconds(int.Parse(result));
                Timer_WEBA.Start();
            }
            catch (Exception)
            {
                Timer_WEBA.Interval = TimeSpan.FromSeconds(_HtmlDispTime);
                Timer_WEBA.Start();
            }
        }
        private void WebView2Browser_A_ProcessFailed(object sender, Microsoft.Web.WebView2.Core.CoreWebView2ProcessFailedEventArgs e)
        {
            webView2Browser_A.Stop();

            _Main.FnQueueAdd(eQueueMode.StatusWrite, "Fn_MoviePlayer_A_MediaFailed()");

            FnPlay_A(++_CurrentPlayNo_A);
            //Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () { fnPlay_A(++_CurrentPlayNo_A); }));
        }
        void Fn_Timer_WEBA_Tick(object sender, EventArgs e)
        {
            Timer_WEBA.Stop();
            webView2Browser_A.Stop();

            _CyclePlayCount_A++;
            //FnPlay_A(++_CurrentPlayNo_A);
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlay_A(++_CurrentPlayNo_A); }));
        }
       

        //------------------------------------------------------------------------------------------------------------------------------------------------------
        #region Flash play in A area
        /*
        private void FnFlashPlay_A()
        {
            //windowsFormsHost host = new WindowsFormsHost();
            //host.Child = flashPlayer_A;
            //FlashGrid_A.Children.Add(host);

            //FnThreadFlashA();
            Task.Factory.StartNew(() => { FnThreadFlashA(); });
        }
        private void FnThreadFlashA()
        {
            try
            {
                string strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", _ListStructContents_A[_CurrentPlayNo_A].file_name);
                flashPlayer_A.LoadMovie(0, strFileFullPath);

                while (flashPlayer_A.PercentLoaded() < 100) ; // to remove a black screen flip when be changed between media grid. 

                flashPlayer_A.Play();
                flashPlayer_A.ScaleMode = 2;
                flashPlayer_A.Loop = false;
                flashPlayer_A.Menu = false;
                
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() {
                    flashGrid_A.Visibility = Visibility.Visible;
                    grid_transition.Visibility = Visibility.Hidden;
                    movieGrid_A.Visibility = Visibility.Hidden;
                    WEBGrid_A.Visibility = Visibility.Hidden;
                }));

                //Timer_FlashA.Interval = TimeSpan.FromMilliseconds(10000);
                Timer_FlashA.Start();

                // Live, Log
                _Main._StructLive.file_name_A = _ListStructContents_A[_CurrentPlayNo_A].file_name;
                _Main._StructLive.thumb_img_A = _ListStructContents_A[_CurrentPlayNo_A].c_idx + ".jpg";
                _Main.FnPlayerLiveLog("A");
            }
            catch (Exception)
            {
                //FnPlay_A(++_CurrentPlayNo_A);
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlay_A(++_CurrentPlayNo_A); }));
            }
        }
        void Timer_FlashA_Tick(object sender, EventArgs e)
        {
            if (!flashPlayer_A.Playing)
            {
                Timer_FlashA.Stop();
                FnFlashStop(flashPlayer_A);

                _CyclePlayCount_A++;
                //FnPlay_A(++_CurrentPlayNo_A);
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlay_A(++_CurrentPlayNo_A); }));
            }
        }
        */
        #endregion

        //------------------------------------------------------------------------------------------------------------------------------------------------------
        private void FnPPTPlay_A()
        {
            grid_transition.Visibility = Visibility.Hidden;
            //flashGrid_A.Visibility = Visibility.Hidden;
            movieGrid_A.Visibility = Visibility.Hidden;
            //WEBGrid_A.Visibility = Visibility.Hidden;
            HtmlBrowser_A_Container.Visibility = Visibility.Hidden;

            Task.Factory.StartNew(() => { FnThreadPPT(); });
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------
        private void FnDTVPlay_A()
        {
            grid_transition.Visibility = Visibility.Hidden;
            //flashGrid_A.Visibility = Visibility.Hidden;
            movieGrid_A.Visibility = Visibility.Hidden;
            //WEBGrid_A.Visibility = Visibility.Hidden;
            HtmlBrowser_A_Container.Visibility = Visibility.Hidden;

            Task.Factory.StartNew(() => { FnThreadDTV(); });
        }
                                    
        #endregion

        //*************************************************************************************************************************
        #region Play B
        private void FnPlay_B(int p)
        {
            try
            {
                _CurrentPlayNo_B = p;
                if (_CurrentPlayNo_B >= _ListStructContents_B.Count)
                {
                    if (_CyclePlayCount_B > 0)
                    {
                        _CyclePlayCount_B = 0;
                        FnPlay_B(0);
                    }
                    return;
                }

                string strFileName = _ListStructContents_B[_CurrentPlayNo_B].file_name;
                
                // File Check
                string strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", strFileName);
                if (!File.Exists(strFileFullPath))
                {
                    FnPlay_B(++_CurrentPlayNo_B);
                    return;
                }

                switch (FnPlayMode(Path.GetExtension(strFileName).ToLower()))
                {
                    case ePlayMode.Movie:
                        _CurrentPlayMode_B = ePlayMode.Movie;
                        FnMoviePlay_B();
                        break;

                    case ePlayMode.Image:
                        _CurrentPlayMode_B = ePlayMode.Image;
                        FnImagePlay_B();
                        break;

                    case ePlayMode.Flash:
                        //_CurrentPlayMode_B = ePlayMode.Flash;
                        //FnFlashPlay_B();
                        FnPlay_B(++_CurrentPlayNo_B);
                        break;

                    case ePlayMode.Powerpoint:
                        if (!_PptPlayLayout.Equals("B")) { FnPlay_B(++_CurrentPlayNo_B); break; }
                        _CurrentPlayMode_B = ePlayMode.Powerpoint;
                        FnPPTPlay_B();
                        break;

                    case ePlayMode.Dtv:
                        if (!_DtvPlayLayout.Equals("B")) { FnPlay_B(++_CurrentPlayNo_B); break; }
                        _CurrentPlayMode_B = ePlayMode.Dtv;
                        FnDTVPlay_B();
                        break;

                    case ePlayMode.Html:
                        _CurrentPlayMode_B = ePlayMode.Html;
                        FnHtmlPlay_B();
                        break;

                    case ePlayMode.Cctv:
                        _CurrentPlayMode_B = ePlayMode.Cctv;
                        FnPlay_B(++_CurrentPlayNo_B);
                        //FnCctvPlay_B();
                        break;

                    default:
                        FnPlay_B(++_CurrentPlayNo_B);
                        break;
                }
            }
            catch (Exception)
            {
                FnPlay_B(++_CurrentPlayNo_B);
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------
        private void FnMoviePlay_B()
        {
            Task.Factory.StartNew(() => { FnThreadMovieStart_B(); });
            //If you want to use Task(), You can not use the MediaElement object.
            //If you want the thread, You should use a "Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { codes .... }));"
        }
        private void FnThreadMovieStart_B()
        {
            try
            {
                string strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", _ListStructContents_B[_CurrentPlayNo_B].file_name);
                
                // Code using MediaElement in the XAML  
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                {
                    //movieElement.StretchDirection = StretchDirection.UpOnly;
                    moviePlayer_B.Volume = (double)_Main.FnString2Int(_Main._StructDspConfig.volume) / 31;
                    moviePlayer_B.Stretch = Stretch.Fill;
                    moviePlayer_B.Source = new Uri(strFileFullPath); // When set the Source, It will be played automatically

                    if (_OnExecution_BGM == true)
                    {
                        _BGMplayer.Pause();
                    }

                    while (moviePlayer_B.Position.Milliseconds < 500) ; // waiting for exchanging the screen for loading time 

                    movieGrid_B.Visibility = Visibility.Visible;
                    //flashGrid_B.Visibility = Visibility.Hidden;
                    if (_StructConfig.package.Equals("t0") || _StructConfig.package.Equals("f0"))
                    {
                        imagePlayer_B.Visibility = Visibility.Hidden;
                        HtmlBrowser_B_Container.Visibility = Visibility.Hidden;
                        //WEBGrid_B.Visibility = Visibility.Hidden;
                        //HtmlBrowser_B_1_Container.Visibility = Visibility.Hidden;
                        //HtmlBrowser_B_2_Container.Visibility = Visibility.Hidden;
                    }
                    else
                    {

                        imagePlayer_B.Visibility = Visibility.Hidden;
                        HtmlBrowser_B_Container.Visibility = Visibility.Hidden;
                        //WEBGrid_B.Visibility = Visibility.Hidden;                       
                    }
                  
                }));
                
                // Live, Log
                _Main._StructLive.file_name_B = _ListStructContents_B[_CurrentPlayNo_B].file_name;
                _Main._StructLive.thumb_img_B = _ListStructContents_B[_CurrentPlayNo_B].c_idx + ".jpg";
                _Main.FnPlayerLiveLog("B");
            }
            catch (Exception)
            {
                FnMovieStop_B();
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlay_B(++_CurrentPlayNo_B); }));
            }
        }
        private void Fn_MoviePlayer_B_MediaEnded(object sender, EventArgs e)
        {
            try
            {
                //movieElement.Stop(); // Because it is stopped while the last frame is displied, This code does not need, 

                //---------2017. 11.7 by John----------------------------------------------------------------
                if (_OnExecution_BGM == true)
                {
                    int temp = _CurrentPlayNo_B + 1;
                    if (temp >= _ListStructContents_B.Count)
                        temp = 0;

                    string strFileName = _ListStructContents_B[(temp)].file_name;
                    if ((FnPlayMode(Path.GetExtension(strFileName).ToLower()) != ePlayMode.Movie) & (_CurrentPlayMode_A != ePlayMode.Movie))
                    {
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { _BGMplayer.Play(); }));
                    }
                }
                //-------------------------------------------------------------------------------------------

                _CyclePlayCount_B++;
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlay_B(++_CurrentPlayNo_B); }));
            }
            catch (Exception)
            {
                _CyclePlayCount_B++;
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlay_B(++_CurrentPlayNo_B); }));
            }
        }
        private void Fn_MoviePlayer_B_MediaFailed(object sender, EventArgs e)
        {
            try
            {
                moviePlayer_B.Stop();
                moviePlayer_B.Close();

                //---------2017. 11.7 by John----------------------------------------------------------------
                if (_OnExecution_BGM == true)
                {
                    int temp = _CurrentPlayNo_B + 1;
                    if (temp >= _ListStructContents_B.Count)
                        temp = 0;

                    string strFileName = _ListStructContents_B[(temp)].file_name;
                    if ((FnPlayMode(Path.GetExtension(strFileName).ToLower()) != ePlayMode.Movie) && (_CurrentPlayMode_A != ePlayMode.Movie))
                    {
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () { _BGMplayer.Play(); }));
                    }
                }
                //-------------------------------------------------------------------------------------------

                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Fn_MoviePlayer_B_MediaFailed()");

                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () { FnPlay_B(++_CurrentPlayNo_B); }));
            }
            catch (Exception)
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () { FnPlay_B(++_CurrentPlayNo_B); }));
            }
        }
        private void FnMovieStop_B()
        {
            try
            {
                moviePlayer_B.Stop();
                moviePlayer_B.Close();
            }
            catch (Exception) {  }
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------
        private void FnImagePlay_B()
        {
            try
            {
                if (_StructConfig.package.Equals("t0") || _StructConfig.package.Equals("f0"))
                    {
                        imagePlayer_B.Visibility = Visibility.Visible;
                        //flashGrid_B.Visibility = Visibility.Hidden;
                        movieGrid_B.Visibility = Visibility.Hidden;

                        HtmlBrowser_B_Container.Visibility = Visibility.Hidden;
                        //WEBGrid_B.Visibility = Visibility.Hidden;
                        //HtmlBrowser_B_1_Container.Visibility = Visibility.Hidden;
                        //HtmlBrowser_B_2_Container.Visibility = Visibility.Hidden;

                        string strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", _ListStructContents_B[_CurrentPlayNo_B].file_name);
                        BitmapImage bi = new BitmapImage(new Uri(strFileFullPath, UriKind.RelativeOrAbsolute));
                        bi.Freeze();
                        imagePlayer_B.Source = bi;
                    }
                    else
                    {
                        imagePlayer_B.Visibility = Visibility.Visible;
                        //flashGrid_B.Visibility = Visibility.Hidden;
                        movieGrid_B.Visibility = Visibility.Hidden;

                        HtmlBrowser_B_Container.Visibility = Visibility.Hidden;
                        //WEBGrid_B.Visibility = Visibility.Hidden;                       

                        string strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", _ListStructContents_B[_CurrentPlayNo_B].file_name);
                        BitmapImage bi = new BitmapImage(new Uri(strFileFullPath, UriKind.RelativeOrAbsolute));
                        bi.Freeze();
                        imagePlayer_B.Source = bi;
                    }

                    Timer_imageB.Interval = TimeSpan.FromSeconds(_Main.FnString2Int(_Main._StructDspConfig.duration));
                    Timer_imageB.Start();

                    // Live, Log
                    _Main._StructLive.file_name_B = _ListStructContents_B[_CurrentPlayNo_B].file_name;
                    _Main._StructLive.thumb_img_B = _ListStructContents_B[_CurrentPlayNo_B].c_idx + ".jpg";
                    _Main.FnPlayerLiveLog("B");
            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnImagePlay_B()");

                Timer_imageA.Stop();
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () { FnPlay_A(++_CurrentPlayNo_A); }));
            }
        }

        void Fn_Timer_ImageB_Tick(object sender, EventArgs e)
        {
            Timer_imageB.Stop();

            _CyclePlayCount_B++;
            FnPlay_B(++_CurrentPlayNo_B);
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------
        private void FnHtmlPlay_B()
        {
            //System.Windows.MessageBox.Show(WEBBrowser_A.Version.ToString());
            //Task.Factory.StartNew(() => { FnThreadHtmlBasync(); });
            FnThreadHtmlBasync();
        }
        private void FnThreadHtmlBasync()
        {
            try
            {
                string strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", _ListStructContents_B[_CurrentPlayNo_B].file_name);

                if (webView2Browser_B.Source == new Uri(strFileFullPath, UriKind.Absolute))
                    webView2Browser_B.Reload();
                else
                    webView2Browser_B.Source = new Uri(strFileFullPath, UriKind.Absolute);

                // Live, Log
                _Main._StructLive.file_name_B = _ListStructContents_B[_CurrentPlayNo_B].file_name;
                _Main._StructLive.thumb_img_B = _ListStructContents_B[_CurrentPlayNo_B].c_idx + ".jpg";
                _Main.FnPlayerLiveLog("B");
            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnThreadHtmlBasync()");

                //FnPlay_A(++_CurrentPlayNo_A);
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () { FnPlay_B(++_CurrentPlayNo_B); }));
            }
        }
        private async void WebView2Browser_B_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            HtmlBrowser_B_Container.Visibility = Visibility.Visible;
            //WEBGrid_B.Visibility = Visibility.Visible;
            webView2Browser_B.UpdateWindowPos();    // Parent Control (WEBGrid_A) Visible 이후 HTML 화면이 안나오는 경우를 피하기 위하여 추가함 
                                                    // WebView2 windows are blank when created in the background
                                                    //flashGrid_A.Visibility = Visibility.Hidden;
            imagePlayer_B.Visibility = Visibility.Hidden;
            movieGrid_B.Visibility = Visibility.Hidden;

            try
            {
                var result = await webView2Browser_B.CoreWebView2.ExecuteScriptAsync("play_time()");
                Timer_WEBB.Interval = TimeSpan.FromSeconds(int.Parse(result));
                Timer_WEBB.Start();
            }
            catch (Exception)
            {
                Timer_WEBB.Interval = TimeSpan.FromSeconds(_HtmlDispTime);
                Timer_WEBB.Start();
            }
        }
        private void WebView2Browser_B_ProcessFailed(object sender, Microsoft.Web.WebView2.Core.CoreWebView2ProcessFailedEventArgs e)
        {
            webView2Browser_B.Stop();

            _Main.FnQueueAdd(eQueueMode.StatusWrite, "Fn_MoviePlayer_B_MediaFailed()");

            FnPlay_B(++_CurrentPlayNo_B);
            //Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () { fnPlay_B(++_CurrentPlayNo_B); }));
        }
        void Fn_Timer_WEBB_Tick(object sender, EventArgs e)
        {
            Timer_WEBB.Stop();

            _CyclePlayCount_B++;
            //FnPlay_B(++_CurrentPlayNo_B);
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () { FnPlay_B(++_CurrentPlayNo_B); }));
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------
        #region Flash play in B area
        /*
        private void FnFlashPlay_B()
        {
            Task.Factory.StartNew(() => { FnThreadFlashB(); });
        }
        private void FnThreadFlashB()
        {
            try
            {
                string strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", _ListStructContents_B[_CurrentPlayNo_B].file_name);
                flashPlayer_B.LoadMovie(0, strFileFullPath);

                while (flashPlayer_B.PercentLoaded() < 100) ; // to remove a black screen flip when be changed between media grid. 

                flashPlayer_B.Play();
                flashPlayer_B.ScaleMode = 2;
                flashPlayer_B.Loop = false;
                flashPlayer_B.Menu = false;

                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                {
                    flashGrid_B.Visibility = Visibility.Visible;
                    movieGrid_B.Visibility = Visibility.Hidden;
                    imgPlayerB.Visibility = Visibility.Hidden;
                    WEBGrid_B.Visibility = Visibility.Hidden;
                   
                }));
                
                Timer_FlashB.Start();

                // Live, Log
                _Main._StructLive.file_name_B = _ListStructContents_B[_CurrentPlayNo_B].file_name;
                _Main._StructLive.thumb_img_B = _ListStructContents_B[_CurrentPlayNo_B].c_idx + ".jpg";
                _Main.FnPlayerLiveLog("B");
            }
            catch (Exception)
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlay_B(++_CurrentPlayNo_B); }));
            }
        }
        void Timer_FlashB_Tick(object sender, EventArgs e)
        {
            if (!flashPlayer_B.Playing)
            {
                Timer_FlashB.Stop();
                FnFlashStop(flashPlayer_B);

                _CyclePlayCount_B++;
                FnPlay_B(++_CurrentPlayNo_B);
            }
        }
        */
        #endregion

        //------------------------------------------------------------------------------------------------------------------------------------------------------
        private void FnPPTPlay_B()
        {
            imagePlayer_B.Visibility = Visibility.Hidden;
            //flashGrid_B.Visibility = Visibility.Hidden;
            movieGrid_B.Visibility = Visibility.Hidden;

            WEBGrid_B.Visibility = Visibility.Hidden;
            HtmlBrowser_B_Container.Visibility = Visibility.Hidden;

            Task.Factory.StartNew(() => { FnThreadPPT(); });
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------
        private void FnDTVPlay_B()
        {
            imagePlayer_B.Visibility = Visibility.Hidden;
            //flashGrid_B.Visibility = Visibility.Hidden;
            movieGrid_B.Visibility = Visibility.Hidden;

            WEBGrid_B.Visibility = Visibility.Hidden;
            HtmlBrowser_B_Container.Visibility = Visibility.Hidden;

            Task.Factory.StartNew(() => { FnThreadDTV(); });
        }

        #endregion

        #region PPT Play
        private void FnThreadPPT()
        {
            try
            {
                FnPlayerTopMost();  // TopMost

                int nLeft, nTop, nWidth, nHeight;
                string strFileFullPath = string.Empty;
                switch (_PptPlayLayout)
                {
                    case "B":
                        strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", _ListStructContents_B[_CurrentPlayNo_B].file_name);
                        nLeft = _PlayLeft_B;
                        nTop = _PlayTop_B;
                        nWidth = _PlayWidth_B;
                        nHeight = _PlayHeight_B;
                        break;
                    default:
                        strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", _ListStructContents_A[_CurrentPlayNo_A].file_name);
                        nLeft = _PlayLeft_A;
                        nTop = _PlayTop_A;
                        nWidth = _PlayWidth_A;
                        nHeight = _PlayHeight_A;
                        break;
                }
                
                string strPptViewerPath = Path.Combine(_Main.FnGetProgramFilesX86FolderName(), "Microsoft Office", "Office14", "PPTVIEW.EXE");
                ProcessStartInfo psi = new ProcessStartInfo(strPptViewerPath, strFileFullPath);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                Process p = Process.Start(psi);
                p.WaitForInputIdle();
                p.Close();
                                             
                IntPtr handle = IntPtr.Zero;
                for (int i = 0; i < 30; i++)
                {
                    handle = Win32API.FindWindow("PPTFrameClass", null);
                    handle = Win32API.FindWindowEx(handle, IntPtr.Zero, "MDIClient", null);
                    handle = Win32API.FindWindowEx(handle, IntPtr.Zero, "mdiClass", null);
                    handle = Win32API.FindWindowEx(handle, IntPtr.Zero, "paneClassDC", null);
                    if (!handle.Equals(IntPtr.Zero)) break;
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
                if (handle.Equals(IntPtr.Zero)) throw new Exception();

                Win32API.SetParent(handle, IntPtr.Zero); // Root로 이동
                IntPtr handle1 = Win32API.FindWindow("paneClassDC", null); // Find
                Win32API.MoveWindow(handle1, nLeft, nTop, nWidth, nHeight, true); // ReSize
                Win32API.SetParent(handle1, _PlayerHandle); // Paste

                IntPtr handle2 = Win32API.FindWindowEx(_PlayerHandle, IntPtr.Zero, "paneClassDC", null);
                if (!handle1.Equals(handle2)) throw new Exception();

                
                string strHookExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "whk.exe");
                string strHookDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "whklib.dll");
                psi = new ProcessStartInfo(strHookExePath, strHookDllPath);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                p = Process.Start(psi);
                p.Close();
                
                //*********************************************************************************************
                Timer_PPT.Interval = TimeSpan.FromSeconds(_Main.FnString2Int(_Main._StructDspConfig.duration));
                Timer_PPT.Start();
                //*********************************************************************************************

                // Live, Log
                switch (_PptPlayLayout)
                {
                    case "B":
                        _Main._StructLive.file_name_B = _ListStructContents_B[_CurrentPlayNo_B].file_name;
                        _Main._StructLive.thumb_img_B = _ListStructContents_B[_CurrentPlayNo_B].c_idx + ".jpg";
                        _Main.FnPlayerLiveLog("B");
                        break;

                    default:
                        _Main._StructLive.file_name_A = _ListStructContents_A[_CurrentPlayNo_A].file_name;
                        _Main._StructLive.thumb_img_A = _ListStructContents_A[_CurrentPlayNo_A].c_idx + ".jpg";
                        _Main.FnPlayerLiveLog("A");
                        break;
                }
            }
            catch (Exception)
            {
                _Main.FnProcessKill("PPTVIEW");
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnPPTProc()");
                switch (_PptPlayLayout)
                {
                    case "B":
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlay_B(++_CurrentPlayNo_B); }));
                        break;

                    default:
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlay_A(++_CurrentPlayNo_A); }));
                        break;
                }
            }
        }

        //******************************************************************************************************************
        void Fn_Timer_PPT_Tick(object sender, EventArgs e)
        {
            Timer_PPT.Stop();
            _Main.FnProcessKill("PPTVIEW");

            switch (_PptPlayLayout)
            {
                case "B":
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlay_B(++_CurrentPlayNo_B); }));
                    break;

                default:
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlay_A(++_CurrentPlayNo_A); }));
                    break;
            }        
        }
        //******************************************************************************************************************

        #endregion

        #region DTV Play
        private void FnThreadDTV()
        {
            try
            {
                FnPlayerTopMost();  // TopMost

                int nLeft, nTop, nWidth, nHeight;
                string strFileFullPath = string.Empty;

                switch (_DtvPlayLayout)
                {
                    case "B":
                        nLeft = _PlayLeft_B;
                        nTop = _PlayTop_B;
                        nWidth = _PlayWidth_B;
                        nHeight = _PlayHeight_B;
                        break;

                    default:
                        nLeft = _PlayLeft_A;
                        nTop = _PlayTop_A;
                        nWidth = _PlayWidth_A;
                        nHeight = _PlayHeight_A;
                        break;
                }

                string strPptViewerPath = Path.Combine(_Main.FnGetProgramFilesX86FolderName(), "SKY Capture", "SKYTV HD Yellow", "CapApplication.EXE");
                // SKY HD Yellow 를 사용하는 경우 --> default

                //string strPptViewerPath = Path.Combine(_Main.FnGetProgramFilesX86FolderName(), "SKY Capture", "SKYTV HD Magenta", "CapApplication.EXE");
                // SKY HD Magenta 를 사용하는 경우 --> KB손해보험 

                ProcessStartInfo psi = new ProcessStartInfo(strPptViewerPath);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                Process p = Process.Start(psi);
                p.WaitForInputIdle();
                p.Close();

                IntPtr handle = IntPtr.Zero;
                for (int i = 0; i < 30; i++)
                {
                    handle = Win32API.FindWindow("GrpFrameClass", null);
                    if (!handle.Equals(IntPtr.Zero)) break;
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
                if (handle.Equals(IntPtr.Zero)) throw new Exception();

                Win32API.MoveWindow(handle, nLeft, nTop, nWidth, nHeight, true); // ReSize
                Win32API.SetParent(handle, _PlayerHandle); // Paste

                IntPtr handle2 = Win32API.FindWindowEx(_PlayerHandle, IntPtr.Zero, "GrpFrameClass", null);
                if (!handle.Equals(handle2)) throw new Exception();

                // Live, Log
                switch (_DtvPlayLayout)
                {
                    case "B":
                        _Main._StructLive.file_name_B = _ListStructContents_B[_CurrentPlayNo_B].file_name;
                        _Main._StructLive.thumb_img_B = _ListStructContents_B[_CurrentPlayNo_B].c_idx + ".jpg";
                        _Main.FnPlayerLiveLog("B");
                        break;

                    default:
                        _Main._StructLive.file_name_A = _ListStructContents_A[_CurrentPlayNo_A].file_name;
                        _Main._StructLive.thumb_img_A = _ListStructContents_A[_CurrentPlayNo_A].c_idx + ".jpg";
                        _Main.FnPlayerLiveLog("A");
                        break;
                }

            }
            catch (Exception)
            {
                _Main.FnProcessKill("CAPAPPLICATION");
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnDTVProc()");
                switch (_DtvPlayLayout)
                {
                    case "B":
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlay_B(++_CurrentPlayNo_B); }));
                        break;

                    default:
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnPlay_A(++_CurrentPlayNo_A); }));
                        break;
                }

            }
        }
        #endregion

        #region BGM Play
        private void FnBgmPlay(int p)
        {
            try
            {
                _CurrentBgmNo = p;
                if (_CurrentBgmNo >= _ListStructBGM.Count)
                {
                    if (_CyclePlayCount_BGM.Equals(0)) return;
                    _CyclePlayCount_BGM = 0;
                    FnBgmPlay(0);
                    return;
                }

                Task.Factory.StartNew(() => { FnThreadBgmStart(); });

                //---------2017. 11.7 by John------------
                _OnExecution_BGM = true;
                //---------------------------------------
            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnBgmPlay()");

                FnBgmPlay(++_CurrentBgmNo);
            }
        }

        private void FnThreadBgmStart()
        {
            try
            {
                string strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", _ListStructBGM[_CurrentBgmNo]);

                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                {
                    _BGMplayer.MediaEnded += Fn_BGMplayer_MediaEnded;
                    _BGMplayer.MediaFailed += Fn_BGMplayer_MediaFailed;
                    _BGMplayer.Volume = (double)_Main.FnString2Int(_Main._StructDspConfig.volume) / 31;
                    _BGMplayer.Open(new Uri(strFileFullPath)); // When set the Source, It will be played automatically
                    _BGMplayer.Play();
                }));
            }
            catch (Exception)
            {
                //FnMovieStop_A();
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnBgmPlay(++_CurrentBgmNo); }));
            }

        }
       
        private void Fn_BGMplayer_MediaEnded(object sender, EventArgs e)
        {
             try
            {
                _CyclePlayCount_BGM++;
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnBgmPlay(++_CurrentBgmNo); }));
            }
            catch (Exception) 
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnBgmPlay(++_CurrentBgmNo); }));
            }
        }

        private void Fn_BGMplayer_MediaFailed(object sender, EventArgs e)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnBgmPlay(++_CurrentBgmNo); }));
        }

        private void FnBgmStop()
        {
            try
            {
                lock (this)
                {
                    _BGMplayer.Stop();
                    _BGMplayer.Close();
                    
                    _OnExecution_BGM = false;
                }
            }
            catch (Exception) { }
        }
        #endregion

        #region Kiosk Play
        private void FnThreadKiosk()
        {
            try
            {
                FnKioskPlayProc(eKioskPlayStatus.Play);

                if (_StructKiosk.dblTime > 0)
                {
                    Timer_Kiosk.Interval = TimeSpan.FromMinutes(_StructKiosk.dblTime);
                    FnKioskTimerStopStart();
                }

                _StructKiosk.first_execute = true;
            }
            catch (Exception)
            {
                _Main.FnPlayerErrorProc("Kiosk 컨텐츠 재생오류");
            }
        }
        public void FnKioskTimerStopStart()
        {
            Timer_Kiosk.Stop();
            Timer_Kiosk.Start();
        }
        // 광고재생시작
        void Fn_Timer_Kiosk_Tick(object sender, EventArgs e)
        {
            Timer_Kiosk.Stop();
            _StructKiosk.play = false;

            FnKioskPlayProc(eKioskPlayStatus.Pause);
            switch (_StructKiosk.layout)
            {
                case "B":
                    FnPlay_B(0);
                    break;

                default:
                    FnPlay_A(0);
                    break;
            }
        }
        // 터치 -> 키오스크 재생
        public void FnKioskReplay()
        {
            FnKioskTimerStopStart();
            FnKioskAdsStop();
            FnKioskPlayProc(eKioskPlayStatus.Play);
        }
        private void FnKioskAdsStop()
        {
            try
            {
                switch (_StructKiosk.layout)
                {
                    case "B":
                        FnStopB();
                        break;

                    default:
                        FnStopA();
                        break;
                }

                // PPT
                switch (_StructKiosk.mode)
                {
                    case ePlayMode.Exe:
                    case ePlayMode.Html:
                    case ePlayMode.Flash:
                        _Main.FnProcessKill("PPTView");
                        break;

                    default:
                        break;
                }
            }
            catch (Exception) { }
        }
        private void FnKioskPlayProc(eKioskPlayStatus _eKioskPlayStatus)
        {
            try
            {
                if (!_StructKiosk.use) return;

                if (_eKioskPlayStatus.Equals(eKioskPlayStatus.Pause))
                {
                    switch (_StructKiosk.mode)
                    {
                        case ePlayMode.Powerpoint:
                        case ePlayMode.Exe:
                            Win32API.ShowWindow(_StructKiosk.handle, 0);
                            break;

                        case ePlayMode.Flash:
                        case ePlayMode.Html:
                            kioskGrid.Visibility = Visibility.Collapsed;
                            break;

                        default:
                            break;
                    }
                    Win32API.ShowCursor(false); // 마우스커서 숨김
                }
                else if (_eKioskPlayStatus.Equals(eKioskPlayStatus.Play))
                {
                    switch (_StructKiosk.mode)
                    {
                        case ePlayMode.Powerpoint:
                            FnKioskPPTPlay();
                            break;

                        case ePlayMode.Exe:
                            FnKioskExePlay();
                            break;

                        case ePlayMode.Flash:
                            //Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnKioskFlashPlay(); }));
                            break;

                        case ePlayMode.Html:
                            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { FnKioskWebPlay(); }));
                            break;

                        default:
                            break;
                    }

                    // Live, Log
                    switch (_StructKiosk.layout)
                    {
                        case "B":
                            _Main._StructLive.file_name_B = (_StructKiosk.mode.Equals(ePlayMode.Html)) ? _StructKiosk.file_name : Path.GetFileName(_StructKiosk.file_name);
                            _Main._StructLive.thumb_img_B = "-4.jpg";
                            _Main.FnPlayerLiveLog("B");
                            break;

                        default:
                            _Main._StructLive.file_name_A = (_StructKiosk.mode.Equals(ePlayMode.Html)) ? _StructKiosk.file_name : Path.GetFileName(_StructKiosk.file_name);
                            _Main._StructLive.thumb_img_A = "-4.jpg";
                            _Main.FnPlayerLiveLog("A");
                            break;
                    }

                    _StructKiosk.play = true;
                }
                else if (_eKioskPlayStatus.Equals(eKioskPlayStatus.Stop))
                {
                    if (Timer_Kiosk != null) Timer_Kiosk.Stop();
                    switch (_StructKiosk.mode)
                    {
                        case ePlayMode.Powerpoint:
                            _Main.FnProcessKill("PPTView");
                            break;

                        case ePlayMode.Flash:
                            //FnFlashStop(flashPlayer_Kiosk);
                            //kioskGrid.Visibility = Visibility.Collapsed;
                            break;

                        case ePlayMode.Exe:
                            _Main.FnProcessKill(Path.GetFileNameWithoutExtension(_StructKiosk.file_name));
                            break;

                        case ePlayMode.Html:
                            kioskGrid.Visibility = Visibility.Collapsed;
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception)
            {
                _Main.FnPlayerErrorProc("Kiosk 컨텐츠 오류");
            }
        }
        /*
        private void FnKioskFlashPlay()
        {
            if (_StructKiosk.first_execute)
            {
                kioskGrid.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                kioskGrid = new Grid();
                WindowsFormsHost host = new WindowsFormsHost();
                host.Child = flashPlayer_Kiosk;
                kioskGrid.Children.Add(host);
                switch (_StructKiosk.layout)
                {
                    case "B":
                        playGrid_B.Children.Add(kioskGrid);
                        break;
                    default:
                        playGrid_A.Children.Add(kioskGrid);
                        break;
                }
                kioskGrid.Visibility = Visibility.Visible;

                string strLocalKioskPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kiosk", Path.GetFileName(_StructKiosk.file_name));
                flashPlayer_Kiosk.LoadMovie(0, strLocalKioskPath);
                flashPlayer_Kiosk.Play();
                flashPlayer_Kiosk.ScaleMode = 2;
                flashPlayer_Kiosk.Loop = false;
                flashPlayer_Kiosk.Menu = false;
            }
        }
        */
        private void FnKioskPPTPlay()
        {
            if (_StructKiosk.first_execute)
            {
                Win32API.ShowWindow(_StructKiosk.handle, 1);
            }
            else
            {
                try
                {
                    FnPlayerTopMost();  // TopMost

                    int nLeft, nTop, nWidth, nHeight;
                    switch (_StructKiosk.layout)
                    {
                        case "B":
                            nLeft = _PlayLeft_B;
                            nTop = _PlayTop_B;
                            nWidth = _PlayWidth_B;
                            nHeight = _PlayHeight_B;
                            break;

                        default:
                            nLeft = _PlayLeft_A;
                            nTop = _PlayTop_A;
                            nWidth = _PlayWidth_A;
                            nHeight = _PlayHeight_A;
                            break;
                    }

                    string strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kiosk", Path.GetFileName(_StructKiosk.file_name));
                    string strPptViewerPath = Path.Combine(_Main.FnGetProgramFilesX86FolderName(), "Microsoft Office", "Office14", "PPTVIEW.EXE");
                    ProcessStartInfo psi = new ProcessStartInfo(strPptViewerPath, "/f /s " + strFileFullPath);
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    Process p = Process.Start(psi);
                    p.WaitForInputIdle();
                    p.Close();

                    IntPtr handle = IntPtr.Zero;
                    for (int i = 0; i < 10; i++)
                    {
                        handle = Win32API.FindWindow("screenClass", null);
                        if (!handle.Equals(IntPtr.Zero)) break;
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                    if (handle.Equals(IntPtr.Zero)) throw new Exception();

                    Win32API.SetParent(handle, IntPtr.Zero); // Root로 이동
                    IntPtr handle1 = Win32API.FindWindow("screenClass", null); // Find
                    Win32API.MoveWindow(handle1, nLeft, nTop, nWidth, nHeight, true);
                    Win32API.SetParent(handle1, _PlayerHandle);

                    IntPtr handle2 = Win32API.FindWindowEx(_PlayerHandle, IntPtr.Zero, "screenClass", null);
                    if (!handle1.Equals(handle2)) throw new Exception();

                    _StructKiosk.handle = handle2;
                }
                catch (Exception)
                {
                    _Main.FnProcessKill("PPTVIEW");
                    _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnKioskPPTPlay()");
                    _Main.FnPlayerErrorProc("Kiosk 컨텐츠 오류");
                }
            }
        }
        private void FnKioskExePlay()
        {
            if (_StructKiosk.first_execute)
            {
                Win32API.ShowWindow(_StructKiosk.handle, 1);
            }
            else
            {
                FnPlayerTopMost();  // TopMost

                try
                {
                    int nLeft, nTop, nWidth, nHeight;
                    switch (_StructKiosk.layout)
                    {
                        case "B":
                            nLeft = _PlayLeft_B;
                            nTop = _PlayTop_B;
                            nWidth = _PlayWidth_B;
                            nHeight = _PlayHeight_B;
                            break;

                        default:
                            nLeft = _PlayLeft_A;
                            nTop = _PlayTop_A;
                            nWidth = _PlayWidth_A;
                            nHeight = _PlayHeight_A;
                            break;
                    }

                    string strLocalKioskPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kiosk", Path.GetFileName(_StructKiosk.file_name));
                    ProcessStartInfo psi = new ProcessStartInfo(strLocalKioskPath);
                    psi.WindowStyle = ProcessWindowStyle.Hidden;
                    psi.CreateNoWindow = true;
                    Process p = Process.Start(psi);
                    p.WaitForInputIdle();
                    p.Close();

                    string strFileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(strLocalKioskPath);
                    if (strFileNameWithoutExtension.Contains("[")) strFileNameWithoutExtension = strFileNameWithoutExtension.Split('[')[0];

                    IntPtr handle = IntPtr.Zero;
                    for (int i = 0; i < 10; i++)
                    {
                        handle = Win32API.FindWindow(null, strFileNameWithoutExtension);
                        if (!handle.Equals(IntPtr.Zero)) break;
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                    if (handle.Equals(IntPtr.Zero)) throw new Exception();

                    Win32API.MoveWindow(handle, nLeft, nTop, nWidth, nHeight, true);
                    Win32API.SetParent(handle, _PlayerHandle);

                    IntPtr nHandle2 = Win32API.FindWindowEx(_PlayerHandle, IntPtr.Zero, null, strFileNameWithoutExtension);
                    if (!nHandle2.Equals(handle)) throw new Exception();

                    _StructKiosk.handle = nHandle2;
                }
                catch (Exception)
                {
                    _Main.FnProcessKill(Path.GetFileNameWithoutExtension(_StructKiosk.file_name));
                    _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnKioskExePlay()");
                    _Main.FnPlayerErrorProc("Kiosk 컨텐츠 오류");
                }
            }
        }
        private void FnKioskWebPlay()
        {
            try
            {
                if (_StructKiosk.first_execute)
                {
                    kioskGrid.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    System.Windows.Controls.WebBrowser webBrowser = new System.Windows.Controls.WebBrowser();
                    kioskGrid = new Grid();
                    kioskGrid.Children.Add(webBrowser);
                    switch (_StructKiosk.layout)
                    {
                        case "B":
                            playGrid_B.Children.Add(kioskGrid);
                            break;

                        default:
                            playGrid_A.Children.Add(kioskGrid);
                            break;
                    }
                    kioskGrid.Visibility = Visibility.Visible;
                    webBrowser.Navigate(new Uri(_StructKiosk.file_name));
                }
            }
            catch (Exception)
            {
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnKioskWebPlay()");
                _Main.FnPlayerErrorProc("Kiosk 컨텐츠 오류");
            }
        }
        #endregion

        #region Ticker
        private void FnTickerPlay()
        {
            try
            {
                if (!_TickerUse ||
                    _StructConfig.package.Equals("t0") ||
                    _StructConfig.package.Equals("f1")) return;

                FnTickerSize();
                FnTickerFlowPlay();
            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(Convert.ToString(ex));
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnTickerPlay()");
            }
        }
        private void FnTickerSize()
        {
            switch (_StructConfig.package)
            {
                case "e0":
                case "e1":
                case "e2":
                    ticker_canvas.Visibility = Visibility.Visible;
                    ticker_canvas.HorizontalAlignment = templete_ticker.HorizontalAlignment;
                    ticker_canvas.VerticalAlignment = templete_ticker.VerticalAlignment;
                    ticker_canvas.Margin = templete_ticker.Margin;
                    ticker_canvas.Width = templete_ticker.Width;
                    ticker_canvas.Height = templete_ticker.Height;
                    ticker_canvas.Background = System.Windows.Media.Brushes.Transparent;
                    //ticker_textBlock.Foreground = Brushes.White;
                    ticker_textBlock.Foreground = FnGetColor(_Main._StructDspConfig.ticker_font_color);
                    ticker_textBlock.FontSize = 60;
                    ticker_textBlock.FontFamily = new System.Windows.Media.FontFamily("맑은고딕");
                    break;

                case "e4":
                    ticker_canvas.Visibility = Visibility.Visible;
                    ticker_canvas.HorizontalAlignment = e4_templete_ticker.HorizontalAlignment;
                    ticker_canvas.VerticalAlignment = e4_templete_ticker.VerticalAlignment;
                    ticker_canvas.Margin = e4_templete_ticker.Margin;
                    ticker_canvas.Width = e4_templete_ticker.Width;
                    ticker_canvas.Height = e4_templete_ticker.Height;
                    ticker_canvas.Background = System.Windows.Media.Brushes.Transparent;
                    //ticker_textBlock.Foreground = Brushes.White;
                    ticker_textBlock.Foreground = FnGetColor(_Main._StructDspConfig.ticker_font_color);
                    ticker_textBlock.FontSize = 60;
                    ticker_textBlock.FontFamily = new System.Windows.Media.FontFamily("맑은고딕");
                    break;

                case "f0":
                    fade_canvas.Visibility = Visibility.Visible;
                    fade_canvas.HorizontalAlignment = f0_templete_news_ticker.HorizontalAlignment;
                    fade_canvas.VerticalAlignment = f0_templete_news_ticker.VerticalAlignment;
                    fade_canvas.Margin = f0_templete_news_ticker.Margin;
                    fade_canvas.Width = f0_templete_news_ticker.Width;
                    fade_canvas.Height = f0_templete_news_ticker.Height;
                    fade_canvas.Background = System.Windows.Media.Brushes.Transparent;
                    //ticker_textBlock.Foreground = Brushes.White;
                    fade_textBlock.Foreground = FnGetColor(_Main._StructDspConfig.ticker_font_color);
                    fade_textBlock.FontSize = 18;
                    fade_textBlock.FontFamily = new System.Windows.Media.FontFamily("맑은고딕");
                    fade_textBlock.Width = f0_templete_news_ticker.Width;
                    Canvas.SetTop(fade_textBlock, 10); // canvas 내의 top 으로 부터의 15 거리에 textblock 위치 
                    break;

                default: //a0, b0, b1, b2
                    ticker_canvas.Visibility = Visibility.Visible;
                    ticker_canvas.VerticalAlignment = VerticalAlignment.Bottom;
                    ticker_canvas.Margin = new Thickness(0, 0, 0, 0);
                    ticker_canvas.Width = rootGrid.Width;
                    ticker_canvas.Height = _DoubleTickerHeight;
                    ticker_canvas.Background = FnGetColor(_Main._StructDspConfig.ticker_bg_color);
                    ticker_textBlock.Foreground = FnGetColor(_Main._StructDspConfig.ticker_font_color);
                    ticker_textBlock.FontSize = _DoubleTickerFontSize;
                    ticker_textBlock.FontFamily = new System.Windows.Media.FontFamily("NanumSquare");
                    if (_Main._StructDspConfig.ticker_font_size == "big")
                        Canvas.SetTop(ticker_textBlock, 33); // canvas 내의 top 으로 부터의 13 거리에 textblock 위치 
                    else if (_Main._StructDspConfig.ticker_font_size == "noemal")
                        Canvas.SetTop(ticker_textBlock, 33 * 0.8); // canvas 내의 top 으로 부터의 13 거리에 textblock 위치 
                    else if (_Main._StructDspConfig.ticker_font_size == "small")
                        Canvas.SetTop(ticker_textBlock, 33 * 0.6); // canvas 내의 top 으로 부터의 13 거리에 textblock 위치 
                    else
                        Canvas.SetTop(ticker_textBlock, 33 * 0.8); // canvas 내의 top 으로 부터의 13 거리에 textblock 위치 
                    break;
            }
        }        
        private void FnTickerFlowPlay()
        {
            switch (_StructConfig.package)
            {
                case "a0":
                case "b0":
                case "b1":
                case "b2":
                case "e0":
                case "e1":
                case "e2":
                case "e4":            
                    
                    string strTickerText = string.Empty;

                    //-------------------------------------------------------------------------------------------------------------
                    if (_StructConfig.ticker_use.Equals("1")) // 스케쥴 자막
                    {
                        foreach (string strLine in _StructConfig.ticker_msg.Split(new string[] { "\r\n" }, StringSplitOptions.None))
                        {
                            if (string.IsNullOrEmpty(strLine.Trim())) continue;
                            strTickerText += "▷" + strLine.Trim() + "          ";
                        }
                    }

                    //-------------------------------------------------------------------------------------------------------------
                    if (_Main._StructDspConfig.ticker_rss_use.Equals("1")) // RSS/Mpartners news
                    {
                        // default news is Mpartners news
                        if (string.Equals(_Main._StructDspConfig.ticker_rss_url, "default.xml"))
                        {
                            try
                            {                                
                                string loadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "data_mnews_all.txt");
                                string weather_file_string = System.IO.File.ReadAllText(loadPath, Encoding.UTF8);
                                char[] weather_list_delimiter_chars = { '|' };

                                foreach (string strLine in weather_file_string.Split(weather_list_delimiter_chars, StringSplitOptions.None))
                                {
                                    if (string.IsNullOrEmpty(strLine.Trim())) continue;
                                    strTickerText += "▷" + strLine.Trim() + "          ";
                                }
                            }
                            catch (Exception)
                            {

                            }
                        }
                        else  // Mpartners news
                        {
                            try
                            {
                                string loadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "rss_news_all.txt");
                                string weather_file_string = System.IO.File.ReadAllText(loadPath, Encoding.UTF8);
                                char[] weather_list_delimiter_chars = { '|' };

                                foreach (string strLine in weather_file_string.Split(weather_list_delimiter_chars, StringSplitOptions.None))
                                {
                                    if (string.IsNullOrEmpty(strLine.Trim())) continue;
                                    strTickerText += "▷" + strLine.Trim() + "          ";
                                }
                                                                
                                /*
                                string strRssXmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "rss_news_all.txt");
                                if (!File.Exists(strRssXmlPath)) return;

                                XmlDocument xRssDoc = new XmlDocument();
                                xRssDoc.Load(strRssXmlPath);

                                XmlNode nodeRss = xRssDoc.SelectSingleNode("rss");
                                XmlNode nodeChannel = nodeRss.SelectSingleNode("channel");

                                foreach (XmlNode xn in nodeChannel.SelectNodes("item"))
                                {
                                    strTickerText += ("▷" + _Main.FnXmlNodeCheck(xn.SelectSingleNode("title")) + "          ");

                                    cnt++;
                                    if (cnt >= 50) break;   //strTickerText 의 크기가 작으면 ticker 의 동작이 부드럽게 보인다.
                                }
                                xRssDoc = null;
                                */
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }

                    //-------------------------------------------------------------------------------------------------------------
                    if (strTickerText != string.Empty)
                    {
                        try
                        {
                            ticker_textBlock.Text = strTickerText.Trim();

                            double equSlope = 0;
                            switch (_Main._StructDspConfig.ticker_speed)
                            {
                                case "slow":
                                    equSlope = 0.015;
                                    break;
                                case "normal":
                                    equSlope = 0.010;
                                    break;
                                case "quick":
                                    equSlope = 0.005;
                                    break;
                                default:
                                    equSlope = 0.010;
                                    break;
                            }

                            CultureInfo cultureInfo = CultureInfo.GetCultureInfo("ko");
                            Typeface fontTF = new Typeface(ticker_textBlock.FontFamily, ticker_textBlock.FontStyle, ticker_textBlock.FontWeight, ticker_textBlock.FontStretch);
                            
                            FormattedText frmmtText = new FormattedText(strTickerText, cultureInfo, System.Windows.FlowDirection.LeftToRight, fontTF, ticker_textBlock.FontSize, ticker_textBlock.Foreground, VisualTreeHelper.GetDpi(this).PixelsPerDip);                                                          
                                                       
                            double stringSize = frmmtText.Width;
                            double pixelXFactor = (stringSize < 100) ? 1.02 : 1.01;
                            double offSetY = 10.96286472;
                            double textBoxWidth = stringSize * pixelXFactor;
                            double negXOffSet = textBoxWidth * -1;
                            double fromSecValue = (stringSize * equSlope) + offSetY;
                            ticker_textBlock.Width = textBoxWidth;

                            Duration durX = new Duration(TimeSpan.FromSeconds(fromSecValue));
                            DoubleAnimation daX = new DoubleAnimation(ticker_canvas.Width, negXOffSet, durX)
                            {
                                RepeatBehavior = RepeatBehavior.Forever
                            };

                            Storyboard.SetTargetName(daX, "rtTTransform");
                            Storyboard.SetTargetProperty(daX, new PropertyPath(TranslateTransform.XProperty));

                            storyboard = new Storyboard();
                            storyboard.Children.Add(daX);
                            storyboard.Begin(ticker_textBlock, true);
                        }
                        catch (Exception)
                        {
                            storyboard.Stop(ticker_textBlock);
                            storyboard.Remove(ticker_textBlock);
                            storyboard = null;

                            //_Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnTickerFlowPlay()");
                            //System.Windows.MessageBox.Show(Convert.ToString(ex));
                        }
                    }
                    
                    break;

                case "f0":                

                    //-------------------------------------------------------------------------------------------------------------
                    // 사용자가 직접 입력한 자막
                    if (_StructConfig.ticker_use.Equals("1")) // 스케쥴 자막
                    {
                        foreach (string strLine in _StructConfig.ticker_msg.Split(new string[] { "\r\n" }, StringSplitOptions.None))
                        {
                            if (string.IsNullOrEmpty(strLine.Trim())) continue;
                            tempTickerText.Add(strLine.Trim());
                        }
                    }

                    //-------------------------------------------------------------------------------------------------------------
                    // 뉴스 RSS/엠파트너스 
                    if (_Main._StructDspConfig.ticker_rss_use.Equals("1")) 
                    {
                           
                        // default news is Mpartners news
                        if (string.Equals(_Main._StructDspConfig.ticker_rss_url, "default.xml"))
                        {
                            try
                            {
                                string loadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "data_mnews_all.txt");
                                string weather_file_string = System.IO.File.ReadAllText(loadPath, Encoding.UTF8);
                                char[] weather_list_delimiter_chars = { '|' };

                                foreach (string strLine in weather_file_string.Split(weather_list_delimiter_chars, StringSplitOptions.None))
                                {
                                    if (string.IsNullOrEmpty(strLine.Trim())) continue;
                                    tempTickerText.Add(strLine.Trim());
                                }
                            }
                            catch (Exception)
                            {

                            }
                        }
                        else  // 고객이 입력한 RSS URL, 미리 연동 테스트가 필요함
                        {
                            try
                            {                                   
                                string loadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "data_mnews_all.txt");
                                string weather_file_string = System.IO.File.ReadAllText(loadPath, Encoding.UTF8);
                                char[] weather_list_delimiter_chars = { '|' };

                                foreach (string strLine in weather_file_string.Split(weather_list_delimiter_chars, StringSplitOptions.None))
                                {
                                    if (string.IsNullOrEmpty(strLine.Trim())) continue;
                                    tempTickerText.Add(strLine.Trim());
                                }                         
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }

                    //-------------------------------------------------------------------------------------------------------------
                    if (tempTickerText.Count != 0)
                    {
                        try
                        {
                            if (fade_storyboard == null)
                            {

                                listTickerText = tempTickerText;
                                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function : FnTickerFlowPlay()_then");

                                /*
                                double fromSecFadeIn = 0.0;
                                double toSecFadeIn = 1.0;
                                double dursecFadeIn = 2;                            
                                Duration durFadeIn = new Duration(TimeSpan.FromSeconds(dursecFadeIn));
                                DoubleAnimation daFadeIn = new DoubleAnimation(fromSecFadeIn, toSecFadeIn, durFadeIn, FillBehavior.HoldEnd);
                                Storyboard.SetTargetName(daFadeIn, "fade_textBlock");
                                Storyboard.SetTargetProperty(daFadeIn, new PropertyPath(TextBlock.OpacityProperty));
                                */

                                double fromSecFadeOut = 1.0;
                                double toSecFadeOut = 0.0;
                                double dursecFadeOut = 2.0;
                                double begintimeFadeOut = 4;
                                Duration durFadeOut = new Duration(TimeSpan.FromSeconds(dursecFadeOut));
                                //DoubleAnimation daFadeOut = new DoubleAnimation(fromSecFadeOut, toSecFadeOut, durFadeOut, FillBehavior.HoldEnd);
                                DoubleAnimation daFadeOut = new DoubleAnimation(fromSecFadeOut, toSecFadeOut, durFadeOut, FillBehavior.Stop)
                                {
                                    BeginTime = TimeSpan.FromSeconds(begintimeFadeOut)
                                };
                                Storyboard.SetTargetName(daFadeOut, "fade_textBlock");
                                Storyboard.SetTargetProperty(daFadeOut, new PropertyPath(TextBlock.OpacityProperty));

                                fade_storyboard = new Storyboard();
                                //fade_storyboard.Children.Add(daFadeIn);
                                fade_storyboard.Children.Add(daFadeOut);
                                fade_storyboard.Completed += new EventHandler(Fn_Fade_Storyboard_Completed);
                                //fade_storyboard.Completed += (sender, fade_storyboarde) => Fade_Storyboard_Completed(sender, fade_storyboard);

                                fade_textBlock.Opacity = 1.0;
                                fade_textBlock.Text = listTickerText[0];
                                listcounter = 1;

                                fade_storyboard.Begin(fade_textBlock, true);                                
                            }
                            else
                            {
                                fade_storyboard.Pause(fade_textBlock);
                                
                                listTickerText = tempTickerText;
                                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function : FnTickerFlowPlay()_else");                           

                                fade_storyboard.Resume(fade_textBlock);
                            }
                        }
                        catch (Exception)
                        {
                            fade_storyboard.Stop(fade_textBlock);
                            fade_storyboard.Remove(fade_textBlock);
                            fade_storyboard = null;

                            //_Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnTickerFlowPlay()");
                            //System.Windows.MessageBox.Show(Convert.ToString(ex));
                        }
                    }
                    else
                    {
                        _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function  : FnTickerFlowPlay()_data");
                    }

                    break;

                default:
                    break;
            }  //switch (_StructConfig.package)
        }
        //---------------------------------------------------------------------------------
        // stroryboard completed eventhandler method
        //private void Fade_Storyboard_Completed(object sender, Storyboard fade_storyboard)
        private void Fn_Fade_Storyboard_Completed(object sender, EventArgs e)
        {
            fade_textBlock.Opacity = 1.0;
            fade_textBlock.Text = listTickerText[listcounter % listTickerText.Count];
            listcounter++;

            fade_storyboard.Begin(fade_textBlock, true);            
        }

        //--------------------------------------------------------------------------------
        private System.Windows.Media.Brush FnGetColor(string strValue)
        {
            switch (strValue)
            {
                case "red":
                    return System.Windows.Media.Brushes.Red;
                case "orange":
                    return System.Windows.Media.Brushes.Orange;
                case "yellow":
                    return System.Windows.Media.Brushes.Yellow;
                case "green":
                    return System.Windows.Media.Brushes.Green;
                case "blue":
                    return System.Windows.Media.Brushes.Blue;
                case "deepblue":
                    return System.Windows.Media.Brushes.DarkBlue;
                case "violet":
                    return System.Windows.Media.Brushes.Violet;
                case "black":
                    return System.Windows.Media.Brushes.Black;
                case "white":
                    return System.Windows.Media.Brushes.White;
                case "transparent":
                    return System.Windows.Media.Brushes.Transparent;
                default:
                    return null;
            }
        }
        #endregion

        //=========================================================================================
        //---------------------------------------------------------------------------------------------------------------------
        #region Microsoft_Edge WebView2 초기화 함수
        private async void FnWebVew2_Initialization()
        {
            // the case of using a WeView2 Runtime which be installed in application machine  
            try
            {
                var op = new CoreWebView2EnvironmentOptions("--disable-web-security");
                var env = await CoreWebView2Environment.CreateAsync(null, null, op);

                webView2Browser_A.CoreWebView2InitializationCompleted += WebView_A_CoreWebView2InitializationCompleted;
                await webView2Browser_A.EnsureCoreWebView2Async(env);

                //--------------------------------------------------------------------------------------------------------------------------------------------------------

                webView2Browser_B.CoreWebView2InitializationCompleted += WebView_B_CoreWebView2InitializationCompleted;
                await webView2Browser_B.EnsureCoreWebView2Async(env);

            }
            catch (Exception ex)
            {
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnWebVew2_Initialization()");
                System.Windows.MessageBox.Show(Convert.ToString(ex));
            }

            #region for reference 
            /* 
            // the case of using a fixed WeView2 Runtime 
            try
            {
                var op = new CoreWebView2EnvironmentOptions("--disable-web-security");
                
                string subKey;

                if (Environment.Is64BitOperatingSystem) //64bit OS
                {
                    subKey = @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"; // 64 bit
                    using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(subKey))
                    {
                        if (ndpKey != null && ndpKey.GetValue("pv") != null) {

                            var env = await CoreWebView2Environment.CreateAsync(@"C:\Program Files (x86)\Microsoft\EdgeWebView\Application\" + ndpKey.GetValue("pv"), null, op);

                            await webView2Browser_A.EnsureCoreWebView2Async(env);
                            if ((webView2Browser_A != null) && (webView2Browser_A.CoreWebView2 != null))
                            {
                                webView2Browser_A.DefaultBackgroundColor = System.Drawing.Color.Transparent;
                                webView2Browser_A.NavigationCompleted += webView2Browser_A_NavigationCompleted;
                                webView2Browser_A.CoreWebView2.ProcessFailed += webView2Browser_A_ProcessFailed;

                                _Main.fnQueueAdd(eQueueMode.StatusWrite, "Function Completed : FnWebVew2_A_Initialization()");
                            }
                            else
                            {
                                _Main.fnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnWebVew2_A_Initialization()");
                            }

                            //--------------------------------------------------------------------------------------------------------------------------------------------------------
                            await webView2Browser_B.EnsureCoreWebView2Async(env);
                            if ((webView2Browser_B != null) && (webView2Browser_B.CoreWebView2 != null))
                            {
                                webView2Browser_B.DefaultBackgroundColor = System.Drawing.Color.Transparent;
                                webView2Browser_B.NavigationCompleted += webView2Browser_B_NavigationCompleted;
                                webView2Browser_B.CoreWebView2.ProcessFailed += webView2Browser_B_ProcessFailed;

                                _Main.fnQueueAdd(eQueueMode.StatusWrite, "Function Completed : FnWebVew2_B_Initialization()");
                            }
                            else
                            {
                                _Main.fnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnWebVew2_B_Initialization()");
                            }
                        }
                        else
                        {
                            _Main.fnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnWebVew2_Initialization()");
                        }
                    }
                }
                else  //32bit OS
                {
                    subKey = @"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"; // 32 bit
                    using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subKey))
                    {
                        if (ndpKey != null && ndpKey.GetValue("pv") != null)
                        {
                            var env = await CoreWebView2Environment.CreateAsync(@"C:\Program Files (x86)\Microsoft\EdgeWebView\Application\" + ndpKey.GetValue("pv"), null, op);

                            await webView2Browser_A.EnsureCoreWebView2Async(env);
                            if ((webView2Browser_A != null) && (webView2Browser_A.CoreWebView2 != null))
                            {
                                webView2Browser_A.DefaultBackgroundColor = System.Drawing.Color.Transparent;
                                webView2Browser_A.NavigationCompleted += webView2Browser_A_NavigationCompleted;
                                webView2Browser_A.CoreWebView2.ProcessFailed += webView2Browser_A_ProcessFailed;

                                _Main.fnQueueAdd(eQueueMode.StatusWrite, "Function Completed : FnWebVew2_A_Initialization()");
                            }
                            else
                            {
                                _Main.fnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnWebVew2_A_Initialization()");
                            }

                            //--------------------------------------------------------------------------------------------------------------------------------------------------------
                            await webView2Browser_B.EnsureCoreWebView2Async(env);
                            if ((webView2Browser_B != null) && (webView2Browser_B.CoreWebView2 != null))
                            {
                                webView2Browser_B.DefaultBackgroundColor = System.Drawing.Color.Transparent;
                                webView2Browser_B.NavigationCompleted += webView2Browser_B_NavigationCompleted;
                                webView2Browser_B.CoreWebView2.ProcessFailed += webView2Browser_B_ProcessFailed;

                                _Main.fnQueueAdd(eQueueMode.StatusWrite, "Function Completed : FnWebVew2_B_Initialization()");
                            }
                            else
                            {
                                _Main.fnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnWebVew2_B_Initialization()");
                            }
                        }
                        else
                        {
                            _Main.fnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnWebVew2_Initialization()");
                        }
                    }
                }
            }
            catch (Exception)
            {
                _Main.fnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnWebVew2_Initialization()");
            }         
            */
            #endregion
        }
        private void WebView_A_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                //webView2Browser_A.DefaultBackgroundColor = System.Drawing.Color.Transparent;  // windows 10 이후만 가능
                webView2Browser_A.NavigationCompleted += WebView2Browser_A_NavigationCompleted;
                webView2Browser_A.CoreWebView2.ProcessFailed += WebView2Browser_A_ProcessFailed;

                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Completed : FnWebVew2_A_Initialization()");
            }
            else
            {
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnWebVew2_A_Initialization()");
            }
        }
        private void WebView_B_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                //webView2Browser_B.DefaultBackgroundColor = System.Drawing.Color.Transparent;  // windows 10 이후만 가능
                webView2Browser_B.NavigationCompleted += WebView2Browser_B_NavigationCompleted;
                webView2Browser_B.CoreWebView2.ProcessFailed += WebView2Browser_B_ProcessFailed;

                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Completed : FnWebVew2_B_Initialization()");
            }
            else
            {
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnWebVew2_B_Initialization()");
            }
        }

        #endregion

        //=========================================================================================
        #region 기타함수
        private void FnPlayerStop()
        {            
            FnStopCommon();
            FnStopA();
            FnStopB();         
        }

        private void FnStopCommon()
        {
            try
            {                
                // BGM Stop
                FnBgmStop();

                // 이미지 효과 초기화
                _transitions.SelectedIndex = -1;
                _data.SelectedIndex = -1;

                // PowerPoint Viewer Close
                _Main.FnProcessKill("PPTVIEW");

                // Digital TV Viewer Close
                _Main.FnProcessKill("CAPAPPLICATION");
                 
            }
            catch (Exception) 
            {
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnStopCommon()");
            }
        }

        private void FnStopA()
        {
            try
            {
                FnMovieStop_A();
                //FnFlashStop(flashPlayer_A);
                   
                if (Timer_imageA != null) Timer_imageA.Stop();
                //if (Timer_FlashA != null) Timer_FlashA.Stop();          

                // HTML contents close in A area
                if (Timer_WEBA != null) Timer_WEBA.Stop();
                // a case of using Microsoft_Edge WebView2 API
                if ((webView2Browser_A != null) && (webView2Browser_A.CoreWebView2 != null))
                {
                    webView2Browser_A.Stop();
                    webView2Browser_A.Dispose();
                }
            }
            catch (Exception)
            {
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnStopA()");
            }
        }

        private void FnStopB()
        {
            try
            {
                FnMovieStop_B();
                //FnFlashStop(flashPlayer_B);
         
                if (Timer_imageB != null) Timer_imageB.Stop();
                //if (Timer_FlashB != null) Timer_FlashB.Stop();

                // HTML contents close in B area
                if (Timer_WEBB != null) Timer_WEBB.Stop();
                // a case of using Microsoft_Edge WebView2 API
                if ((webView2Browser_B != null) && (webView2Browser_B.CoreWebView2 != null))
                {
                    webView2Browser_B.Stop();
                    webView2Browser_B.Dispose();
                }
            }
            catch (Exception)
            {
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnStopB()");
            }
        }

        private void FnTempleteStop()
        {
            try
            {                                
                // Thread Timer 해제 for templete services
                if (TempleteThreadingTimer != null)
                {
                    TempleteThreadingTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    TempleteThreadingTimer.Dispose();
                }
                
                
                // Stop a Storyboard for Single news display (한줄뉴스, 우-->좌) 
                if (storyboard != null)
                {
                    storyboard.Stop(ticker_textBlock);
                    storyboard.Remove(ticker_textBlock);
                    storyboard = null;
                }
                // Stop a Storyboard for Single news display (한줄뉴스, 페이드 인 --> 페이드 아웃) 
                if (fade_storyboard != null)
                {
                    // it should be included to stop the storyboard of fade_textBlock
                    fade_storyboard.Completed -= new EventHandler(Fn_Fade_Storyboard_Completed);

                    fade_storyboard.Stop(fade_textBlock);
                    fade_storyboard.Remove(fade_textBlock);                   

                    fade_storyboard = null;
                } 
                
                
                // special contents for each templete 
                if (WEBBrowser_Weather.ReadyState != WebBrowserReadyState.Uninitialized)
                {
                    WEBBrowser_Weather.Stop();
                    WEBBrowser_Weather.Dispose();
                }                
                //FnFlashStop(flashPlayer_Weather);

                if (WEBBrowser_Clock.ReadyState != WebBrowserReadyState.Uninitialized)
                {
                    WEBBrowser_Clock.Stop();
                    WEBBrowser_Clock.Dispose();
                }
                //FnFlashStop(flashPlayer_Clock);
                
            }
            catch (Exception)
            {
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnTempleteStop()");
            }
        }

        #region Flash
        /*
        private void FnFlashStop(AxShockwaveFlashObjects.AxShockwaveFlash _flashPlayer)
        {
            try
            {
                if (_flashPlayer.IsHandleCreated)
                {
                    _flashPlayer.Stop();
                    _flashPlayer.Dispose();
                }
            }
            catch (Exception) { }
        }
        */
        #endregion

        private int FnLayoutCount(string layoutNo)
        {
            int cnt = 1;
            switch (layoutNo)
            {
                case "b0":
                case "b1":
                case "b2":
                case "f0":
                case "f1":
                    cnt = 2;
                    break;

                default:
                    cnt = 1;  //a0, e0, e1, e2, e4, t0
                    break;
            }
            return cnt;
        }

        private string FnLayoutTitle(int n)
        {
            string strTitle = string.Empty;
            switch (n)
            {
                case 1:
                    strTitle = "B";
                    break;

                default:
                    strTitle = "A";
                    break;
            }
            return strTitle;
        }

        private ePlayMode FnPlayMode(string strValue)
        {
            if (string.IsNullOrEmpty(strValue))
                return ePlayMode.NULL;
            else if (_Main._ListMovie.Contains(strValue))
                return ePlayMode.Movie;
            else if (_Main._ListImage.Contains(strValue))
                return ePlayMode.Image;
            else if (_Main._ListFlash.Contains(strValue))
                return ePlayMode.Flash;
            else if (_Main._ListPpt.Contains(strValue))
                return ePlayMode.Powerpoint;
            else if (_Main._ListMusic.Contains(strValue))
                return ePlayMode.Music;
            else if (_Main._ListDtv.Contains(strValue))
                return ePlayMode.Dtv;
            else if ((_Main._ListWeb.Contains(strValue)) || (strValue.Trim().ToLower().StartsWith("http://")))
                return ePlayMode.Html;
            else if (_Main._ListCctv.Contains(strValue))
                return ePlayMode.Cctv;
            return ePlayMode.NULL;
        }

        public void FnPlayerTopMost()
        {
            try
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { this.Topmost = true; }));
                //Win32API.ShowWindow(Win32API.FindWindow("Shell_TrayWnd", null), 0);
                Win32API.BringWindowToTop(_PlayerHandle);
                Win32API.SetForegroundWindow(_PlayerHandle);
            }
            catch (Exception)
            {
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : FnPlayerTopMost()");
            }
        }

        public int FnString2Int(string s)
        {
            bool result = int.TryParse(s, out int i);
            return i;
        }        
        #endregion

        #region 이벤트 모음
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (_WindowsClosed == false) _WindowsClosed = true;
                FnKioskPlayProc(eKioskPlayStatus.Stop);
                FnPlayerStop();
                FnTempleteStop();               
            }
            catch (Exception)
            {
                _Main.FnQueueAdd(eQueueMode.StatusWrite, "Function Error : Window_Closing()");
            }
        }
        #endregion
    }

    #region Class Picture
    public class Picture
    {
        public Picture(string uri, string stretch)
        {
            _uri = uri;
            _stretch = stretch;
        }

        private readonly string _uri;
        public string GetUri
        {
            get { return _uri; }
        }

        private readonly string _stretch;
        public string GetStretch
        {
            get { return _stretch; }
        }
    }
    #endregion
    
    #region 구조체
   
    #endregion
}
