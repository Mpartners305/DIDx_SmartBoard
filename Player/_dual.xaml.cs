﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Xml;
using DirectShowLib;

namespace Player
{
    public partial class _dual : Window
    {
        #region 변수 선언
        private const int WMGraphNotify_B = 0x0400 + 15;
        private IGraphBuilder graphBuilder_B = null;
        private IMediaControl mediaControl_B = null;
        private IMediaEventEx mediaEventEx_B = null;
        private IVideoWindow videoWindow_B = null;
        private IBasicAudio basicAudio_B = null;

        List<StructContents> _ListStructContents_B = new List<StructContents>();

        IntPtr _PlayerHandle = IntPtr.Zero;
        int _CurrentPlayNo_B = 0, _CyclePlayCount_B = 0;
        int _PlayLeft_B = 0, _PlayTop_B = 0, _PlayWidth_B = 0, _PlayHeight_B = 0;
        double _ScreenHeightRatio = 0, _ScreenWidthRatio = 0;
        bool _IsFirstImage_B = true, _WindowsClosed = false;
        Grid playGrid_B, flashGrid_B;

        Random random = new Random();
        DispatcherTimer timer_imageB = new DispatcherTimer();
        DispatcherTimer Timer_FlashB = new DispatcherTimer();

        AxShockwaveFlashObjects.AxShockwaveFlash flashPlayer_B = new AxShockwaveFlashObjects.AxShockwaveFlash(); 
        #endregion

        #region 생성자 & 시작
        public _main _Main;
        public _dual(_main win)
        {
            InitializeComponent();
            _Main = win;

            fnFormInit();
        }

        private void fnFormInit()
        {
            System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;
            this.Left = screens[_Main._Secondary].Bounds.Left;
            this.Top = screens[_Main._Secondary].Bounds.Top;
            this.Width = screens[_Main._Secondary].Bounds.Width;
            this.Height = screens[_Main._Secondary].Bounds.Height;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            fnGlobalHook();
            fnPlayerStart();
        }
        #endregion

