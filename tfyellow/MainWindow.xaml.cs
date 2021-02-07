using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using OpenCvSharp;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using MahApps.Metro.Controls;
using System.Runtime.InteropServices;
using Rectangle = System.Drawing.Rectangle;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using System.Diagnostics;
using System.IO.Ports;

namespace tfyellow
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow
    {
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern UInt16 GetAsyncKeyState(Int32 vKey);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            internal int left;
            internal int top;
            internal int right;
            internal int bottom;
        }


        string AppName = "";//"MapleStory"; //
        Bitmap srcImg = null;
        Bitmap srcImg2 = null;
        SerialPort sp = new SerialPort();



        //백그라운드 워커 선언
        private BackgroundWorker myThread;

        public MainWindow()
        {
            srcImg = new Bitmap(@"Bitmap.bmp");
            srcImg2 = new Bitmap(@"Bitmap2.bmp");
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            //백그라운드 워커 초기화
            //작업의 진행율이 바뀔때 ProgressChanged 이벤트 발생여부
            //작업취소 가능여부 true로 설정
            myThread = new BackgroundWorker()
            {
                WorkerReportsProgress = false, //진행률
                WorkerSupportsCancellation = true //취소여부
            };

            //백그라운드에서 실행될 콜백 이벤트 생성
            //For the performing operation in the background.   
            //해야할 작업을 실행할 메소드 정의
            myThread.DoWork += myThread_DoWork;

            //작업이 완료되었을 때 실행할 콜백메소드 정의  
            myThread.RunWorkerCompleted += myThread_RunWorkerCompleted;

            lblState.Content = "Worker 초기화";
        }


        //백그라운드 워커가 실행하는 작업
        //DoWork 이벤트 처리 메소드에서 lstNumber.Items.Add(i) 와 같은 코드를
        //직접 실행시키면 "InvalidOperationException" 오류발생
        private void myThread_DoWork(object sender, DoWorkEventArgs e)
        {
            //int count = (int)e.Argument;
            int i = 0;
            int prevKeyState = 0;
            string str;
            int count = 0;

            RECT cRect, wRect;
            int appPointX;
            int appPointY;
            int appSizeWidth;
            int appSizeHeight;

            bool b = false;

            while (!myThread.CancellationPending)
            {
                if (GetAsyncKeyState(69) > 1 && prevKeyState <= 1)
                {
                   //if (b) b = false;
                    //else b = true;
                    b = true;
                }

                if (b && tglRun.IsOn)
                {
                    IntPtr hWnd = FindWindow(null, AppName);
                    if (hWnd != IntPtr.Zero)
                    {
                        GetClientRect(hWnd, out cRect);
                        GetWindowRect(hWnd, out wRect);

                        appPointX = wRect.left + (wRect.right - wRect.left - cRect.right) / 2;
                        appPointY = wRect.bottom - cRect.bottom - (wRect.right - wRect.left - cRect.right) / 2;
                        appSizeWidth = cRect.right;
                        appSizeHeight = cRect.bottom;

                        using (Bitmap bmp = new Bitmap(55, 55, PixelFormat.Format32bppArgb))
                        using (Graphics gr = Graphics.FromImage(bmp))
                        {
                            gr.CopyFromScreen(appPointX + 825, appPointY + 970, 0, 0, bmp.Size);
                            //printImg(bmp, imgSpeed);

                            using (Mat ScreenMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp))
                            using (Mat FindMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(srcImg))
                            using (Mat FindMat2 = OpenCvSharp.Extensions.BitmapConverter.ToMat(srcImg2))
                            using (Mat res = ScreenMat.MatchTemplate(FindMat, TemplateMatchModes.CCoeffNormed))
                            using (Mat res2 = ScreenMat.MatchTemplate(FindMat2, TemplateMatchModes.CCoeffNormed))
                            {
                                double minval, maxval = 0;
                                double minval2, maxval2 = 0;
                                OpenCvSharp.Point minloc, maxloc;
                                OpenCvSharp.Point minloc2, maxloc2;
                                Cv2.MinMaxLoc(res, out minval, out maxval, out minloc, out maxloc);
                                Cv2.MinMaxLoc(res2, out minval2, out maxval2, out minloc2, out maxloc2);
                                if (maxval2 > 0.90)
                                {
                                    //찾을때 이벤트
                                    if (sp.IsOpen)
                                    {
                                        sp.Write("KDw");
                                        Thread.Sleep(10);
                                        sp.Write("KUw");
                                    }
                                }

                                if (maxval > 0.90)
                                {
                                    //찾을때 이벤트
                                    if (sp.IsOpen)
                                    {
                                        sp.Write("KDw");
                                        Thread.Sleep(10);
                                        sp.Write("KUw");

                                        b = false;
                                    }
                                }
                            }
                        }
                    }
                }

                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                                    (ThreadStart)delegate ()
                                                    {
                                                        if (b)
                                                        {
                                                            lblResult.Content = "실행중";
                                                            lblResult.Foreground = System.Windows.Media.Brushes.Blue;
                                                        }
                                                        else
                                                        {
                                                            lblResult.Content = "정지중";
                                                            lblResult.Foreground = System.Windows.Media.Brushes.Red;
                                                        }
                                                    });


                if (GetAsyncKeyState(121) > 1 && prevKeyState <= 1 && false)
                {
                    string strpos = "";
                    count++;

                    IntPtr hWnd = FindWindow(null, (string)e.Argument);
                    if (hWnd != IntPtr.Zero)
                    {
                        GetClientRect(hWnd, out cRect);
                        GetWindowRect(hWnd, out wRect);

                        appPointX = wRect.left + (wRect.right - wRect.left - cRect.right) / 2;
                        appPointY = wRect.bottom - cRect.bottom - (wRect.right - wRect.left - cRect.right) / 2;
                        appSizeWidth = cRect.right;
                        appSizeHeight = cRect.bottom;


                        strpos += "cRect.left : " + cRect.left + "\n";
                        strpos += "cRect.right : " + cRect.right + "\n";
                        strpos += "cRect.top : " + cRect.top + "\n";
                        strpos += "cRect.bottom : " + cRect.bottom + "\n\n";

                        strpos += "wRect.left : " + wRect.left + "\n";
                        strpos += "wRect.right : " + wRect.right + "\n";
                        strpos += "wRect.top : " + wRect.top + "\n";
                        strpos += "wRect.bottom : " + wRect.bottom + "\n\n";

                        strpos += "appPointX : " + appPointX + "\n";
                        strpos += "appPointY : " + appPointY + "\n";
                        strpos += "appSizeWidth : " + appSizeWidth + "\n";
                        strpos += "appSizeHeight : " + appSizeHeight + "\n";


                        //플레이어를 찾았을 경우
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                                    (ThreadStart)delegate ()
                                                    {
                                                        lblPos.Content = strpos;

                                                        IntPtr findwindow = FindWindow(null, AppName);
                                                        if (findwindow != IntPtr.Zero)
                                                        {
                                                            //플레이어를 찾았을 경우
                                                            lblState.Content = "찾았습니다.";

                                                            //찾은 플레이어를 바탕으로 Graphics 정보를 가져옵니다.
                                                            Graphics Graphicsdata = Graphics.FromHwnd(findwindow);

                                                            //찾은 플레이어 창 크기 및 위치를 가져옵니다. 
                                                            Rectangle rect = Rectangle.Round(Graphicsdata.VisibleClipBounds);

                                                            //플레이어 창 크기 만큼의 비트맵을 선언해줍니다.
                                                            Bitmap bmp = new Bitmap(rect.Width, rect.Height);

                                                            //비트맵을 바탕으로 그래픽스 함수로 선언해줍니다.
                                                            using (Graphics g = Graphics.FromImage(bmp))
                                                            {
                                                                //찾은 플레이어의 크기만큼 화면을 캡쳐합니다.
                                                                IntPtr hdc = g.GetHdc();
                                                                PrintWindow(findwindow, hdc, 0x2);
                                                                g.ReleaseHdc(hdc);
                                                            }

                                                            // pictureBox1 이미지를 표시해줍니다.
                                                            printImg(bmp, imgPrint);
                                                            printImg(srcImg, imgSrcPrint);

                                                            searchIMG(bmp, srcImg);
                                                        }
                                                    });

                    }
                    else
                    {
                        //플레이어를 못찾을경우
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                                    (ThreadStart)delegate ()
                                                    {

                                                        lblState.Content = "못찾았어요";
                                                    });
                    }
                }

                prevKeyState = GetAsyncKeyState(69);

                str = i++ + "\n정지신호 : " + myThread.CancellationPending.ToString() + "\nF9 상태 : " + GetAsyncKeyState(121).ToString() + "\ncount : " + count.ToString();
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                            (ThreadStart)delegate ()
                                            {
                                                
                                                lblState.Content = str;
                                            });
                Thread.Sleep(50);
            }
            
            e.Cancel = true;
        }

        //작업완료
        private void myThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled) lblState.Content = "작업 취소";
            else if (e.Error != null) lblState.Content = "오류 발생";
            else
            {
                //tblkSum.Text = ((int)e.Result).ToString();
                lblState.Content = "작업 완료";
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (AppName == "") //숫자가 아닐 경우 반환 num
            {
                lblState.Content = "앱 이름 입력해주세요";
                return;
            }

            if (myThread.IsBusy)
            {
                lblState.Content = "이미 실행중";
                return;
            }

            myThread.RunWorkerAsync(AppName);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            myThread.CancelAsync();
        }

        private void btn1_Click(object sender, RoutedEventArgs e)
        {
            IntPtr findwindow = FindWindow(null, AppName);
            if (findwindow != IntPtr.Zero)
            {
                //플레이어를 찾았을 경우
                lblState.Content = "앱플레이어 찾았습니다.";
            }
            else
            {
                //플레이어를 못찾을경우
                lblState.Content = "못찾았어요";
            }
        }

        private void btn2_Click(object sender, RoutedEventArgs e)
        {
            IntPtr findwindow = FindWindow(null, AppName);
            if (findwindow != IntPtr.Zero)
            {
                //플레이어를 찾았을 경우
                lblState.Content = "찾았습니다.";

                //찾은 플레이어를 바탕으로 Graphics 정보를 가져옵니다.
                Graphics Graphicsdata = Graphics.FromHwnd(findwindow);

                //찾은 플레이어 창 크기 및 위치를 가져옵니다. 
                System.Drawing.Rectangle rect = System.Drawing.Rectangle.Round(Graphicsdata.VisibleClipBounds);

                //플레이어 창 크기 만큼의 비트맵을 선언해줍니다.
                Bitmap bmp = new Bitmap(rect.Width, rect.Height);

                //비트맵을 바탕으로 그래픽스 함수로 선언해줍니다.
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    //찾은 플레이어의 크기만큼 화면을 캡쳐합니다.
                    IntPtr hdc = g.GetHdc();
                    PrintWindow(findwindow, hdc, 0x2);
                    g.ReleaseHdc(hdc);
                }

                // pictureBox1 이미지를 표시해줍니다.

                using (MemoryStream memory = new MemoryStream())
                {
                    bmp.Save(memory, ImageFormat.Bmp);
                    memory.Position = 0; BitmapImage bitmapimage = new BitmapImage(); 
                    bitmapimage.BeginInit(); 
                    bitmapimage.StreamSource = memory;
                    bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapimage.EndInit();
                    imgPrint.Source = bitmapimage;
                    bmp.Save("D:\\myBitmap.bmp");
                }
            }
            else
            {
                //플레이어를 못찾을경우
                lblState.Content = "못찾았어요";
            }
        }

        public void searchIMG(Bitmap screen_img, Bitmap find_img)
        {
            //스크린 이미지 선언
            using (Mat ScreenMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screen_img))
            //찾을 이미지 선언
            using (Mat FindMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(find_img))
            //스크린 이미지에서 FindMat 이미지를 찾아라
            using (Mat res = ScreenMat.MatchTemplate(FindMat, TemplateMatchModes.CCoeffNormed))
            {
                string str = "";
                //찾은 이미지의 유사도를 담을 더블형 최대 최소 값을 선언합니다.
                double minval, maxval = 0;
                //찾은 이미지의 위치를 담을 포인트형을 선업합니다.
                OpenCvSharp.Point minloc, maxloc;
                //찾은 이미지의 유사도 및 위치 값을 받습니다. 
                Cv2.MinMaxLoc(res, out minval, out maxval, out minloc, out maxloc);
                str += "minval : " + minval + "\n";
                str += "maxval : " + maxval + "\n";
                str += "minloc.X : " + minloc.X + "\n";
                str += "minloc.Y : " + minloc.Y + "\n";
                str += "maxloc.X : " + maxloc.X + "\n";
                str += "maxloc.Y : " + maxloc.Y + "\n";
                lblImgsrch.Content = str;

                if(maxval > 0.99)
                {
                    lblResult.FontWeight = FontWeights.Bold;
                    lblResult.Foreground = System.Windows.Media.Brushes.Blue;
                    lblResult.Content = "이미지 탐색 성공";
                }
                else
                {
                    lblResult.FontWeight = FontWeights.Bold;
                    lblResult.Foreground = System.Windows.Media.Brushes.Red;
                    lblResult.Content = "이미지 탐색 실패";
                }
            }
        }

        private void btnImgsrch_Click(object sender, RoutedEventArgs e)
        {
            IntPtr findwindow = FindWindow(null, AppName);
            if (findwindow != IntPtr.Zero)
            {
                //플레이어를 찾았을 경우
                lblState.Content = "찾았습니다.";

                //찾은 플레이어를 바탕으로 Graphics 정보를 가져옵니다.
                Graphics Graphicsdata = Graphics.FromHwnd(findwindow);

                //찾은 플레이어 창 크기 및 위치를 가져옵니다. 
                Rectangle rect = Rectangle.Round(Graphicsdata.VisibleClipBounds);

                //플레이어 창 크기 만큼의 비트맵을 선언해줍니다.
                Bitmap bmp = new Bitmap(rect.Width, rect.Height);

                //비트맵을 바탕으로 그래픽스 함수로 선언해줍니다.
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    //찾은 플레이어의 크기만큼 화면을 캡쳐합니다.
                    IntPtr hdc = g.GetHdc();
                    PrintWindow(findwindow, hdc, 0x2);
                    g.ReleaseHdc(hdc);
                }

                // pictureBox1 이미지를 표시해줍니다.
                printImg(bmp, imgPrint);
                printImg(srcImg, imgSrcPrint);

                searchIMG(bmp, srcImg);
            }
        }

        void printImg(Bitmap bmp, System.Windows.Controls.Image img)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bmp.Save(memory, ImageFormat.Bmp);
                memory.Position = 0; BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                img.Source = bitmapimage;
                //bmp.Save("D:\\myBitmap.bmp");
            }
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            // 화면 크기만큼의 Bitmap 생성
            using (Bitmap bmp = new Bitmap(500, 200, PixelFormat.Format32bppArgb))
            {
                // Bitmap 이미지 변경을 위해 Graphics 객체 생성
                using (Graphics gr = Graphics.FromImage(bmp))
                {
                    // 화면을 그대로 카피해서 Bitmap 메모리에 저장
                    gr.CopyFromScreen(50, 50, 200, 200, new System.Drawing.Size(500,500));
                }
                // Bitmap 데이타를 파일로 저장
                bmp.Save("5050200200.png", ImageFormat.Png);
            }
        }

        private void btnPrintWindow_Click(object sender, RoutedEventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            for (int i = 0; i < 100; i++)
            {
                IntPtr findwindow = FindWindow(null, AppName);
                if (findwindow != IntPtr.Zero)
                {
                    Graphics Graphicsdata = Graphics.FromHwnd(findwindow);
                    System.Drawing.Rectangle rect = System.Drawing.Rectangle.Round(Graphicsdata.VisibleClipBounds);

                    using (Bitmap bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            IntPtr hdc = g.GetHdc();
                            PrintWindow(findwindow, hdc, 0x2);
                            g.ReleaseHdc(hdc);
                        }
                        printImg(bmp, imgSpeed);
                        //bmp.Save("D:\\PrintWindow.png");
                    }
                }
                else
                {
                    MessageBox.Show("no");
                }
            }

            
            sw.Stop();

            lblSpeed.Content = "PrintWindow\t\t" + sw.ElapsedMilliseconds.ToString();
        }

        private void btnCopyFromScreen_Click(object sender, RoutedEventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            for(int i=0; i<100; i++)
            {
                IntPtr hWnd = FindWindow(null, AppName);
                if (hWnd != IntPtr.Zero)
                {
                    RECT cRect, wRect;
                    int appPointX;
                    int appPointY;
                    int appSizeWidth;
                    int appSizeHeight;

                    GetClientRect(hWnd, out cRect);
                    GetWindowRect(hWnd, out wRect);

                    appPointX = wRect.left + (wRect.right - wRect.left - cRect.right) / 2;
                    appPointY = wRect.bottom - cRect.bottom - (wRect.right - wRect.left - cRect.right) / 2;
                    appSizeWidth = cRect.right;
                    appSizeHeight = cRect.bottom;

                    using (Bitmap bmp = new Bitmap(appSizeWidth, appSizeHeight, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics gr = Graphics.FromImage(bmp))
                        {
                            gr.CopyFromScreen(appPointX, appPointY, 0, 0, bmp.Size);
                        }
                        printImg(bmp, imgSpeed);
                        //bmp.Save("D:\\CopyFromScreen.png");
                    }
                }
                else
                {
                    MessageBox.Show("no");
                }
            }
            
            sw.Stop();

            lblSpeed.Content = "CopyFromScreen\t\t" + sw.ElapsedMilliseconds.ToString();
        }

        private void btnSerialGet_Click(object sender, RoutedEventArgs e)
        {
            cbxSerial.ItemsSource = SerialPort.GetPortNames();
        }

        private void btnSerialCon_Click(object sender, RoutedEventArgs e)
        {

            if (!sp.IsOpen)  //시리얼포트가 열려 있지 않으면
            {
                sp.PortName = cbxSerial.Text;  //콤보박스의 선택된 COM포트명을 시리얼포트명으로 지정
                sp.BaudRate = 9600;  //보레이트 변경이 필요하면 숫자 변경하기
                sp.DataBits = 8;
                sp.StopBits = StopBits.One;
                sp.Parity = Parity.None;

                sp.Open();  //시리얼포트 열기

                lblSerialState.Content = cbxSerial.SelectedItem.ToString() + " 포트 연결";
                cbxSerial.IsEnabled = false;
                btnSerialCon.IsEnabled = false;
            }
            else  //시리얼포트가 열려 있으면
            {
                lblSerialState.Content = "포트가 이미 열려 있습니다.";
            }
        }

        private void btnSerialSend_Click(object sender, RoutedEventArgs e)
        {
            if (sp.IsOpen)
            {
                sp.Write(txtSerial.Text);
                lblSerialSendMessage.Content = cbxSerial.SelectedItem.ToString() + " 포트로 전송 : " + txtSerial.Text;
            }
            else
            {
                lblSerialSendMessage.Content = "통신포트가 열리지 않았습니다";
            }
        }

        private void btnSerialDiscon_Click(object sender, RoutedEventArgs e)
        {
            sp.Close();
            cbxSerial.IsEnabled = true;
            btnSerialCon.IsEnabled = true;
        }

        private void txtAppName_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppName = txtAppName.Text;
        }
    }
}
