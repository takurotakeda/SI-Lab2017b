using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NTL.KinectLib
{
    //*************************************************************************
    //*************************************************************************
    //【CLASS】『NoiseReduction』【放物線で補間】
    //*************************************************************************
    //*************************************************************************
    // Oj = a*xj^2+b*xj+c で近似
    public class NoiseReduction
    {
        private int nSample;   // FIFO段数
        public double[] FIFO;   // FIFOデータ サンプル
        private int wPtr;      // FIFO書込みポインタ
        private int rPtr;      // FIFO読出しポインタ
        private double a, b, c; // 放物線O(t)=a*t^2+b*t+cの未知数
        private double d;       // 放物線の軸値d=-b/(2*a)
        private double Ka;      // Ka=1.0f/Σtj^4
        private double Kb;      // Kb=1.0f/Σtj^2
        private double Kc;      // Kb=1.0f/n
        private bool FIFOfull;  // FIFOfull flag

        public struct ParabolaData
        {
            public double A; //【放物線係数】
            public double B; //【放物線係数】
            public double C; //【放物線係数】
            public double D; //【放物線の軸】d=-b/(2*a)
        }

        private ParabolaData Parabola1;

        //*************************************************************
        //【Initialize】
        //*************************************************************
        public void Initialize(int n)
        {
            float t, t2;
            nSample = n;
            FIFO = new double[nSample];
            wPtr = rPtr = 0;
            Ka = Kb = 0.0f;
            for (int i = 0; i < nSample; i++)
            {
                t = (float)(-i); // *0.033f; //【1フレームは33mS】
                FIFO[i] = 0.0f;  //【FIFOは0クリア】
                t2 = t * t;      //(-tj)^2
                Ka += t2 * t2;   //Σ(-tj)^4
                Kb += t2;        //Σ(-tj)^2
            }
            Ka = 1.0f / Ka;
            Kb = 1.0f / Kb;
            Kc = 1.0f / (double)nSample;
            FIFOfull = false;
        }

        //*************************************************************
        //【Estimation】
        //*************************************************************
        public void Estimation(double data)
        {
            FIFO[wPtr] = data;
            rPtr = wPtr;
            //【書込みポインタの更新】
            wPtr++;
            if (wPtr == nSample)
            {
                wPtr = 0; FIFOfull = true;
            }

            //【Base Transition Ruleで放物線a*t^2+b*t+cを推定する】１０回実行
            if (FIFOfull)
            {
                ParabolaEstimation();
                ParabolaEstimation();
                ParabolaEstimation();
                ParabolaEstimation();
                ParabolaEstimation();
                ParabolaEstimation();
                ParabolaEstimation();
                ParabolaEstimation();
                ParabolaEstimation();
                ParabolaEstimation();

                //【推定した放物線a*t^2+b*t+cから軸値d=-b/(2*a)を計算】
                CalculateParabolaAxis();
            }
        }

        //*************************************************************
        //【Property】☆☆☆☆☆ Estimated Value ☆☆☆☆☆
        //*************************************************************
        public int Length { get { return nSample; } }
        public int Pointer { get { return wPtr; } }
        public bool Full { get { return FIFOfull; } }
        public double A { get { if (FIFOfull) { return a; } else { return 0.0f; } } }
        public double B { get { if (FIFOfull) { return b; } else { return 0.0f; } } }
        public double C { get { if (FIFOfull) { return c; } else { return 0.0f; } } }
        public double D { get { if (FIFOfull) { return d; } else { return -1000.0f; } } }
        public ParabolaData ParabolaParameters
        {
            get
            {
                if (FIFOfull)
                {
                    Parabola1.A = a;
                    Parabola1.B = b;
                    Parabola1.C = c;
                    Parabola1.D = d;
                }
                else
                {
                    Parabola1.A = 0.0f;
                    Parabola1.B = 0.0f;
                    Parabola1.C = 0.0f;
                    Parabola1.D = -1000.0f;
                }
                return Parabola1;
            }
        }

        //---------------------------------------------------------------
        //【CircleEstimation】☆☆☆☆☆ Base Transition Rule ☆☆☆☆☆
        //---------------------------------------------------------------
        private void ParabolaEstimation()
        {
            int j;
            double t, t2, t4, e;
            double Sa, Sb, Sc; // (working parameter) Sa=Σt^4*(Sj-Oj), Sb=Σt^2*(Sj-Oj), Sc=Σ(Sj-Oj)
            //【Estimate Xc】
            Sa = 0.0f;
            for (j = 0; j < nSample; j++)
            {
                t = (double)(-j); // *0.033f;
                t2 = t * t;
                t4 = t2 * t2;
                e = FIFO[rPtr] - (a * t2 + b * t + c);
                Sa += t2 * e;
                //【読出しポインタの更新】
                rPtr--; if (rPtr < 0) { rPtr = nSample - 1; }
            }
            a += Ka * Sa;
            //【Estimate Xc】
            Sb = 0.0f;
            for (j = 0; j < nSample; j++)
            {
                t = (double)(-j); // *0.033f;
                t2 = t * t;
                e = FIFO[rPtr] - (a * t2 + b * t + c);
                Sb += t * e;
                //【読出しポインタの更新】
                rPtr--; if (rPtr < 0) { rPtr = nSample - 1; }
            }
            b += Kb * Sb;
            //【Estimate Xc】
            Sc = 0.0f;
            for (j = 0; j < nSample; j++)
            {
                t = (double)(-j); // *0.033f;
                t2 = t * t;
                e = FIFO[rPtr] - (a * t2 + b * t + c);
                Sc += e;
                //【読出しポインタの更新】
                rPtr--; if (rPtr < 0) { rPtr = nSample - 1; }
            }
            c += Kc * Sc;
        }

        //---------------------------------------------------------------
        //【CalculateRadius】☆☆☆☆☆ Average ☆☆☆☆☆
        //---------------------------------------------------------------
        private void CalculateParabolaAxis()
        {
            d = -b / (2.0f * a);
        }
    }
}