        #region WndProc
        private void fnGlobalHook()
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
                        _Main.fnProcessKill("PPTVIEW");
                        _CyclePlayCount_B++;
                        fnPlay_B(++_CurrentPlayNo_B);
                    }
                    break;
                case WMGraphNotify_B:
                    HandleGraphEvent_B();
                    break;
                default:
                    break;
            }
            if (this.videoWindow_B != null) this.videoWindow_B.NotifyOwnerMessage(hWnd, nMsg, wParam, lParam);
            return IntPtr.Zero;
        }

        private void HandleGraphEvent_B()
        {
            try
            {
                EventCode evCode;
                IntPtr evParam1, evParam2;

                if (this.mediaEventEx_B == null) return;

                while (this.mediaEventEx_B.GetEvent(out evCode, out evParam1, out evParam2, 0) == 0)
                {
                    if (evCode == EventCode.Complete)
                    {
                        Task.Factory.StartNew(() =>
                        {
                            fnMovieStop_B();
                            _CyclePlayCount_B++;
                            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { fnPlay_B(++_CurrentPlayNo_B); }));
                        });
                        break;
                    }
                }
            }
            catch (Exception) { }
        }
        #endregion

        // ===============================

        #region Player Start
        private void fnPlayerStart()
        {
            try
            {
                fnPlayerStop();
                fnCodeInit();
                fnGridInit();
                fnLayoutStructSet();
                fnPlay();
            }
            catch (Exception) { _Main.fnPlayerErrorProc("Player Error"); }
        }
        #endregion

        #region CodeInit
        private void fnCodeInit()
        {
            Win32API.SetCursorPos(Convert.ToInt32(this.Width), 0); // 마우스 위치조정

            timer_imageB.Tick += new EventHandler(timer_imageB_Tick);
            Timer_FlashB.Interval = TimeSpan.FromMilliseconds(100);
            Timer_FlashB.Tick += new EventHandler(Timer_FlashB_Tick);
            
            // 모니터 가로, 세로 사이즈 비율
            _ScreenWidthRatio = this.Width / rootGrid.Width;
            _ScreenHeightRatio = this.Height / rootGrid.Height;
        }
        
        #endregion

        #region PlayGird, Control Init
        private void fnGridInit()
        {
            double dblLeft_B, dblTop_B, dblWidth_B, dblHeight_B;
            // Play Grid Size
            dblLeft_B = 0;
            dblTop_B = 0;
            dblWidth_B = rootGrid.Width;
            dblHeight_B = rootGrid.Height;

            // Screen 가로, 세로 비율 적용
            _PlayLeft_B = (int)(dblLeft_B * _ScreenWidthRatio);
            _PlayTop_B = (int)(dblTop_B * _ScreenHeightRatio);
            _PlayWidth_B = (int)Math.Ceiling(dblWidth_B * _ScreenWidthRatio);
            _PlayHeight_B = (int)Math.Ceiling(dblHeight_B * _ScreenHeightRatio);

            // playGrid_B
            playGrid_B = new Grid();
            playGrid_B.SetValue(Grid.ColumnProperty, 0);
            playGrid.Children.Add(playGrid_B);

            // Flash Grid                    
            flashGrid_B = new Grid();
            playGrid_B.Children.Add(flashGrid_B);
        }
        #endregion

        #region Playlist 구조체 Set
        private void fnLayoutStructSet()
        {
            List<object> _ListData = new List<object>();
            string strXmlFormat = string.Format("//playlist/schedule[@mode='{0}'][@no='{1}']/layout[@no='{2}']/file"
                              , _Main._CurrentPlayScheduleMode, _Main._CurrentPlayScheduleNo, 1);
            foreach (XmlNode xNode in _Main._PlaylistXmlDoc.SelectNodes(strXmlFormat))
            {
                string strFileName = Path.GetFileName(_Main.fnXmlNodeCheck(xNode));
                int nCIdx = _Main.fnString2Int(_Main.fnXmlNodeCheck(xNode.Attributes["id"]));

                StructContents structContents = new StructContents();
                structContents.file_name = strFileName;
                structContents.c_idx = nCIdx;
                _ListStructContents_B.Add(structContents);
                _ListData.Add(new Picture(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", strFileName), "Fill"));
            }
            if (_ListData.Count > 0) this.DataContext = _data.ItemsSource = _ListData;
        }
        #endregion

        //=====================================

        #region Play Start
        private void fnPlay()
        {
            fnPlay_B(0);
        }
        #endregion

        #region Play B
        private void fnPlay_B(int p)
        {
            try
            {
                _CurrentPlayNo_B = p;
                if (_CurrentPlayNo_B >= _ListStructContents_B.Count)
                {
                    if (_CyclePlayCount_B > 0)
                    {
                        _CyclePlayCount_B = 0;
                        fnPlay_B(0);
                    }
                    return;
                }

                string strFileName = _ListStructContents_B[_CurrentPlayNo_B].file_name;
                
                // File Check
                string strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", strFileName);
                if (!File.Exists(strFileFullPath))
                {
                    fnPlay_B(++_CurrentPlayNo_B);
                    return;
                }

                switch (fnPlayMode(Path.GetExtension(strFileName).ToLower()))
                {
                    case ePlayMode.Movie:
                        _IsFirstImage_B = true;
                        fnMoviePlay_B();
                        break;
                    case ePlayMode.Image:
                        fnImagePlay_B();
                        break;
                    case ePlayMode.Powerpoint:
                        _IsFirstImage_B = true;
                        fnPPTPlay_B();
                        break;
                    case ePlayMode.Flash:
                        _IsFirstImage_B = true;
                        fnFlashPlay_B();
                        break;
                    default:
                        fnPlay_B(++_CurrentPlayNo_B);
                        break;
                }
            }
            catch (Exception)
            {
                fnPlay_B(++_CurrentPlayNo_B);
            }
        }

        private void fnMoviePlay_B()
        {
            grid_transition.Visibility = Visibility.Collapsed;
            flashGrid_B.Visibility = Visibility.Collapsed;
            Task.Factory.StartNew(() => { fnThreadMovieStart_B(); });
        }

        private void fnThreadMovieStart_B()
        {
            try
            {
                string strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", _ListStructContents_B[_CurrentPlayNo_B].file_name);
                int nVolume = -10000;
                int hr = 0;
                this.graphBuilder_B = (IGraphBuilder)new FilterGraph();

                hr = this.graphBuilder_B.RenderFile(strFileFullPath, null);
                DsError.ThrowExceptionForHR(hr);

                this.mediaControl_B = (IMediaControl)this.graphBuilder_B;
                this.mediaEventEx_B = (IMediaEventEx)this.graphBuilder_B;
                this.videoWindow_B = this.graphBuilder_B as IVideoWindow;
                this.basicAudio_B = this.graphBuilder_B as IBasicAudio;

                OABool lVisible;
                hr = this.videoWindow_B.get_Visible(out lVisible);
                DsError.ThrowExceptionForHR(hr);

                hr = this.mediaEventEx_B.SetNotifyWindow(_PlayerHandle, WMGraphNotify_B, IntPtr.Zero);
                DsError.ThrowExceptionForHR(hr);

                hr = this.videoWindow_B.put_Owner(_PlayerHandle);
                DsError.ThrowExceptionForHR(hr);

                hr = this.videoWindow_B.SetWindowPosition(_PlayLeft_B, _PlayTop_B, _PlayWidth_B, _PlayHeight_B);
                DsError.ThrowExceptionForHR(hr);

                hr = this.videoWindow_B.put_WindowStyle(DirectShowLib.WindowStyle.Child | DirectShowLib.WindowStyle.ClipSiblings | DirectShowLib.WindowStyle.ClipChildren);
                DsError.ThrowExceptionForHR(hr);

                hr = this.mediaControl_B.Run();
                DsError.ThrowExceptionForHR(hr);

                try
                {
                    hr = this.basicAudio_B.put_Volume(nVolume); // -10000 ~ 0
                    DsError.ThrowExceptionForHR(hr);
                }
                catch (Exception) { }

                // Live, Log
                _Main._StructLive.file_name_B = _ListStructContents_B[_CurrentPlayNo_B].file_name;
                _Main._StructLive.thumb_img_B = _ListStructContents_B[_CurrentPlayNo_B].c_idx + ".jpg";
                _Main.fnPlayerLiveLog("B");
            }
            catch (Exception)
            {
                if (!_WindowsClosed)
                {
                    fnMovieStop_B();
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { fnPlay_B(++_CurrentPlayNo_B); }));
                }
            }
        }

        private void fnMovieStop_B()
        {
            try
            {
                lock (this)
                {
                    if (this.mediaControl_B != null)
                    {
                        this.mediaControl_B.Stop();
                        this.mediaControl_B = null;
                    }
                    if (this.mediaEventEx_B != null)
                    {
                        this.mediaEventEx_B.SetNotifyWindow(IntPtr.Zero, 0, IntPtr.Zero);
                        this.mediaEventEx_B = null;
                    }
                    if (this.basicAudio_B != null)
                        this.basicAudio_B = null;
                    if (this.videoWindow_B != null)
                        this.videoWindow_B = null;
                    if (this.graphBuilder_B != null)
                    {
                        Marshal.ReleaseComObject(this.graphBuilder_B);
                        this.graphBuilder_B = null;
                        //GC.Collect();
                    }
                }
            }
            catch (Exception) { }
        }

        private void fnImagePlay_B()
        {
            grid_transition.Visibility = Visibility.Visible;
            flashGrid_B.Visibility = Visibility.Collapsed;

            // Win XP 일때 0 ~ 11
            int nTransitionCount = (Environment.OSVersion.Version.Major < 6) ? 12 : _transitions.Items.Count;
            int img_no = (_IsFirstImage_B) ? -1 : (_Main._StructDspConfig.transition.Equals("disable")) ? -1 : random.Next(0, nTransitionCount);
            _transitions.SelectedIndex = img_no;
            _data.SelectedIndex = _CurrentPlayNo_B;

            timer_imageB.Interval = TimeSpan.FromSeconds(_Main.fnString2Int(_Main._StructDspConfig.duration));
            timer_imageB.Start();

            // Live, Log
            _Main._StructLive.file_name_B = _ListStructContents_B[_CurrentPlayNo_B].file_name;
            _Main._StructLive.thumb_img_B = _ListStructContents_B[_CurrentPlayNo_B].c_idx + ".jpg";
            _Main.fnPlayerLiveLog("B");
        }

        void timer_imageB_Tick(object sender, EventArgs e)
        {
            timer_imageB.Stop();
            _IsFirstImage_B = false;

            _CyclePlayCount_B++;
            fnPlay_B(++_CurrentPlayNo_B);
        }

        private void fnFlashPlay_B()
        {
            flashGrid_B.Visibility = Visibility.Visible;
            grid_transition.Visibility = Visibility.Collapsed;

            WindowsFormsHost host = new WindowsFormsHost();
            host.Child = flashPlayer_B;
            flashGrid_B.Children.Add(host);

            Task.Factory.StartNew(() => { fnThreadFlashB(); });
        }

        private void fnThreadFlashB()
        {
            try
            {
                string strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", _ListStructContents_B[_CurrentPlayNo_B].file_name);
                flashPlayer_B.LoadMovie(0, strFileFullPath);
                flashPlayer_B.Play();
                flashPlayer_B.ScaleMode = 2;
                flashPlayer_B.Loop = false;
                flashPlayer_B.Menu = false;

                Timer_FlashB.Start();

                // Live, Log
                _Main._StructLive.file_name_B = _ListStructContents_B[_CurrentPlayNo_B].file_name;
                _Main._StructLive.thumb_img_B = _ListStructContents_B[_CurrentPlayNo_B].c_idx + ".jpg";
                _Main.fnPlayerLiveLog("B");
            }
            catch (Exception)
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { fnPlay_B(++_CurrentPlayNo_B); }));
            }
        }

        void Timer_FlashB_Tick(object sender, EventArgs e)
        {
            if (!flashPlayer_B.Playing)
            {
                Timer_FlashB.Stop();
                fnFlashStop(flashPlayer_B);

                _CyclePlayCount_B++;
                fnPlay_B(++_CurrentPlayNo_B);
            }
        }

        private void fnPPTPlay_B()
        {
            grid_transition.Visibility = Visibility.Collapsed;
            flashGrid_B.Visibility = Visibility.Collapsed;
            Task.Factory.StartNew(() => { fnThreadPPT(); });
        }
        #endregion

        #region PPT Play
        private void fnThreadPPT()
        {
            try
            {
                fnPlayerTopMost();  // TopMost

                string strFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "contents", _ListStructContents_B[_CurrentPlayNo_B].file_name);
                int nLeft = _PlayLeft_B;
                int nTop = _PlayTop_B;
                int nWidth = _PlayWidth_B;
                int nHeight = _PlayHeight_B;

                string strPptViewerPath = Path.Combine(_Main.fnGetProgramFilesX86FolderName(), "Microsoft Office", "Office14", "PPTVIEW.EXE");
                ProcessStartInfo psi = new ProcessStartInfo(strPptViewerPath, strFileFullPath);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                Process p = Process.Start(psi);
                p.WaitForInputIdle();
                p.Close();

                IntPtr handle = IntPtr.Zero;
                for (int i = 0; i < 10; i++)
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

                // Live, Log
                _Main._StructLive.file_name_B = _ListStructContents_B[_CurrentPlayNo_B].file_name;
                _Main._StructLive.thumb_img_B = _ListStructContents_B[_CurrentPlayNo_B].c_idx + ".jpg";
                _Main.fnPlayerLiveLog("B");
            }
            catch (Exception)
            {
                _Main.fnProcessKill("PPTVIEW");
                _Main.fnQueueAdd(eQueueMode.StatusWrite, "Function Error : fnPPTProc()");
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { fnPlay_B(++_CurrentPlayNo_B); }));
            }
        }
        #endregion

        //=====================================

        #region 기타함수
        private void fnPlayerStop()
        {
            fnStopCommon();
            fnStopB();
        }

        private void fnStopB()
        {
            fnMovieStop_B();
            fnFlashStop(flashPlayer_B);
            if (timer_imageB != null) timer_imageB.Stop();
            if (Timer_FlashB != null) Timer_FlashB.Stop();
        }

        private void fnStopCommon()
        {
            // 이미지 효과 초기화
            _transitions.SelectedIndex = -1;
            _data.SelectedIndex = -1;

            // PowerPoint Viewer Close
            _Main.fnProcessKill("PPTVIEW");
        }

        private void fnFlashStop(AxShockwaveFlashObjects.AxShockwaveFlash _flashPlayer)
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

        private ePlayMode fnPlayMode(string strValue)
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
            else if (_Main._ListExe.Contains(strValue))
                return ePlayMode.Exe;
            else if (strValue.Trim().ToLower().StartsWith("http://"))
                return ePlayMode.Web;
            return ePlayMode.NULL;
        }

        public void fnPlayerTopMost()
        {
            try
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { this.Topmost = true; }));
                //Win32API.ShowWindow(Win32API.FindWindow("Shell_TrayWnd", null), 0);
                Win32API.BringWindowToTop(_PlayerHandle);
                Win32API.SetForegroundWindow(_PlayerHandle);
            }
            catch (Exception) { }
        }
        #endregion

        #region 이벤트 모음
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _WindowsClosed = true;
                fnPlayerStop();
            }
            catch (Exception) { }
        }
        #endregion
    }

}
