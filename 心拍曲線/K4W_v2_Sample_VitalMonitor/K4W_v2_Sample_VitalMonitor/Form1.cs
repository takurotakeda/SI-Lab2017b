using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Drawing;                 //【】
using System.Drawing.Imaging;         //【】
using System.Runtime.InteropServices; //【Marshal】
using Microsoft.Kinect;               //【Kinect SDK】
using NTL.KinectLib;                  //【ColorBar】


namespace K4W_v2_Sample_VitalMonitor
{
    public partial class Form1 : Form
    {
        private KinectSensor KS = null;         //【Kinect Sensor】
        private MultiSourceFrameReader mReader; //【Multi】
        private byte[] cXRGB;  //【RGB画像】
        private ushort[] dBuf; //【Depthデータ】
        private byte[] dXRGB;  //【Depth画像】
        private ushort[] iBuf; //【IRデータ】
        private byte[] iXRGB;  //【IR画像】
        private byte[] bBuf;   //【BodyIndexデータ】255:None, 0-254:Human
        private byte[] bXRGB;  //【BodyIndex画像】
        private Body[] bodies; //【骨格データ】
        private Body body;     //【骨格データ】選択されたデータ
        private int Dx, Dy, Cx, Cy;
        private Rectangle cRect, dRect;
        private Bitmap cBmp, dBmp, iBmp, bBmp;
        private const int nColors = 3501; 
        private ColorBar dBar = new ColorBar();
        private ColorBar iBar = new ColorBar();
        private System.TimeSpan pctime, ctime; //【FPS計算用】
        private float fps;                     //【FPS計算値】
        private int fSwitch = 1; //【表示切替Switch】1:Depth, 2:Infrared, 3:BodyIndex
        private CameraSpacePoint[] jPos = new CameraSpacePoint[6];
        private ColorSpacePoint[] cPix = new ColorSpacePoint[6];
        private DepthSpacePoint[] dPix = new DepthSpacePoint[6];
        private Rectangle cfRect, cbRect, dfRect, dbRect;
        private float cFace, iFace, dBreath;
        private float cFout, iFout, dFout, hFout;
        private int ncFilter = 10;
        private int niFilter = 10;
        private int ndFilter = 20;
        private int nhFilter = 10;
        private NoiseReduction cFilter = new NoiseReduction();
        private NoiseReduction iFilter = new NoiseReduction();
        private NoiseReduction dFilter = new NoiseReduction();
        private NoiseReduction hFilter = new NoiseReduction();
        private const int nFIFO = 600;
        private const int ncLevel = 150;
        private const int niLevel = 45;
        private const int ndLevel = 150;
        private const int nhLevel = 45;
        private const int nH = 360;
        private FIFO cWave = new FIFO(); //【HeartBeat】カラー画像
        private FIFO iWave = new FIFO(); //【HeartBeat】赤外線画像
        private FIFO dWave = new FIFO(); //【Breathing】デプス画像
        private FIFO hWave = new FIFO(); //【HeartBeat】デプス画像

        //【計測】
        private Boolean fSampling = false;
        private const int nSample = 32000; //【最大測定時間】30回/秒x60秒x17.5分=31500サンプル
        private int eSample = 30 * 60 * 1; //【デフォルト測定時間】１分間
        private int pSample = 0;
        private int[] time = new int[nSample];
        private float[] cAD = new float[nSample];
        private float[] iAD = new float[nSample];
        private float[] dAD = new float[nSample];
        private float[] hAD = new float[nSample];
        private System.TimeSpan mtime; //【時刻計測用】

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.KeyPreview = true;
            this.KeyPress += Form1_KeyPress;
            this.FormClosing += Form1_FormClosing;

            this.radioButton1.Click += DisplayOption_Click;
            this.radioButton2.Click += DisplayOption_Click;
            this.radioButton3.Click += DisplayOption_Click;

            this.timer1.Interval = 500;
            this.timer1.Enabled = true;
            this.timer1.Tick += timer1_Tick;
        }

