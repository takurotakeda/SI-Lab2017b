using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using Microsoft.Kinect;       //【Kinect SDK】
using NTL.KinectLib;          //【NoiseReduction】

namespace K4W_v2_Sample_Acceleration
{
    public partial class Form1 : Form
    {
        private KinectSensor KS = null;   //【Kinect Sensor】
        private FrameDescription fd;      //【FrameDescription】
        private MultiSourceFrameReader multiReader = null;
        private Bitmap cBmp;
        private Rectangle cRect;
        private uint cSize;

        //【骨格トラッキング】
        private Body[] bodies;
        private Body body;
        private CameraSpacePoint[] pos = new CameraSpacePoint[25];
        private ColorSpacePoint[] pix = new ColorSpacePoint[25];

        //【残像FIFO】
        private const int nFIFO = 10; //【２秒間の残像】
        private int pFIFO = 0;
        private Boolean fFIFO = false;
        private ColorSpacePoint[] FIFO1 = new ColorSpacePoint[nFIFO];
        private ColorSpacePoint[] FIFO2 = new ColorSpacePoint[nFIFO];
        private System.Drawing.PointF[] pos1 = new System.Drawing.PointF[nFIFO];
        private System.Drawing.PointF[] pos2 = new System.Drawing.PointF[nFIFO];
        private NoiseReduction[] NR = new NoiseReduction[6];
        private const int nSample = 600;
        private FIFO[] wave = new FIFO[2];
        private System.Drawing.PointF[] Acc = new System.Drawing.PointF[nSample];

        public Form1()
        {
            InitializeComponent();

            this.KeyPreview = true;
            this.KeyPress += Form1_KeyPress;
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
        }

        //********************************************************
        //【Kinect初期化処理】
        //********************************************************
        void Form1_Load(object sender, EventArgs e)
        {
            KS = KinectSensor.GetDefault(); //【KinectV2センサ取得】
            if (KS == null) { return; }

            fd = KS.ColorFrameSource.FrameDescription;
            cBmp = new Bitmap(fd.Width, fd.Height, PixelFormat.Format32bppRgb);
            cRect = new Rectangle(0, 0, fd.Width, fd.Height);
            cSize = (uint)(fd.Width * fd.Height * 4); //【注意】fd.BytesPerPixel=2(圧縮)になっている。
            this.BackgroundImage = cBmp;
            this.BackgroundImageLayout = ImageLayout.Stretch;
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.pictureBox1.Top = 0;
            this.pictureBox1.Left = 0;
            this.pictureBox1.Width = this.Width;
            this.pictureBox1.Height = this.Width * fd.Height / fd.Width;
            this.pictureBox1.BorderStyle = BorderStyle.None;
            this.pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;

            multiReader = KS.OpenMultiSourceFrameReader(
                FrameSourceTypes.Color |
                FrameSourceTypes.Body);
            multiReader.MultiSourceFrameArrived += multiReader_MultiSourceFrameArrived;
            bodies = new Body[KS.BodyFrameSource.BodyCount];
            KS.Open();

            for(int i=0; i<NR.Length; i++)
            {
                NR[i] = new NoiseReduction();
                NR[i].Initialize(nFIFO);
            }
            for(int i=0; i<wave.Length; i++)
            {
                wave[i] = new FIFO();
                wave[i].Initialize(nSample, (int)(nSample/4));
            }
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
            if (multiReader != null) { multiReader.Dispose(); multiReader = null; }
            if (KS != null) { KS.Close(); KS = null; }
        }


        //********************************************************
        //【Form終了処理】
        //********************************************************
        void multiReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame mFrame = e.FrameReference.AcquireFrame();

