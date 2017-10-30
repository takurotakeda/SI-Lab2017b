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
        private double F1;
        private int ptr = 0;
        private double Offset;
        private int nOffset;   //【オフセットを判断するサンプル数】
        public double[] dFIFO;
        public PointF[] Points;
        private double vmax;
        private double vmin;
        private Boolean fPeriod = false;
        private const double minLimit = 2.0;
        private Boolean fBreathingAlert = false;

        //***************************************************************
        //【Initialize】
        //***************************************************************
        //【パラメータ】n:FIFOの総サンプル数  nO:DCオフセットを判定するサンプル個数
        public void Initialize(int n, int nO)
        {
            vmax = -32767.0;
            vmin = 32767.0;
            ptr = 0;
            nFIFO = n;
            F1 = 1.0 / (double)nO;
            dFIFO = new double[n];
            nOffset = nO;

            Points = new PointF[n];
            for (var i = 0; i < n; i++) { dFIFO[i] = 0.0; }
        }

        //***************************************************************
        //【LoadDataToFIFO】
        //***************************************************************
        public double LoadDataToFIFO(double DataIn)
        {
            int i, j;
            double d, sum;
            double DataOut = dFIFO[ptr];
            dFIFO[ptr] = DataIn;

            /*
            max = -1.0D + 105;
            min = 1.0D + 105;
            for (i = 0; i < nFIFO; i++)
            {
                d = dFIFO[i];
                if (d > max) { max = d; }
                if (d < min) { min = d; }
            }
            */

            vmax = 0.0;
            vmin = 8000.0;
            sum = 0.0;
            for (i = 0, j = ptr; i < nOffset; i++)
            {
                d = dFIFO[j];
                if (d > vmax) { vmax = d; }
                if (d < vmin) { vmin = d; }
                sum += d;
                j--; if (j < 0) { j = nFIFO - 1; }
            }
            Offset = sum * F1;
            ptr++;
            if (ptr == nFIFO) { ptr = 0; fPeriod = true; }

            //【呼吸停止判定】Offset判定時間中の変動が所定値よりも小さい場合
            if ((fPeriod == true) && ((vmax - vmin) < minLimit)) { fBreathingAlert = true; } else { fBreathingAlert = false; }
            
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
        //【BreathingStop】
        //***************************************************************
        public Boolean BreathingStop
        {
            get { return fBreathingAlert; }
        }
    }
}
