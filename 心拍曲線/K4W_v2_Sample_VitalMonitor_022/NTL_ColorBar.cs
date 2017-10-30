using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;                 //【Bitmap】【Color】
using System.Drawing.Imaging;         //【BitmapData】
using System.Runtime.InteropServices; //【Marshal.Copy】

namespace NTL.KinectLib
{
    //*************************************************************************
    //*************************************************************************
    //【CLASS】『ColorBar』【カラー・ルックアップ・テーブル】
    //*************************************************************************
    //*************************************************************************
    public class ColorBar
    {
        public byte[] A;
        public byte[] R;
        public byte[] G;
        public byte[] B;

        //*******************************************************
        //【CreateColorPalletForInfrared2】
        //*******************************************************
        // 赤外線輝度(16bit/画素)表示用のカラーパレットを作る
        //【色の総数】  65536色
        //【赤外線輝度】0 ⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒ 65535
        //【基本色】　　黒⇒濃灰⇒灰⇒紫⇒藍⇒青⇒淡青⇒緑⇒黄⇒橙⇒赤⇒白
        public void CreateColorPalletForInfrared2()
        {
            int[] Index = new int[]
            {
                0, 5500, 6553, 8191, 9362, 10922, 13107, 16383, 21845, 32767, 65535
            };
            Color[] BC = new Color[]
            {
                Color.Black, Color.LightGray, Color.DarkGray, Color.Violet, Color.DarkBlue, Color.Blue,
                Color.Green, Color.Yellow, Color.Orange, Color.Red, Color.White
            };
            int nColors = 65536;     //【カラーパレットの総数】【2^16 = 65536色】
            GenerateColorPallets(nColors,Index, BC);
        }


        //*******************************************************
        //【CreateColorPalletForInfrared】
        //*******************************************************
        // 赤外線輝度(16bit/画素)表示用のカラーパレットを作る
        //【色の総数】  65536色
        //【赤外線輝度】0 ⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒ 65535
        //【基本色】　　黒⇒濃灰⇒灰⇒紫⇒藍⇒青⇒淡青⇒緑⇒黄⇒橙⇒赤⇒白
        public void CreateColorPalletForInfrared()
        {
            Color[] BC = new Color[]
            {
                Color.Black, Color.DarkGray, Color.Violet, Color.DarkBlue, Color.Blue,
                Color.Green, Color.Yellow, Color.Orange, Color.Red, Color.White
            };
            int nColors = 65536;     //【カラーパレットの総数】【2^16 = 65536色】
            GenerateColorPallets(nColors, BC);
        }

        //*******************************************************
        //【CreateColorPalletForAbsolute】
        //*******************************************************
        public void CreateColorPalletForAbsolute(int nColors)
        {
            // 絶対値表示用のカラーパレットを作る
            //【色の総数】  nColors色
            //【絶対値】　　0 ⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒ nColors
            //【基本色】　　黒⇒濃灰⇒灰⇒淡灰⇒茶⇒紫⇒藍⇒青⇒淡青⇒緑⇒黄⇒橙⇒赤⇒白
            Color[] BC = new Color[]
            {
                Color.Black, Color.DarkGray, Color.Gray, Color.LightGray, Color.Brown,
                Color.Violet, Color.DarkBlue, Color.Blue, Color.Aqua, Color.Green,
                Color.Yellow, Color.Orange, Color.Red, Color.White
            };
            GenerateColorPallets(nColors, BC);
        }

        //*******************************************************
        //【CreateColorPalletForDistance】
        //*******************************************************
        public void CreateColorPalletForDistance(int nColors)
        {
            // 距離表示用のカラーパレットを作る
            //【色の総数】  nColors色
            //【距離】　　　0 ⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒ nColors
            //【基本色】　　白⇒赤⇒橙⇒黄⇒緑⇒淡青⇒青⇒藍⇒紫⇒茶⇒淡灰⇒灰⇒濃灰⇒黒
            Color[] BC = new Color[]
            {
                Color.White, Color.Red, Color.Orange, Color.Yellow, Color.Green,
                Color.Aqua, Color.Blue, Color.DarkBlue, Color.Violet, Color.Brown,
                Color.LightGray, Color.Gray , Color.DarkGray, Color.Black                                  
            };
            GenerateColorPallets(nColors, BC);
        }

