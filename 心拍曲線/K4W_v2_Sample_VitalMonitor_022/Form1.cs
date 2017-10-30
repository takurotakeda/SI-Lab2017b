using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;         //【Bitmap】
using System.Drawing.Drawing2D;       //【Bitmap】
using System.Runtime.InteropServices; //【Marshal】【DllImport】
using Microsoft.Kinect;       //【Kinect SDK】
using NTL.KinectLib;          //【ColorBar】

namespace K4W_v2_Sample_VitalMonitor_022
{
    public partial class Form1 : Form
    {
        private KinectSensor KS = null;                 //【Kinect Sensor】
        private FrameDescription fd = null;             //【FrameDescription】
        private MultiSourceFrameReader mReader = null;  //【MultiSourceFrameReader】

        private HeartBeatFromColor cCam = new HeartBeatFromColor();
        private Rectangle mRect = new Rectangle(800, 300, 320, 280);
        private int Count;
        private int DispSwitch = 1; //【基底遷移アルゴリズムでノイズ除去後の脈波を表示する】

        private DepthImage dCam = new DepthImage();
        private HeartBeatFromInfrared iCam = new HeartBeatFromInfrared();

        public Form1()
        {
            InitializeComponent();

            this.KeyPreview = true;
            this.KeyPress += Form1_KeyPress;
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
            this.radioButton1.CheckedChanged += radioButton1_CheckedChanged;
            this.radioButton2.CheckedChanged += radioButton2_CheckedChanged;
            this.radioButton3.CheckedChanged += radioButton3_CheckedChanged;
            this.radioButton4.CheckedChanged += radioButton4_CheckedChanged;
        }

        //********************************************************
        //【表示する波形を選択する】
        //********************************************************
        void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked) { DispSwitch = 0; }; //【生データを表示する】
        }
        void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked) { DispSwitch = 1; }; //【ノイズ除去後の脈波を表示する】
        }
        void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked) { DispSwitch = 2; }; //【加速度脈波を表示する】
        }
        void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked) { DispSwitch = 3; }; //【矩形波相関フィルタ出力】
        }

        //********************************************************
        //【Kinect初期化処理】
        //********************************************************
        void Form1_Load(object sender, EventArgs e)
        {
            KS = KinectSensor.GetDefault(); //【KinectV2センサ取得】
            if (KS == null) { return; }

            //【Colorカメラ】
            fd = KS.ColorFrameSource.FrameDescription;
            cCam.Initialize(fd.Width, fd.Height, 90, 10, 600, 1.0, 350.0);
            // Offset除去処理時定数：3秒 x 30FPS = 90フレーム
            Count = 0;

            //【Depthカメラ】
            fd = KS.DepthFrameSource.FrameDescription;
            dCam.Initialize(fd.Width, fd.Height);

            //【Infraredカメラ】
            fd = KS.InfraredFrameSource.FrameDescription;
            iCam.Initialize(fd.Width, fd.Height);

            // pictureBox1をスクリーン全体を覆うように張り付け、Imageを伸ばして表示する。
            this.pictureBox1.BorderStyle = BorderStyle.None;
            this.pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            this.pictureBox2.BorderStyle = BorderStyle.None;
            this.pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
            this.pictureBox3.BorderStyle = BorderStyle.None;
            this.pictureBox3.SizeMode = PictureBoxSizeMode.StretchImage;

            // MultiSourceFrameReaderでカラー画像を読み込む
            mReader = KS.OpenMultiSourceFrameReader(
                FrameSourceTypes.Color
              | FrameSourceTypes.Infrared
          //  | FrameSourceTypes.Depth
                );
            mReader.MultiSourceFrameArrived += mReader_FrameArrived; // フレーム毎に発生するイベントとして、cReader_FrameArrivedを使う。
            KS.Open(); // Kinect利用開始
        }

        //********************************************************
        //【Escキー処理】
        //********************************************************
        void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 27) { this.Close(); } //【Escキーでプログラム終了】
        }

        //********************************************************
        //【Form終了処理】
        //********************************************************
        void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (mReader != null) { mReader.Dispose(); mReader = null; }
            if (KS != null) { KS.Close(); KS = null; }
        }

        //********************************************************
        //【フレームデータ取得処理】
        //********************************************************
        void mReader_FrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame mFrame = e.FrameReference.AcquireFrame();

            using (ColorFrame cFrame = mFrame.ColorFrameReference.AcquireFrame())
            {
                if (cFrame != null)
                {
                    pictureBox1.Image = cCam.ReadColorFrame(cFrame); // ColorFrameから１フレーム分のデータを読み込んでBitMap形式に変換
                    cCam.GetHeartBeat(mRect, DispSwitch);

                    //【心拍測定領域枠の描画】
                    Graphics g = Graphics.FromImage(pictureBox1.Image);
                    g.DrawRectangle(new Pen(Color.Yellow, 5.0f), mRect);
                    g.Dispose();

                    //【表示選択した波形の描画】
              //      Bitmap b = new Bitmap(pictureBox3.Width, pictureBox3.Height, PixelFormat.Format32bppRgb);
                    Bitmap b = new Bitmap(610, 700, PixelFormat.Format32bppRgb);
                    pictureBox3.Image = b;
                    g = Graphics.FromImage(pictureBox3.Image);
                    g.DrawLines(new Pen(Color.Red, 3.0f), cCam.Disp.Points);
                    g.Dispose();

                    if (Count == 15) // 30FPSなので0.5秒に1回表示する
                    {
                        label1.Text = "Intensity=" + cCam.Intensity.ToString() + "\nOffset=" + cCam.Offset.ToString() + "\nHeartBeat=" + cCam.HeartBeat.ToString();
                        Count = 0;
                    }
                    Count++;
                }
            }

            using (InfraredFrame iFrame = mFrame.InfraredFrameReference.AcquireFrame())
            {
                if (iFrame != null)
                {
                    pictureBox2.Image = iCam.ReadInfraredFrame(iFrame); // InfraredFrameから１フレーム分のデータを読み込んでBitMap形式に変換
                }
            }

            /*
            using (DepthFrame dFrame = mFrame.DepthFrameReference.AcquireFrame())
            {
                if(dFrame != null)
                {
                    pictureBox2.Image = dCam.ReadDepthFrame(dFrame); // DepthFrameから１フレーム分のデータを読み込んでBitMap形式に変換
                }
            }
            */
        }
    }
}