        //********************************************************
        //【0.5秒タイマー処理】
        //********************************************************
        void timer1_Tick(object sender, EventArgs e)
        {
            string s = "ColorImage: " + cFace.ToString() + "\n"
                        + "InfraredImage: " + iFace.ToString() + "\n"
                        + "DepthImage: " + dBreath.ToString() + "\n";
            s += "FPS:" + fps.ToString() + "\n";
            if (fps < 20f)
            {
                s += "【警告】照明が暗過ぎます。\n";
            }
            if(fSampling==true)
            {
                s += "計測中: No." + pSample.ToString() + "\n";
            }
            label1.Text = s;
        }

        //********************************************************
        //【Kinect初期化処理】
        //********************************************************
        void Form1_Load(object sender, EventArgs e)
        {
            KS = KinectSensor.GetDefault(); //【KinectV2センサ取得】
            if (KS == null) { return; }

            FrameDescription fd; //【FrameDescription】
            fd = KS.ColorFrameSource.FrameDescription;
            Cx = fd.Width; Cy = fd.Height;
            fd = KS.DepthFrameSource.FrameDescription;
            Dx = fd.Width; Dy = fd.Height;

            //【カラー】
            cRect = new Rectangle(0, 0, Cx, Cy);
            cBmp = new Bitmap(Cx, Cy, PixelFormat.Format32bppRgb);
            cXRGB = new byte[Cx * Cy * 4];
            //【デプス】
            dRect = new Rectangle(0, 0, Dx, Dy);
            dBmp = new Bitmap(Dx, Dy, PixelFormat.Format32bppRgb);
            dBuf = new ushort[Dx * Dy];
            dXRGB = new byte[Dx * Dy * 4];
            dBar.CreateColorPalletForDistance(nColors);

            //【赤外線】
            iBmp = new Bitmap(Dx, Dy, PixelFormat.Format32bppRgb);
            iBuf = new ushort[Dx * Dy];
            iXRGB = new byte[Dx * Dy * 4];
            iBar.CreateColorPalletForInfrared2();

            //【人検出】
            bBmp = new Bitmap(Dx, Dy, PixelFormat.Format32bppRgb);
            bBuf = new byte[Dx * Dy];
            bXRGB = new byte[Dx * Dy * 4];

            //【骨格トラッキング】
            bodies = new Body[KS.BodyFrameSource.BodyCount];

            this.pictureBox1.BorderStyle = BorderStyle.None;             //【カラー画像表示】
            this.pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage; //
            this.pictureBox2.BorderStyle = BorderStyle.None;             //【デプス、赤外線、BodyIndex表示】
            this.pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage; //
            this.pictureBox3.BorderStyle = BorderStyle.None;             //【波形表示用】
            this.pictureBox3.SizeMode = PictureBoxSizeMode.StretchImage; //

            pctime = new TimeSpan(0); //【FPS計算用】

            mReader = KS.OpenMultiSourceFrameReader(
                 FrameSourceTypes.Color
               | FrameSourceTypes.Depth
               | FrameSourceTypes.Infrared
               | FrameSourceTypes.BodyIndex
               | FrameSourceTypes.Body);
            mReader.MultiSourceFrameArrived += mReader_MultiSourceFrameArrived;
            KS.Open();

            cFilter.Initialize(ncFilter);
            iFilter.Initialize(niFilter);
            dFilter.Initialize(ndFilter);
            hFilter.Initialize(nhFilter);
            cWave.Initialize(nFIFO, ncLevel, nH, 2.0f);
            iWave.Initialize(nFIFO, niLevel, nH, 100f);
            dWave.Initialize(nFIFO, ndLevel, nH, 20f);
            hWave.Initialize(nFIFO, nhLevel, nH, 2f);
        }

