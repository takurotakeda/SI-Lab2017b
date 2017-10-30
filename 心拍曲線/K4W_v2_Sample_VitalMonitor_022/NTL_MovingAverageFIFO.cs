using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTL.KinectLib
{
    public class MovingAverageFIFO
    {
        public int nFIFO;   // FIFOサイズ
        public int Ptr;     // FIFOのポインタ
        public int[] FIFO;  // FIFOメモリ
        public int Average; // 平均値
        private long Sum;   // FIFO累積加算値
        private double K;   // Offset平均値を得る為の乗算係数

        public void Initialize(int n)
        {
            nFIFO = n;
            K = 1.0 / (double)nFIFO;
            FIFO = new int[nFIFO];
            Ptr = 0;
            Sum = 0;
        }

        public int LoadDataToFIFO(int Data)
        {
            Sum += Data - FIFO[Ptr];
            FIFO[Ptr] = Data;
            Ptr++; if (Ptr == nFIFO) { Ptr = 0; }
            Average = (int)(Sum * K);    // 平均値
            return Average;
        }
    }
}
