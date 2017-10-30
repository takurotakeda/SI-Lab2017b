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
    public class HeartBeatFromInfrared
    {
        public int Width;         // Infraredカメラの水平方向画素数: 512px
        public int Height;        // Infraredカメラの垂直方向画素数: 424px
        public byte[] iXRGB;      // Infraredカメラから読み込んだXRGB形式の生データ 
        public Bitmap iBmp;       // BitMap形式(XRGB)に変換したInfraredカメラ画像
        public Rectangle iRect;   // BitMap形式に変換する際のInfraredカメラの解像度を定義する矩形枠
        public ushort[] iBuf;
        public ColorBar iScale = new ColorBar(); //【デプス値をカラーに変換する為のクラス】

        public void Initialize(int w, int h)
        {
            Width = w;
            Height = h;
            iBuf = new ushort[Width * Height];
            iXRGB = new byte[Width * Height * 4];
            iBmp = new Bitmap(Width, Height, PixelFormat.Format32bppRgb);
            iRect = new Rectangle(0, 0, Width, Height);
            iScale.CreateColorPalletForInfrared2();
        }

        //********************************************************
        //①【ReadInfraredFrame】KinectV2のアクティブ赤外線カメラから１フレーム分のRGB画像を取得する
        //********************************************************
        public Bitmap ReadInfraredFrame(InfraredFrame iFrame)
        {
            // 1フレーム分のデータを配列iBufに読み込む
            iFrame.CopyFrameDataToArray(iBuf);

            // 深度値をRGBカラーに変換する
            ConvertInfraredToColor();

            // BitMapデータをLockして配列dXRGBのデータをデータ領域にコピーする
            BitmapData iBitmapData = iBmp.LockBits(iRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
            Marshal.Copy(iXRGB, 0, iBitmapData.Scan0, iXRGB.Length);
            iBmp.UnlockBits(iBitmapData);
            return iBmp;
        }

        //********************************************************
        //【ConvertDepthToColor】
        //********************************************************
        private void ConvertInfraredToColor()
        {
            int j = 0;
            int d;
            for (int i = 0; i < iBuf.Length; i++)
            {
                d = iBuf[i];
                iXRGB[j] = iScale.B[d];
                iXRGB[j + 1] = iScale.G[d];
                iXRGB[j + 2] = iScale.R[d];
                iXRGB[j + 3] = 0;
                j += 4;
            }
        }
    }
}