        //********************************************************
        //【Escキー処理】
        //********************************************************
        void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 27) { this.Close(); } //【Escキーでプログラム終了】
            if (e.KeyChar == 49) { fSwitch = 1; radioButton1.Checked = true; }  //【１】デプス
            if (e.KeyChar == 50) { fSwitch = 2; radioButton2.Checked = true; }  //【２】赤外線
            if (e.KeyChar == 51) { fSwitch = 3; radioButton3.Checked = true; }  //【３】人検出
        }

        //********************************************************
        //【RadioButton_Click処理】
        //********************************************************
        void DisplayOption_Click(object sender, EventArgs e)
        {
            fSwitch = 0;
            if (radioButton1.Checked) { fSwitch = 1; }
            if (radioButton2.Checked) { fSwitch = 2; }
            if (radioButton3.Checked) { fSwitch = 3; }
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
        //【Frame取得イベント】
        //********************************************************
        void mReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            int fDepth = 0;
            int fColor = 0;

            MultiSourceFrame mFrame = e.FrameReference.AcquireFrame();

            //【カラーフレーム取得】
            using (ColorFrame cFrame = mFrame.ColorFrameReference.AcquireFrame())
            {
                if (cFrame != null)
                {
                    fColor++;
                    cFrame.CopyConvertedFrameDataToArray(cXRGB, ColorImageFormat.Bgra);
                    /*
                    BitmapData cBitmapData = cBmp.LockBits(cRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                    Marshal.Copy(cXRGB, 0, cBitmapData.Scan0, cXRGB.Length);
                    cBmp.UnlockBits(cBitmapData);
                    pictureBox1.Image = cBmp;
                    */
                    //【FPS計算】
                    ctime = cFrame.RelativeTime;
                    fps = 10000000.0f / (float)((ctime - pctime).Ticks);
                    pctime = ctime;
                    //【FPS表示】
                    /*
                    Graphics g = Graphics.FromImage(pictureBox1.Image);
                    SolidBrush br = new SolidBrush(Color.Red);
                    Font ft = new System.Drawing.Font("Times New Roman", 40.0f);
                    g.DrawString("FPS=" + fps.ToString(), ft, br, 10, 10);
                    if(fps<20f)
                    {
                        g.DrawString("【警告】照明が暗過ぎます。", ft, br, 10, 55);
                    }
                    g.Dispose();
                     */ 
                }
            }

            //【デプスフレーム取得】
            using (DepthFrame dFrame = mFrame.DepthFrameReference.AcquireFrame())
            {
                if (dFrame != null)
                {
                    mtime = dFrame.RelativeTime;
                    fDepth++;
                    dFrame.CopyFrameDataToArray(dBuf);
                    if (fSwitch == 1)
                    {
                        ConvertDepthData();
                        BitmapData dBitmapData = dBmp.LockBits(dRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                        Marshal.Copy(dXRGB, 0, dBitmapData.Scan0, dXRGB.Length);
                        dBmp.UnlockBits(dBitmapData);

                        pictureBox2.Image = dBmp;
                    }
                }
            }

            //【赤外線フレーム取得】
            using (InfraredFrame iFrame = mFrame.InfraredFrameReference.AcquireFrame())
            {
                if (iFrame != null)
                {
                    fDepth++;
                    iFrame.CopyFrameDataToArray(iBuf);
                    if (fSwitch == 2)
                    {
                        ConvertInfraredData();
                        BitmapData iBitmapData = iBmp.LockBits(dRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                        Marshal.Copy(iXRGB, 0, iBitmapData.Scan0, iXRGB.Length);
                        iBmp.UnlockBits(iBitmapData);
                        pictureBox2.Image = iBmp;
                    }
                }
            }

            //【人体検出フレーム取得】
            using (BodyIndexFrame bFrame = mFrame.BodyIndexFrameReference.AcquireFrame())
            {
                if (bFrame != null)
                {
                    fDepth++;
                    bFrame.CopyFrameDataToArray(bBuf);
                    if (fSwitch == 3)
                    {
                        ConvertBodyIndexData();
                        BitmapData bBitmapData = bBmp.LockBits(dRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                        Marshal.Copy(bXRGB, 0, bBitmapData.Scan0, bXRGB.Length);
                        bBmp.UnlockBits(bBitmapData);
                        pictureBox2.Image = bBmp;
                    }
                }
            }
            

            //【骨格トラッキングフレーム取得】
            using (BodyFrame bodyFrame = mFrame.BodyFrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    CameraSpacePoint head;
                    float d, dMax;
                    int i, j;

                    try
                    {
                        bodyFrame.GetAndRefreshBodyData(bodies);
                        j = -1;
                        dMax = -1;
                        for (i = 0; i < bodies.Length; i++)
                        {
                            if (bodies[i].IsTracked)
                            {
                                head = bodies[i].Joints[JointType.Head].Position;
                                d = (float)Math.Sqrt(head.X * head.X + head.Y * head.Y + head.Z * head.Z);
                                if (d > dMax) { j = i; dMax = d; }
                            }
                        }
                        if (j >= 0)
                        {
                            //【測定対象の骨格情報の取得】
                            body = bodies[j];
                            jPos[0] = body.Joints[JointType.Head].Position;          //【頭】
                            jPos[1] = body.Joints[JointType.Neck].Position;          //【首】
                            jPos[2] = body.Joints[JointType.SpineShoulder].Position; //【背骨肩】
                            jPos[3] = body.Joints[JointType.SpineMid].Position;      //【背骨中央】
                            jPos[4] = body.Joints[JointType.ShoulderLeft].Position;  //【左肩】
                            jPos[5] = body.Joints[JointType.ShoulderRight].Position; //【右肩】
                            KS.CoordinateMapper.MapCameraPointsToColorSpace(jPos, cPix); //【RGBカメラ座標に変換】
                            KS.CoordinateMapper.MapCameraPointsToDepthSpace(jPos, dPix); //【Depth画像座標に変換】

                            //【カラー画像上の測定位置】
                            j = (int)(cPix[1].Y - cPix[0].Y);
                            try { cfRect = new Rectangle((int)(cPix[0].X - j / 2), (int)cPix[0].Y, j, j); }
                            catch { }
                            i = (int)(cPix[5].X - cPix[4].X);
                            j = (int)(cPix[3].Y - cPix[2].Y);
                         // j = (int)(cPix[3].Y - 0.5f * (cPix[4].Y + cPix[5].Y));
                            try { cbRect = new Rectangle((int)cPix[4].X, (int)cPix[2].Y, i, j); }
                            catch { }

                            //【デプス画像上の測定位置】
                            j = (int)(dPix[1].Y - dPix[0].Y);
                            try { dfRect = new Rectangle((int)(dPix[0].X - j / 2), (int)dPix[0].Y, j, j); }
                            catch { }
                            i = (int)(dPix[5].X - dPix[4].X);
                            j = (int)(dPix[3].Y - dPix[2].Y);
                            // j = (int)(dPix[3].Y - 0.5f*(dPix[4].Y + dPix[5].Y));
                            try { dbRect = new Rectangle((int)dPix[4].X, (int)dPix[2].Y, i, j); }
                            catch { }

                            if ((cfRect.Width * cfRect.Height > 0)
                                && (cbRect.Width * cbRect.Height > 0)
                                && (dfRect.Width * dfRect.Height > 0)
                                && (dbRect.Width * dbRect.Height > 0))
                            {
                                fDepth++;
                                try 
                                {
                                    /*
                                    Pen p1 = new Pen(Color.Yellow, 10.0f);
                                    Graphics g1 = Graphics.FromImage(pictureBox1.Image);
                                    g1.DrawRectangle(p1, cfRect);
                                    g1.DrawRectangle(p1, cbRect);
                                    g1.Dispose();
                                    */
                                    Pen p2 = new Pen(Color.Yellow, 5.0f);
                                    Graphics g2 = Graphics.FromImage(pictureBox2.Image);
                                    g2.DrawRectangle(p2, dfRect);
                                    g2.DrawRectangle(p2, dbRect);
                                    g2.Dispose();
                                }
                                catch
                                { }
                            }
                        }
                    }
                    catch
                    {
                        //【骨格が取得できない場合】
                    }
                }

            }
            
            if (fDepth == 4)
            {
                Bitmap Bmp3 = new Bitmap(nFIFO, nH, PixelFormat.Format32bppRgb);
                Graphics g3 = Graphics.FromImage(Bmp3);
                Pen p3;

                dBreath = GetDepthAverage(dbRect); //【デプス画像枠内加算平均】
                dFilter.Estimation(dBreath);       //【デプス画像基底遷移】
                hFilter.Estimation(dBreath);       //【デプス画像基底遷移】
                dFout = dFilter.C;                 //　：
                hFout = hFilter.C;
                dWave.LoadDataToFIFO(dFout);
             //   hWave.LoadDataToFIFO(hFout - dFout);
             //   p3 = new Pen(Color.Pink, 5f);
             //   g3.DrawLines(p3, hWave.Points);

                if (fColor == 1) //【カラーフレーム】
                {
                    cFace = GetColorAverage(cfRect); //【カラー画像枠内加算平均】
                    cFilter.Estimation(cFace);       //【カラー画像基底遷移】
                    cFout = cFilter.C;               //　：
                    cWave.LoadDataToFIFO(cFout);
                    p3 = new Pen(Color.Red, 3f);    
                    g3.DrawLines(p3, cWave.Points);
                }
                iFace = GetInfraredAverage(dfRect); //【赤外線画像枠内加算平均】
                iFilter.Estimation(iFace);          //【赤外線画像基底遷移】
                iFout = iFilter.C;                  //　：
              //  iWave.LoadDataToFIFO(iFout);
              //  p3 = new Pen(Color.Yellow, 3f);
              //  g3.DrawLines(p3, iWave.Points);

                p3 = new Pen(Color.LightGreen, 5f);
                g3.DrawLines(p3, dWave.Points);

                g3.Dispose();
                pictureBox3.Image = Bmp3;

                //【計測】
                if (fSampling)
                {
                    time[pSample] = (int)(mtime.Ticks * 0.0001); //【ミリ秒】
                    cAD[pSample] = cFout;
                    iAD[pSample] = iFout;
                    dAD[pSample] = dFout;
                    hAD[pSample] = hFout;
                    pSample++;
                }

                if (((fSampling==true) && (pSample >= eSample)) || ((fSampling == false) && (pSample > 600))) //【計測終了時】
                {
                    string t = "time[ms], HeartBeat(RGB), HeartBeat(IR), HeartBeat(depth), Breathing(depth)\n";
                    for (int i = 0; i < pSample; i++)
                    {
                        t += (time[i]*0.001).ToString() + ", "
                            + cAD[i].ToString() + ", "
                            + iAD[i].ToString() + ", "
                            + hAD[i].ToString() + ", "
                            + dAD[i].ToString() + "\n";
                    }

                    //【ファイル作成】
                    DateTime dtNow = DateTime.Now; // 現在の日付と時刻を取得する
                    String filename = @"../../Data/VitalData"
                                    + dtNow.Year.ToString("0000")
                                    + dtNow.Month.ToString("00")
                                    + dtNow.Day.ToString("00") + "_"
                                    + dtNow.Hour.ToString("00")
                                    + dtNow.Minute.ToString("00")
                                    + dtNow.Second.ToString("00")
                                    + ".csv";
                    System.IO.StreamWriter sw = new System.IO.StreamWriter(
                        filename,
                        false,
                        System.Text.Encoding.GetEncoding("shift_jis"));
                    sw.Write(t);
                    sw.Close();  //閉じる

                    fSampling = false;
                    pSample = 0;
                    button1.Text = "計測開始";
                }
            }

        }

        //********************************************************
        //【デプス画像のカラー化】DepthFIFOの処理も含む(ポインタ更新を除く)
        //********************************************************
        void ConvertDepthData()
        {
            int j = 0; ushort k;
            for (int i = 0; i < dBuf.Length; i++)
            {
                k = dBuf[i]; // DataIn
                if ((k < 500) || (k > 3500)) { k = 0; }
                dXRGB[j] = dBar.B[k];
                dXRGB[j + 1] = dBar.G[k];
                dXRGB[j + 2] = dBar.R[k];
                dXRGB[j + 3] = 0;
                j += 4;
            }
        }

        //********************************************************
        //【デプス画像のカラー化】DepthFIFOの処理も含む(ポインタ更新を除く)
        //********************************************************
        void ConvertInfraredData()
        {
            int j = 0; ushort k;
            for (int i = 0; i < iBuf.Length; i++)
            {
                k = iBuf[i]; // DataIn
                iXRGB[j] = iBar.B[k];
                iXRGB[j + 1] = iBar.G[k];
                iXRGB[j + 2] = iBar.R[k];
                iXRGB[j + 3] = 0;
                j += 4;
            }
        }

        //********************************************************
        //【デプス画像のカラー化】DepthFIFOの処理も含む(ポインタ更新を除く)
        //********************************************************
        void ConvertBodyIndexData()
        {
            Color[] BodyColor = new Color[] { Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Blue, Color.Violet };
            int j = 0; ushort k;
            for (int i = 0; i < bBuf.Length; i++)
            {
                k = bBuf[i]; // DataIn
                if (k < KS.BodyFrameSource.BodyCount)
                {
                    bXRGB[j] = BodyColor[k].B;
                    bXRGB[j + 1] = BodyColor[k].G;
                    bXRGB[j + 2] = BodyColor[k].R;
                }
                else
                {
                    bXRGB[j] = bXRGB[j + 1] = bXRGB[j + 2] = 0;
                }
                bXRGB[j + 3] = 0;
                j += 4;
            }
        }

        //********************************************************
        //【カラー枠内加算平均】
        //********************************************************
        float GetColorAverage(Rectangle r)
        {
            if ((r.X < 0) || (r.Y < 0) || (r.Width < 1) || (r.Height < 1)) { return 0.0f; }
            if ((r.X + r.Width >= Cx) || (r.Y + r.Height >= Cy)) { return 0.0f; }

            const float ThresholdColor = 100f;
            float Sum, Intensity;
            int i, j, X, Y, Count;
            Count = 0;
            Sum = 0.0f;
            for (Y = r.Y; Y < r.Y + r.Height; Y++)
            {
                j = Y * Cx;
                for (X = r.X; X < r.X + r.Width; X++)
                {
                    i = 4*(j + X);
                    Intensity = 0.299f * cXRGB[i + 2] + 0.587f * cXRGB[i + 1] + 0.114f * cXRGB[i];
                    if (Intensity >= ThresholdColor)
                    {
                        Count++;
                        Sum += Intensity;
                    }
                }
            }
            if (Count > 0) { Sum = Sum / (float)Count; } else { Sum=0.0f; }
            return Sum;
        }

        //********************************************************
        //【赤外線枠内加算平均】
        //********************************************************
        float GetInfraredAverage(Rectangle r)
        {
            if ((r.X < 0) || (r.Y < 0) || (r.Width < 1) || (r.Height < 1)) { return 0.0f; }
            if ((r.X + r.Width >= Dx) || (r.Y + r.Height >= Dy)) { return 0.0f; }

            const float ThresholdInfrared = 1000;
            float Sum, Intensity;
            int i, j, X, Y, Count;
            Count = 0;
            Sum = 0.0f;
            for (Y = r.Y; Y < r.Y + r.Height; Y++)
            {
                j = Y * Dx;
                for (X = r.X; X < r.X + r.Width; X++)
                {
                    i = j + X;
                    Intensity = iBuf[i];
                    if ((Intensity >= ThresholdInfrared) && (bBuf[i] < 6))
                    {
                        Count++;
                        Sum += Intensity;
                    }
                }
            }
            if (Count > 0) { Sum = Sum / (float)Count; } else { Sum = 0.0f; } 
            return Sum;
        }

        //********************************************************
        //【デプス枠内加算平均】
        //********************************************************
        float GetDepthAverage(Rectangle r)
        {
            if ((r.X < 0) || (r.Y < 0) || (r.Width < 1) || (r.Height < 1)) { return 0.0f; }
            if ((r.X + r.Width >= Dx) || (r.Y + r.Height >= Dy)) { return 0.0f; }

            const float ThresholdDepth1 = 500;  //【最小  50cm】
            const float ThresholdDepth2 = 2000; //【最大 200cm】
            float Sum, Distance;
            int i, j, X, Y, Count;
            Count = 0;
            Sum = 0.0f;
            for (Y = r.Y; Y < r.Y + r.Height; Y++)
            {
                j = Y * Dx;
                for (X = r.X; X < r.X + r.Width; X++)
                {
                    i = j + X;
                    Distance = dBuf[i];
                    if ((Distance >= ThresholdDepth1) && (Distance <= ThresholdDepth2) && (bBuf[i] < 6))
                    {
                        Count++;
                        Sum += Distance;
                    }
                }
            }
            if (Count > 0) { Sum = Sum / (float)Count; } else { Sum = 0.0f; }
            return Sum;
        }

        //********************************************************
        //【計測】
        //********************************************************
        private void button1_Click(object sender, EventArgs e)
        {
            string s = "";
            if(fSampling==false)
            {
                //【計測開始】
                mtime = new TimeSpan(0);
                fSampling = true;
                pSample = 0;
                eSample = int.Parse(textBox1.Text) * 30;
                s = "計測中断";
            }
            else 
            {
                //【計測中断】
                fSampling = false;
                s = "計測開始";
            }
            button1.Text = s;
        }
    }
}
