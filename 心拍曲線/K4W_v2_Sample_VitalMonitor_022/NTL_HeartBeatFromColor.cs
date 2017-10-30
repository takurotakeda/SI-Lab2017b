using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;         //【Bitmap】
using System.Drawing.Drawing2D;       //【Bitmap】
using System.Runtime.InteropServices; //【Marshal】【DllImport】
using Microsoft.Kinect;

namespace NTL.KinectLib
{
    public class HeartBeatFromColor
    {
        public int Width;         // Colorカメラの水平方向画素数: 1920px
        public int Height;        // Colorカメラの垂直方向画素数: 1080px
        public byte[] cXRGB;      // Colorカメラから読み込んだXRGB形式の生データ 
        public Bitmap cBmp;       // BitMap形式(XRGB)に変換したColorカメラ画像
        public Rectangle cRect;   // BitMap形式に変換する際のColorカメラの解像度を定義する矩形枠

        public Rectangle aRect;   // 加算平均用矩形枠
        public double Intensity;  // 輝度値(0～255.0)
        public MovingAverageFIFO DCServo = new MovingAverageFIFO();
        public double Offset;     // Offset値
        public double HeartBeat;  // 心拍変動の生データ

        public NoiseReduction Nr = new NoiseReduction(); //【基底遷移アルゴリズム】Oj = a * t^2 + b * t + c 

        public DisplayFIFO Disp = new DisplayFIFO(); //【波形表示FIFOクラス】

        public RectangleFilter RF = new RectangleFilter(); //【矩形波相関フィルタ】

        public void Initialize(int w, int h, int nOffset, int m, int n, double s, double o)
        {
            //①【ReadColorFrame関連】
            Width = w;
            Height = h;
            cBmp = new Bitmap(Width, Height, PixelFormat.Format32bppRgb);
            cRect = new Rectangle(0, 0, Width, Height);
            cXRGB = new byte[Width * Height * 4];

            //②【Averaging】
            DCServo.Initialize(nOffset);

            //③【基底遷移】
            Nr.Initialize(m);

            //④【矩形波相関フィルタ】
            RF.Initialize(8);

            //⑤【表示用】
            Disp.Initialize(n, s, o);
        }


        //********************************************************
        //①【ReadColorFrame】KinectV2のフルHDカメラから１フレーム分のRGB画像を取得する
        //********************************************************
        public Bitmap ReadColorFrame(ColorFrame cF)
        {
            // 1フレーム分のデータを配列cXRGBに読み込む
            cF.CopyConvertedFrameDataToArray(cXRGB, ColorImageFormat.Bgra);

            // BitMapデータをLockして配列cXRGBのデータをデータ領域にコピーする
            BitmapData cBitmapData = cBmp.LockBits(cRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
            Marshal.Copy(cXRGB, 0, cBitmapData.Scan0, cXRGB.Length);
            cBmp.UnlockBits(cBitmapData);
            return cBmp;
        }

        //********************************************************
        //②【GetHeartBeat】指定矩形枠の輝度の加算平均処理を行う
        //********************************************************
        public void GetHeartBeat(Rectangle r, int sw)
        {
            double cSum; // 輝度値の累積加算値
            double Z;
            int Count;
            aRect = r;
            const double K = 64000.0;
            const double K1 = 100.0 / 64000.0;

            //【矩形枠内の輝度の加算平均値を求める】Intensityは輝度値を64000倍したもの
            cSum = 0.0;
            Count = 0;
            int i, j;
            for (int Y = aRect.Top; Y < aRect.Top + aRect.Height; Y++)
            {
                i = Y * cRect.Width;
                for (int X = aRect.Left; X < aRect.Left + aRect.Width; X++)
                {
                    j = 4 * (i + X);
                    // RGB値から画素の輝度値を求めて累積加算を行う
                    // 輝度値 = 0.299 * R + 0.587 * G + 0.114 * B
                    Z = 0.299 * cXRGB[j + 2] + 0.587 * cXRGB[j + 1] + 0.114 * cXRGB[j];
                    if ((Z > 70.0) && (Z < 250.0)) // 皮膚表面で鏡面反射で白っぽい部分と髪の毛等で暗い部分を排除
                    {
                        cSum += Z;
                        Count++;
                    }
                }
            }
            if (Count > 5)
            {
                Intensity = K * cSum / (double)Count; // 輝度Zの平均値 0.0～255.0
            }
            else 
            {
                Intensity = 0;
            }
            // 心臓拍出圧(高圧)で　毛細血管(動脈部)の吸光度増加⇒反射率低下⇒輝度値低下
            // 心弛緩期(低圧)で　　毛細血管(動脈部)の吸光度減少⇒反射率増加⇒輝度値増加

            Offset = DCServo.LoadDataToFIFO((int)Intensity); // FIFO内部では24bitの整数で取り扱っている。
            HeartBeat = (Intensity - Offset) * K1; // ここで16bit～18bit程度の有効数字を持つ値として抽出できる。

            //【基底遷移アルゴリズム】Oj = a * t^2 + b * t + c
            // ノイズ除去と加速度の推定 2aが加速度だが、30FPSなので、2*30*30=1800で　1800aを用いる
            Nr.Estimation(HeartBeat);
            // 1800a: 加速度
            //   30b: 速度
            //     c: ノイズ除去後の心拍信号

            //【矩形波相関フィルタ】
            RF.GetRectangleFilter(1800.0 * Nr.A);

            switch(sw)
            {
                case 0:
                    Disp.LoadDataToFIFO(HeartBeat);
                    break;
                case 1:
                    Disp.LoadDataToFIFO(Nr.C);
                    break;
                case 2:
                    Disp.LoadDataToFIFO(Nr.A * (1800.0 * 0.1));
                    break;
                case 3:
                    Disp.LoadDataToFIFO(RF.Output * 0.5);
                    break;
            }
        }



        public struct FilterOutput
        {
            public int Time;
            public double Output;
        }

        public struct IrPeak
        {
            public double HeartBeatRate;
            public int pTime;
            public double minValue;
            public int flag;
        }

        private FilterOutput rectOut;
        private IrPeak Peak;

        //*****************************************************
        //【脈拍数検出処理】
        //*****************************************************
        public double GetHeartBeatRate(double curData)
        {

            Peak.HeartBeatRate = 0.0f;
            Peak.pTime = 0;
            Peak.minValue = 0;
            Peak.flag = 0;

            // rectOut.Time  【現在時刻】[ミリ秒]
            // rectOut.Output【現在値】
            int curTime = rectOut.Time;
            double value = curData; //  rectOut.Output;
            if (value > 0)
            {
                Peak.flag = 0;     //【ピーク検出フラグ】0:待機中
                Peak.minValue = 0;
            }
            else //【value≦0】
            {
                if (value > Peak.minValue)
                {
                    curTime = rectOut.Time;                     //【現在時刻を取得する】
                    if (Peak.pTime != 0)
                    {
                        Peak.HeartBeatRate = 60000.0f / (curTime - Peak.pTime);    //【脈拍数を演算する】単位[拍/分]
                    }
                    Peak.pTime = curTime;
                    Peak.minValue = 0;
                    Peak.flag = 1;     //【ピーク検出フラグ】1:検出
                }
                else
                {
                    if (Peak.flag == 0) { Peak.minValue = value; }
                }
            }
            return Peak.HeartBeatRate;
        }
       

    }
}