            using (ColorFrame cFrame = mFrame.ColorFrameReference.AcquireFrame())
            {
                if (cFrame != null)
                {
                    BitmapData cBitmapData = cBmp.LockBits(cRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                    cFrame.CopyConvertedFrameDataToIntPtr(cBitmapData.Scan0, cSize, ColorImageFormat.Bgra);
                    cBmp.UnlockBits(cBitmapData);
                    pictureBox1.Image = cBmp;
                }
            }

            using (BodyFrame bFrame = mFrame.BodyFrameReference.AcquireFrame())
            {
                if (bFrame != null)
                {
                    bFrame.GetAndRefreshBodyData(bodies);
                    for (int i = 0; i < bodies.Length; i++)
                    {
                        if (bodies[i].IsTracked)
                        {
                            body = bodies[i];
                            pos[0] = body.Joints[JointType.Head].Position;           //【頭】
                            pos[1] = body.Joints[JointType.Neck].Position;           //【首】
                            pos[2] = body.Joints[JointType.SpineShoulder].Position;  //【背骨肩】
                            pos[3] = body.Joints[JointType.SpineMid].Position;       //【背骨中央】
                            pos[4] = body.Joints[JointType.SpineBase].Position;      //【背骨基準】
                            pos[5] = body.Joints[JointType.ShoulderLeft].Position;   //【左肩】
                            pos[6] = body.Joints[JointType.ElbowLeft].Position;      //【左肘】
                            pos[7] = body.Joints[JointType.WristLeft].Position;      //【左手首】
                            pos[8] = body.Joints[JointType.HandLeft].Position;       //【左手】
                            pos[9] = body.Joints[JointType.HandTipLeft].Position;    //【左親指】
                            pos[10] = body.Joints[JointType.ThumbLeft].Position;     //【左残り指】
                            pos[11] = body.Joints[JointType.ShoulderRight].Position; //【右肩】
                            pos[12] = body.Joints[JointType.ElbowRight].Position;    //【右肘】
                            pos[13] = body.Joints[JointType.WristRight].Position;    //【右手首】
                            pos[14] = body.Joints[JointType.HandRight].Position;     //【右手】
                            pos[15] = body.Joints[JointType.HandTipRight].Position;  //【右親指】
                            pos[16] = body.Joints[JointType.ThumbRight].Position;    //【右残り指】
                            pos[17] = body.Joints[JointType.HipLeft].Position;       //【左尻】
                            pos[18] = body.Joints[JointType.KneeLeft].Position;      //【左膝】
                            pos[19] = body.Joints[JointType.AnkleLeft].Position;     //【左踝】
                            pos[20] = body.Joints[JointType.FootLeft].Position;      //【左足】
                            pos[21] = body.Joints[JointType.HipRight].Position;      //【右尻】
                            pos[22] = body.Joints[JointType.KneeRight].Position;     //【右膝】
                            pos[23] = body.Joints[JointType.AnkleRight].Position;    //【右踝】
                            pos[24] = body.Joints[JointType.FootRight].Position;     //【右足】
                            KS.CoordinateMapper.MapCameraPointsToColorSpace(pos, pix); //【RGBカメラ座標に変換】

                            //【FIFO処理】
                            FIFO1[pFIFO] = pix[8];  //【左手】
                            FIFO2[pFIFO] = pix[14]; //【右手】
                            pFIFO++;
                            if (pFIFO == nFIFO) { pFIFO = 0; fFIFO = true; }

                            //【位置情報格納】
                            float A, Ax, Ay, Az;
                            NR[0].Estimation(pos[8].X); Ax = 2f * NR[0].A;
                            NR[1].Estimation(pos[8].Y); Ay = 2f * NR[1].A;
                            NR[2].Estimation(pos[8].Z); Az = 2f * NR[2].A;
                            A = 900f * (float)Math.Sqrt(Ax * Ax + Ay * Ay + Az * Az);
                            wave[0].LoadDataToFIFO(A);

                            NR[3].Estimation(pos[14].X); Ax = 2f * NR[3].A;
                            NR[4].Estimation(pos[14].Y); Ay = 2f * NR[4].A;
                            NR[5].Estimation(pos[14].Z); Az = 2f * NR[5].A;
                            A = 900f * (float)Math.Sqrt(Ax * Ax + Ay * Ay + Az * Az);
                            wave[1].LoadDataToFIFO(A);

                            //【残像前処理】
                            int k = pFIFO;
                            for (int j = 0; j < nFIFO; j++)
                            {
                                pos1[j].X = FIFO1[k].X;
                                pos1[j].Y = FIFO1[k].Y;
                                pos2[j].X = FIFO2[k].X;
                                pos2[j].Y = FIFO2[k].Y;
                                k++;
                                if (k == nFIFO) { k = 0; }
                            }



                            if(fFIFO)
                            {
                                Graphics g1 = Graphics.FromImage(pictureBox1.Image);

                                //【残像曲線を描画する】
                                Pen p1 = new Pen(Color.FromArgb(200, Color.Red), 20.0f);
                                Pen p2 = new Pen(Color.FromArgb(200, Color.LightGreen), 20.0f);
                                g1.DrawCurve(p1, pos1);
                                g1.DrawCurve(p2, pos2);

                                //【グリッド】
                                Pen pg = new Pen(Color.Pink, 6f);
                                int Ofs = 100;
                                for (int j = Ofs + 0; j < Ofs + 601; j += 60)
                                {
                                    g1.DrawLine(pg, Ofs + 0, j, Ofs + 601, j);
                                    g1.DrawLine(pg, j, Ofs + 0, j, Ofs + 601);
                                }
                                Font ft = new Font("Times New Roman", 30f);
                                SolidBrush br = new SolidBrush(Color.Yellow);
                                g1.DrawString("【３軸加速度⇒衝撃】",ft,br,(float)Ofs,(float)(Ofs+nSample));
                                
                                //【加速度描画】
                                Pen pw1 = new Pen(Color.FromArgb(200, Color.Red), 5.0f);
                                Pen pw2 = new Pen(Color.FromArgb(200, Color.LightGreen), 5.0f);
                                k = wave[0].Pointer;
                                for (int j = 0; j < nSample; j++)
                                {
                                    Acc[j].X = (float)(Ofs + j);
                                    Acc[j].Y = (float)Ofs + 100f * (6f - (float)wave[0].dFIFO[k]);
                                    k++;
                                    if (k == nSample) { k = 0; }
                                }
                                g1.DrawCurve(pw1, Acc);

                                k = wave[1].Pointer;
                                for (int j = 0; j < nSample; j++)
                                {
                                    Acc[j].X = (float)(Ofs + j);
                                    Acc[j].Y = (float)Ofs + 100f * (6f - (float)wave[1].dFIFO[k]);
                                    k++;
                                    if (k == nSample) { k = 0; }
                                }
                                g1.DrawCurve(pw2, Acc);

                                g1.Dispose();
                            }
                            break;
                        }
                    }
                }
            }
        }
    }
}
