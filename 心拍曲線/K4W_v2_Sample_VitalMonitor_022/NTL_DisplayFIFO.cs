using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing; //【Point】

namespace NTL.KinectLib
{
    //*************************************************************************
    //*************************************************************************
    //【CLASS】『FIFO』【FIFOを実現する】
    //*************************************************************************
    //*************************************************************************
    public class DisplayFIFO
    {
        public int nFIFO;       // 波形データFIFOサイズ
        public int Ptr;         // 波形データFIFOポインタ
        public double[] FIFO;   // 波形データFIFO
        public PointF[] Points; // 頂点データ(波形描画用)
        private double Ky, Kofs;      

        //***************************************************************
        //【Initialize】n:FIFO段数, s:表示倍率, o:表示オフセット
        //***************************************************************
        public void Initialize(int n, double s, double o)
        {
            nFIFO = n;              // FIFO段数
            FIFO = new double[n];   // 
            Points = new PointF[n]; //
            Ptr = 0;                // FIFOポインタクリア
            for (int i = 0; i < n; i++) { FIFO[i] = 0.0; }
            Ky = -s;   // 表示倍率
            Kofs = o; // 表示オフセット
        }

        //***************************************************************
        //【LoadDataToFIFO】
        //***************************************************************
        public double LoadDataToFIFO(double DataIn)
        {
            int i, j;
            double DataOut;

            //【FIFO書込み・読出し】
            DataOut = FIFO[Ptr]; //【FIFO出力データの読出】
            FIFO[Ptr] = DataIn;  //【FIFO入力データの書込】
            //【ポインタ処理】
            Ptr++;
            if (Ptr == nFIFO) { Ptr = 0; }

            //【波形データ作成】
            for (i = 0, j = Ptr; i < nFIFO; i++)
            {
                Points[i].X = (float)i;
                Points[i].Y = (float)(Ky * FIFO[j] + Kofs);
                j++; if (j == nFIFO) { j = 0; }
            }

            return DataOut;
        }
    }
}
