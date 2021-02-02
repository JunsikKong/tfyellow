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

namespace tfyellow
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {

        [System.Runtime.InteropServices.DllImport("User32", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

        String AppName = "League of Legends (TM) Client";


        //백그라운드 워커 선언
        private BackgroundWorker myThread;

        public MainWindow()
        {
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
            int count = (int)e.Argument;
            for (int i = 1; i <= count; i++)
            {
                if (myThread.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                else
                {
                    Thread.Sleep(20);
                    if (i % 2 == 0)
                    {
                        //sum += i;
                        //e.Result = sum;
                    }

                }
            }
        }

        //작업완료
        private void myThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled) lblState.Content = "작업 취소";
            else if (e.Error != null) lblState.Content = "오류 발생";
            else
            {
                tblkSum.Text = ((int)e.Result).ToString();
                lblState.Content = "작업 완료";
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            int num;

            if (!int.TryParse(txtNumber.Text, out num)) //숫자가 아닐 경우 반환 num
            {
                lblState.Content = "숫자를 입력하세요";
                return;
            }

            if (myThread.IsBusy)
            {
                lblState.Content = "이미 실행중";
                return;
            }

            lstNumber.Items.Clear();

            myThread.RunWorkerAsync(num);
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
                lblState.Content = "앱플레이어 찾았습니다.";

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
    }
}