        public void CreateColorPalletForDistance()
        {
            // 距離表示用のカラーパレットを作る
            //【色の総数】  nColors色
            //【距離】　　　0 ⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒ nColors
            //【基本色】　　白⇒赤⇒橙⇒黄⇒緑⇒淡青⇒青⇒藍⇒紫⇒茶⇒淡灰⇒灰⇒濃灰⇒黒
            Color[] BC = new Color[]
            {
                Color.White, Color.Red, Color.Orange, Color.Yellow, Color.Green,
                Color.Aqua, Color.Blue, Color.DarkBlue, Color.Violet, Color.Brown,
                Color.LightGray, Color.Gray , Color.DarkGray, Color.Black, Color.Black                                  
            };
            int[] Index = new int[]
            {
                0, 500, 650, 800, 1100, 1400, 1700, 2000, 2400, 2800, 3200, 3600, 4000, 6000, 8191
            };
            int nColors = 8192; //【カラーパレットの総数】【4001色】0mm～4000mmに相当
            GenerateColorPallets(nColors, Index, BC);
        }

        //*******************************************************
        //【CreateColorPalletForDipole】
        //*******************************************************
        public void CreateColorPalletForDipole()
        {
            // 距離表示用の極性カラーパレットを作る
            //【色の総数】  nColors色
            //【距離】　　　0 ⇒⇒⇒⇒⇒⇒⇒⇒⇒ nColors
            //【基本色】　　紫⇒青⇒緑⇒淡灰⇒黄⇒橙⇒赤
            int nColors = 10001; //10cmを10μmピッチ 
            Color[] BC = new Color[]
            {
                Color.Violet, Color.Blue, Color.Green, Color.LightGray, Color.Orange, Color.Yellow, Color.Red 
            };
            GenerateColorPallets(nColors, BC);
        }

        //*******************************************************
        //【CreateColorPalletForTemperature】
        //*******************************************************
        public void CreateColorPalletForTemperature(int nColors)
        {
            // 温度表示用のカラーパレットを作る
            //【色の総数】  nColors色
            //【赤外線輝度】0 ⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒⇒ nColors
            //【基本色】　　淡灰⇒白⇒藍⇒青⇒淡青⇒緑⇒黄⇒橙⇒赤⇒紫
            Color[] BC = new Color[]
            {
                Color.LightGray, Color.White, Color.DarkBlue, Color.Blue, Color.Aqua,
                Color.Green, Color.Yellow, Color.Orange, Color.Red, Color.Violet 
            };
            GenerateColorPallets(nColors, BC);
        }

        //*******************************************************
        //【GenerateColorPallets】
        //*******************************************************
        // 指定基本色BaseColorsに基づいて総数nColorsの色を作成する
        public void GenerateColorPallets(int nColors, Color[] BaseColors)
        {
            int nB = BaseColors.Length; //【基本色の総数】
            float delt = (float)(nColors - 1) / (float)(nB - 1);
            int[] index = new int[nB];
            index[0] = 0;
            index[nB - 1] = (int)(nColors - 1);
            CreateColorPallets(nColors); //【ｎ色分のカラーパレットを作成】
            if (nB > 2)
            {
                for (int i = 1; i < nB - 1; i++)
                {
                    index[i] = (int)((float)i * delt);
                }
            }
            for (int i = 0; i < nB - 1; i++)
            {
                GenerateColors(index[i], BaseColors[i], index[i + 1], BaseColors[i + 1]);
            }
        }

