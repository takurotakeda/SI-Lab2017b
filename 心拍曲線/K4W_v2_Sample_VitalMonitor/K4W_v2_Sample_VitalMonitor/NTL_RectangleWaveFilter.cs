using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NTL.KinectLib
{
    //*************************************************************************
    //*************************************************************************
    //【CLASS】『RectangleWaveFilter』【矩形波相関フィルタ】
    //*************************************************************************
    //*************************************************************************
    public class RectangleWaveFilter
    {
        private int nFIFO;
        private int ptr0 = 0;
        private int ptr1 = 0;
        private int ptr2 = 0;
        private double[] FIFO;
        private double sum;
        private double K;

        //***************************************************************
        //【矩形波相関フィルタ】★初期化
        //***************************************************************
        public void Initialize(int n)
        {
            int n4 = n / 4;
            nFIFO = n4 * 4;
            ptr0 = 0;
            ptr1 = n4;
            ptr2 = 3 * n4;
            K = 1.0 / (double)nFIFO;
            sum = 0.0;
            FIFO = new double[nFIFO];
            for (int i = 0; i < nFIFO; i++) { FIFO[i] = 0.0; }
        }

        //***************************************************************
        //【矩形波相関フィルタ】
        //***************************************************************
        public double CrossCorrelationFilter(double d)
        {
            sum += -d + 2 * (FIFO[ptr2] - FIFO[ptr1]) + FIFO[ptr0];
            FIFO[ptr0] = d;
            ptr0++; if (ptr0 == nFIFO) { ptr0 = 0; }
            ptr1++; if (ptr1 == nFIFO) { ptr1 = 0; }
            ptr2++; if (ptr2 == nFIFO) { ptr2 = 0; }
            return sum * K;
        }
    }

    //*************************************************************************
    //*************************************************************************
    //【CLASS】『HeartRateFilter』【矩形波相関を用いた心拍数測定フィルタ】
    //*************************************************************************
    //*************************************************************************
    public class HeartRateFilter
    {
        //【矩形波相関用FIFO】必ず4の倍数で、脈波の周期に近いサンプル数であること
        private const int nFIFO = 28; //【矩形波相関フィルタの段数】
        private int[] TimeFIFO = new int[nFIFO];
        private float[] RectFIFO = new float[nFIFO];
        private int ptr0, ptr1, ptr2, ptr3, ptr4, ptr5;   //  0, 26, 24, 21, 20, 19
        private int ptr6, ptr7, ptr8, ptr9, ptr10, ptr11; // 14,  9,  8,  7,  4,  2
        private float K = 1.0f/nFIFO;

        public struct FilterOutput
        {
            public int Time;
            public float Output;
        }

        public struct IrPeak
        {
            public float HeartBeatRate;
            public int pTime;
            public float minValue;
            public int flag;
        }

        private FilterOutput rectOut;
        private IrPeak Peak;

        //*****************************************************
        //【矩形波相関フィルタの演算】
        //*****************************************************
        public void Initialize()
        {
            rectOut.Output = 0.0f; rectOut.Time = 0;
            for (int i = 0; i < nFIFO; i++) { RectFIFO[i] = 0.0f; TimeFIFO[i] = 0; }
            ptr0 =  0;  ptr1 = 26;  ptr2 = 24;  ptr3 = 21;  ptr4 = 20;  ptr5 = 19;
            ptr6 = 14;  ptr7 =  9;  ptr8 =  8;  ptr9 =  7;  ptr10 = 4;  ptr11 = 2;

            Peak.HeartBeatRate = 0.0f;
            Peak.pTime = 0;
            Peak.minValue = 0;
            Peak.flag = 0;
        }

        public float GetFilter(float DataIn, int curTime)
        {
            rectOut.Time = TimeFIFO[ptr6];
            rectOut.Output += (-1) * (DataIn + RectFIFO[ptr1]  + RectFIFO[ptr2])
                      +  2 *(RectFIFO[ptr3]  + RectFIFO[ptr4]  + RectFIFO[ptr5]) 
                      +(-2)*(RectFIFO[ptr7]  + RectFIFO[ptr8]  + RectFIFO[ptr9])
                      +  1 *(RectFIFO[ptr10] + RectFIFO[ptr11] + RectFIFO[ptr0]);
            RectFIFO[ptr0]=DataIn;
            TimeFIFO[ptr0]=curTime; 
            ptr0++;  if (ptr0 == nFIFO)  { ptr0=0; }
            ptr1++;  if (ptr1 == nFIFO)  { ptr1 = 0; }
            ptr2++;  if (ptr2 == nFIFO)  { ptr2 = 0; }
            ptr3++;  if (ptr3 == nFIFO)  { ptr3 = 0; }
            ptr4++;  if (ptr4 == nFIFO)  { ptr4 = 0; }
            ptr5++;  if (ptr5 == nFIFO)  { ptr5 = 0; }
            ptr6++;  if (ptr6 == nFIFO)  { ptr6 = 0; }
            ptr7++;  if (ptr7 == nFIFO)  { ptr7 = 0; }
            ptr8++;  if (ptr8 == nFIFO)  { ptr8 = 0; }
            ptr9++;  if (ptr9 == nFIFO)  { ptr9 = 0; }
            ptr10++; if (ptr10 == nFIFO) { ptr10 = 0; }
            ptr11++; if (ptr11 == nFIFO) { ptr11 = 0; }
            return rectOut.Output * K;
        }

        //*****************************************************
        //【脈拍数検出処理】
        //*****************************************************
        public float GetHeartBeatRate(float curData)
        {
            // rectOut.Time  【現在時刻】[ミリ秒]
            // rectOut.Output【現在値】
            int curTime = rectOut.Time;
            float value = curData; //  rectOut.Output;
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
                } else {
                    if (Peak.flag == 0) { Peak.minValue = value; }
                }
            }
            return Peak.HeartBeatRate;
        }
       
    }
}
