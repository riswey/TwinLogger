using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiDeviceAIO
{
    interface I_MyAIO
    {
        void Init(string device_name);
        void Close();
        void SetupExternalParameters(double frequency, bool clipsOn);
        int SetupTimedSample(short n_channels, short timer_interval, short n_samples, CaioConst range);
        int Start(uint HandleMsgLoop);
        void Stop();
        void PrepareData(int device_id, int num_samples);
        string GetHeader(string delimiter);
        void Print(string delimiter, ref string visitor);
        void Reset();
    }
}
