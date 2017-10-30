using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NTL.KinectLib
{
    //*************************************************************************
    //*************************************************************************
    //【CLASS】『HeartRateFilter』【矩形波相関を用いた心拍数測定フィルタ】
    //*************************************************************************
    //*************************************************************************
    public class RectangleFilter
    {
        //【矩形波相関用FIFO】必ず4の倍数で、脈波の周期に近いサンプル数であること
        private int nFIFO; //【矩形波相関フィルタの段数】
        private double[] FIFO;
        private int Ptr0, Ptr1, Ptr2; // 012 345678 9AB
        private double K;
        private double Sum;
        public double Output;
        public int pTime;
        public int cTime;

        //*****************************************************
        //【矩形波相関フィルタの演算】 3*(12/4)=9, 12/4=3
        //*****************************************************
        public void Initialize(int n)
        {
            nFIFO = 4 * (n / 4);
            K = 1.0 / (double)nFIFO;
            FIFO = new double[nFIFO];
            for (int i = 0; i < nFIFO; i++) { FIFO[i] = 0.0; }
            Ptr0 = 0;
            Ptr1 = 3 * nFIFO / 4;
            Ptr2 = nFIFO / 4;
            Sum = 0.0;
        }

        public double GetRectangleFilter(double DataIn)
        {
            Sum += (-1) * DataIn + 2 * FIFO[Ptr1]  - 2 * FIFO[Ptr2] + FIFO[Ptr0];
            FIFO[Ptr0] = DataIn;
            Ptr0++;  if (Ptr0 == nFIFO)  { Ptr0 = 0; }
            Ptr1++;  if (Ptr1 == nFIFO)  { Ptr1 = 0; }
            Ptr2++;  if (Ptr2 == nFIFO)  { Ptr2 = 0; }
            Output = Sum * K;
            return Output;
        }
    }
}