        //*******************************************************
        //【GenerateColorPallets】
        //*******************************************************
        // 指定基本色BaseColorsに基づいて総数nColorsの色を作成する
        // 均等に割り付けず、indexに基づいて色勾配を定める
        public void GenerateColorPallets(int nColors, int[] index, Color[] BaseColors)
        {
            if ((nColors < 2) || (index.Length != BaseColors.Length) || (index.Length < 2)) { return; }
            int nc = BaseColors.Length;
            CreateColorPallets(nColors); //【ｎ色分のカラーパレットを作成】
            for (int i = 0; i < BaseColors.Length - 1; i++)
            {
                GenerateColors(index[i], BaseColors[i], index[i + 1], BaseColors[i + 1]);
            }
        }

        //*******************************************************
        //【CreateColorPallet】ColorPalletを初期化してｎ色分の領域を確保する
        //*******************************************************
        public void CreateColorPallets(int n)
        {
            A = new byte[n];
            R = new byte[n];
            G = new byte[n];
            B = new byte[n];
        }

        //*******************************************************
        //【GenerateColor】指定パレット番号i1からi2までを指定色P1からP2まで直線補間した色を作って設定する
        //*******************************************************
        public void GenerateColors(int i1, Color C1, int i2, Color C2)
        {
            float eA, eR, eG, eB, dA, dR, dG, dB, d;
            d = 1.0f / ((float)i2 - (float)i1);
            eA = (float)C1.A;
            eR = (float)C1.R;
            eG = (float)C1.G;
            eB = (float)C1.B;
            dA = ((float)C2.A - eA) * d;
            dR = ((float)C2.R - eR) * d;
            dG = ((float)C2.G - eG) * d;
            dB = ((float)C2.B - eB) * d;
            for (int i = i1; i <= i2; i++)
            {
                A[i] = (byte)eA;
                R[i] = (byte)eR;
                G[i] = (byte)eG;
                B[i] = (byte)eB;
                eA += dA;
                eR += dR;
                eG += dG;
                eB += dB;
            }
        }

        //*******************************************************
        //【GetColorCode】int型で指定番号のパレット色を取得する
        //*******************************************************
        public int GetColorCode(int index)
        {
            int C = (int)((long)A[index] << 24 | (long)R[index] << 16 | (long)G[index] << 8 | (long)B[index]);
            return C;
        }

        //*******************************************************
        //【GetColor】Color型で指定番号のパレット色を取得する
        //*******************************************************
        public Color GetColor(int index)
        {
            Color p = new Color();
            p = Color.FromArgb((int)A[index], (int)R[index], (int)G[index], (int)B[index]);
            return p;
        }

        //*******************************************************
        //【GetPalletSize】パレットの色数を得る
        //*******************************************************
        public int GetPalletSize
        {
            get { return A.Length; }
        }

        //*******************************************************
        //【CreateColorScale】Bitmap形式のカラースケールを作る
        //*******************************************************
        public Bitmap CreateColorScale(int width, int height, Boolean reverse)
        {
            int i, j, k, eA, eR, eG, eB, index; float d;
            Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb); //【ColorImage表示用】
            Rectangle rect = new Rectangle(0, 0, width, height); //【ColorImage用枠】
            byte[] pixelData = new byte[width * height * 4];
            d = (float)A.Length / (float)width;
            for (i = 0; i < width; i++)
            {
                index = (int)((float)i * d);
                eB = B[index];
                eG = G[index];
                eR = R[index];
                eA = A[index];
                for (j = 0; j < height; j++)
                {
                    k = (j * width + i) * 4;
                    pixelData[k] = (byte)eB;
                    pixelData[k + 1] = (byte)eG;
                    pixelData[k + 2] = (byte)eR;
                    pixelData[k + 3] = (byte)eA;
                }
            }
            BitmapData data = bmp.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb); //【Lock】
            Marshal.Copy(pixelData, 0, data.Scan0, pixelData.Length); //【UnmanagedMemoryのBitmapDataに配列PixelDataをコピー】
            bmp.UnlockBits(data);           //【Lock解除】
            if(reverse) {
                bmp.RotateFlip(RotateFlipType.Rotate180FlipY); //【左右反転】
            } 
            return bmp;
        }
    }
}
