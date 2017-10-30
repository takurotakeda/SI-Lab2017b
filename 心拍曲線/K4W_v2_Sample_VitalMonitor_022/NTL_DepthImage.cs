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
    public class DepthImage
    {
        public int Width;         // Depthカメラの水平方向画素数: 512px
        public int Height;        // Depthカメラの垂直方向画素数: 424px
        public byte[] dXRGB;      // Depthカメラから読み込んだXRGB形式の生データ 
        public Bitmap dBmp;       // BitMap形式(XRGB)に変換したDepthカメラ画像
        public Rectangle dRect;   // BitMap形式に変換する際のDepthカメラの解像度を定義する矩形枠
        public ushort[] dBuf;
        public ColorBar dScale = new ColorBar(); //【デプス値をカラーに変換する為のクラス】
        public const int dMax = 2501; //4096;

        public void Initialize(int w, int h)
        {
            Width = w;
            Height = h;
            dBuf = new ushort[Width * Height];
            dXRGB = new byte[Width * Height * 4];
            dBmp = new Bitmap(Width, Height, PixelFormat.Format32bppRgb);
            dRect = new Rectangle(0, 0, Width, Height);
            dScale.CreateColorPalletForDistance(dMax);
        }

        //********************************************************
        //①【ReadDepthFrame】KinectV2のToFカメラから１フレーム分のRGB画像を取得する
        //********************************************************
        public Bitmap ReadDepthFrame(DepthFrame dF)
        {
            // 1フレーム分のデータを配列dBufに読み込む
            dF.CopyFrameDataToArray(dBuf);

            // 深度値をRGBカラーに変換する
            ConvertDepthToColor();

            // BitMapデータをLockして配列dXRGBのデータをデータ領域にコピーする
            BitmapData dBitmapData = dBmp.LockBits(dRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
            Marshal.Copy(dXRGB, 0, dBitmapData.Scan0, dXRGB.Length);
            dBmp.UnlockBits(dBitmapData);
            return dBmp;
        }

        //********************************************************
        //【ConvertDepthToColor】
        //********************************************************
        private void ConvertDepthToColor()
        {
            int j = 0;
            int d;
            for(int i=0; i<dBuf.Length; i++)
            {
                d = dBuf[i];
                if (d >= dMax) { d = dMax - 1; }
                if (d < 500) { d = 500; }
                dXRGB[j] = dXRGB[j + 1] = dXRGB[j + 2] = dXRGB[j + 3] = 0;
              //  if ((d >= 500) && (d <= 1000)) //【50cm～1ｍの範囲だけを表示】
                if (d <= 1000) //【50cm～1ｍの範囲だけを表示】
                {
                    dXRGB[j] = dScale.B[d];
                    dXRGB[j + 1] = dScale.G[d];
                    dXRGB[j + 2] = dScale.R[d];
                }
                j += 4;
            }
        }
    }
}
