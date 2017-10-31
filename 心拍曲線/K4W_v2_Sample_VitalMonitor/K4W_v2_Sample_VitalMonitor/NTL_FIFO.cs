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
    public class FIFO
    {
        private int nFIFO;
        private float F1;
        private int ptr = 0;
        private float Offset;
        private int nOffset;   //【オフセットを判断するサンプル数】
        public float[] dFIFO;
        public PointF[] Points;
        private float vmax;
        private float vmin;
        private Boolean fFIFOfull = false;
        private int dH, dH2;
        float dRng, KK;

        //***************************************************************
        //【Initialize】
        //***************************************************************
        //【パラメータ】n:FIFOの総サンプル数  nO:DCオフセットを判定するサンプル個数
        public void Initialize(int n, int nO, int dHeight, float dRange)
        {
            vmax = 0.0f;
            vmin = 70000.0f;
            ptr = 0;               //【FIFOポインタクリア】
            nFIFO = n;             //【FIFO段数】
            nOffset = nO;          //【FIFOオフセットレベル判定段数】
            F1 = 1.0f / (float)nO; //【FIFOオフセットレベル平均値演算用】
            dFIFO = new float[n];
            Points = new PointF[n];
            for (var i = 0; i < n; i++) { dFIFO[i] = 0.0f; }
            dH = dHeight;
            dH2 = dH / 2;
            dRng = dRange;
            KK = dH / dRng;
            fFIFOfull = false;
        }

        //***************************************************************
        //【LoadDataToFIFO】
        //***************************************************************
        public float LoadDataToFIFO(float DataIn)
        {
            int i, j;
            float d, sum, DataOut;

            //【FIFO書込み・読出し】
            DataOut = dFIFO[ptr]; //【FIFO出力データの読出】
            dFIFO[ptr] = DataIn;  //【FIFO入力データの書込】

            //【オフセットレベル演算】
            vmax = 0.0f;
            vmin = 70000.0f;
            sum = 0.0f;
            for (i = 0, j = ptr; i < nOffset; i++)
            {
                d = dFIFO[j];
                if (d > vmax) { vmax = d; }
                if (d < vmin) { vmin = d; }
                sum += d;
                j--; if (j < 0) { j = nFIFO - 1; }
            }
            Offset = sum * F1;

            //【波形データ作成】
            for (i = 0, j = ptr; i < nFIFO; i++)
            {
                d = dFIFO[j];
                Points[i].X = (float)i;
                Points[i].Y = (d - Offset) * KK + dH2;
                j++; if (j == nFIFO) { j = 0; }
            }

                //【ポインタ処理】
                ptr++;
            if (ptr == nFIFO) { ptr = 0; fFIFOfull = true; }
            return DataOut;
        }

        //***************************************************************
        //【OffsetLevel】
        //***************************************************************
        public double OffsetLevel
        {
            get { return Offset; }
        }

        //***************************************************************
        //【Pointer】
        //***************************************************************
        public int Pointer
        {
            get { return ptr; }
        }

        //***************************************************************
        //【Max】
        //***************************************************************
        public double Max
        {
            get { return vmax; }
        }

        //***************************************************************
        //【Min】
        //***************************************************************
        public double Min
        {
            get { return vmin; }
        }

        //***************************************************************
        //【FIFO Full Flag】
        //***************************************************************
        public Boolean FullFlag
        {
            get { return fFIFOfull; }
        }
    }
}
