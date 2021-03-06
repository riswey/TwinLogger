﻿using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace CaioCs
{
    /// <summary>
    /// Summary description for Caio.
    /// </summary>
    public class Caio1: Caio
    {
        [DllImport("user32.dll")]
        public static extern int SendMessage(
                  int hWnd,      // handle to destination window
                  uint Msg,       // message
                  long wParam,  // first message parameter
                  long lParam   // second message parameter
                  );

        MultiDeviceAIO.LoggerState state = new MultiDeviceAIO.LoggerState();

        //Dummy Data source!
        Random rnd = new Random();

        //constructor
        public Caio1()
        {
        }

        //Signal generators
        float nextVolt(int id, int channel)
        {
            //random
            //return (float) (4 * rnd.NextDouble() - 2);

            if (channel == 0)
            {
                if (++t > 1000) t = 0;
            }
            return (float) (2 * Math.Sin(t * 0.1));
        }

        int t = 0;
        int nextWord(int id, int channel)
        {
            if (channel == 0)
            {
                if (++t > 1000) t = 0;
            }
            return 38192 + (int)Math.Floor(2413.5 * (Math.Sin(t*0.08)));
            //random
            //return rnd.Next(0,65535);
        }

        //Common Function
        public override int Init(string DeviceName, out short Id)
        {
            Id = 0;
            int ret = 10000;//AioInit(DeviceName, ref Id);
            if (DeviceName == "Aio000")
            {
                Id = 0;
                ret = 0;
            }
            if (DeviceName == "Aio001")
            {
                Id = 1;
                ret = 0;
            }

            return ret;
        }
        public override int Exit(short Id)
        {
            int ret = 0;//AioExit(Id);
            return ret;
        }
        public override int ResetDevice(short Id)
        {
            int ret = 0;//AioResetDevice(Id);
            return ret;
        }
        public override int GetErrorString(int ErrorCode, out string ErrorString)
        {
            ErrorString = new String('0', 1);
            System.Text.StringBuilder errorstring = new System.Text.StringBuilder(256);
            int ret = 10;//AioGetErrorString(ErrorCode, errorstring);
            if (ret == 0)
            {
                ErrorString = errorstring.ToString();
            }
            return ret;
        }
        public override int QueryDeviceName(short Index, out string DeviceName, out string Device)
        {
            DeviceName = new String('0', 1);
            Device = new String('0', 1);
            System.Text.StringBuilder devicename = new System.Text.StringBuilder(256);
            System.Text.StringBuilder device = new System.Text.StringBuilder(256);
            int ret = 10;// AioQueryDeviceName(Index, devicename, device);
            if (ret == 0)
            {
                DeviceName = devicename.ToString();
                Device = device.ToString();
            }
            return ret;
        }
        public override int GetDeviceType(string Device, out short DeviceType)
        {
            DeviceType = 0;
            int ret = 10;// AioGetDeviceType(Device, ref DeviceType);
            return ret;
        }
        public override int SetControlFilter(short Id, short Signal, float Value)
        {
            int ret = 10;// AioSetControlFilter(Id, Signal, Value);
            return ret;
        }
        public override int GetControlFilter(short Id, short Signal, out float Value)
        {
            Value = 0;
            int ret = 10;// AioGetControlFilter(Id, Signal, ref Value);
            return ret;
        }
        public override int ResetProcess(short Id)
        {
            int ret = 10;// AioResetProcess(Id);
            return ret;
        }

        //Analog Input Function
        public override int SingleAi(short Id, short AiChannel, out int AiData)
        {
            AiData = 0;
            int ret = 10;// AioSingleAi(Id, AiChannel, ref AiData);
            return ret;
        }
        public override int SingleAiEx(short Id, short AiChannel, out float AiData)
        {
            AiData = 0;
            int ret = 10;//AioSingleAiEx(Id, AiChannel, ref AiData);
            return ret;
        }
        public override int MultiAi(short Id, short AiChannels, int[] AiData)
        {
            //Create random data
            for (int i = 0; i < AiChannels; i++)
            {
                AiData[i] = nextWord(Id, i);
            }

            int ret = 0;//AioMultiAiEx(Id, AiChannels, AiData);
            return ret;
        }
        public override int MultiAiEx(short Id, short AiChannels, float[] AiData)
        {
            //Create random data
            for (int i=0;i<AiChannels;i++)
            {
                AiData[i] = nextVolt(Id, i);
            }

            int ret = 0;//AioMultiAiEx(Id, AiChannels, AiData);
            return ret;
        }
        public override int GetAiResolution(short Id, out short AiResolution)
        {
            AiResolution = 0;
            int ret = 10;//AioGetAiResolution(Id, ref AiResolution);
            return ret;
        }
        public override int SetAiInputMethod(short Id, short AiInputMethod)
        {
            int ret = 10;//AioSetAiInputMethod(Id, AiInputMethod);
            return ret;
        }
        public override int GetAiInputMethod(short Id, out short AiInputMethod)
        {
            AiInputMethod = 0;
            int ret = 10;//AioGetAiInputMethod(Id, ref AiInputMethod);
            return ret;
        }
        public override int GetAiMaxChannels(short Id, out short AiMaxChannels)
        {
            AiMaxChannels = 0;
            int ret = 10;//AioGetAiMaxChannels(Id, ref AiMaxChannels);
            return ret;
        }
        public override int SetAiChannel(short Id, short AiChannel, short Enabled)
        {
            int ret = 10;//AioSetAiChannel(Id, AiChannel, Enabled);
            return ret;
        }
        public override int GetAiChannel(short Id, short AiChannels, out short Enabled)
        {
            Enabled = 0;
            int ret = 10;//AioGetAiChannel(Id, AiChannels, ref Enabled);
            return ret;
        }
        public override int SetAiChannels(short Id, short AiChannels)
        {
            state.n_channels = AiChannels;
            int ret = 0;//AioSetAiChannels(Id, AiChannels);
            return ret;
        }
        public override int GetAiChannels(short Id, out short AiChannels)
        {
            AiChannels = state.n_channels;
            int ret = 0;//AioGetAiChannels(Id, ref AiChannels);
            return ret;
        }
        public override int SetAiChannelSequence(short Id, short AiSequence, short AiChannel)
        {
            int ret = 10;//AioSetAiChannelSequence(Id, AiSequence, AiChannel);
            return ret;
        }
        public override int GetAiChannelSequence(short Id, short AiSequence, out short AiChannel)
        {
            AiChannel = 0;
            int ret = 10;//AioGetAiChannelSequence(Id, AiSequence, ref AiChannel);
            return ret;
        }
        public override int SetAiRange(short Id, short AiChannel, short AiRange)
        {
            int ret = 10;//AioSetAiRange(Id, AiChannel, AiRange);
            return ret;
        }
        public override int SetAiRangeAll(short Id, short AiRange)
        {
            int ret = 10;//AioSetAiRangeAll(Id, AiRange);
            return ret;
        }
        public override int GetAiRange(short Id, short AiChannel, out short AiRange)
        {
            AiRange = 0;
            int ret = 10;//AioGetAiRange(Id, AiChannel, ref AiRange);
            return ret;
        }
        public override int SetAiTransferMode(short Id, short AiTransferMode)
        {
            //Mode 0 - device buffer!
            int ret = 0;//AioSetAiTransferMode(Id, AiTransferMode);
            return ret;
        }
        public override int GetAiTransferMode(short Id, out short AiTransferMode)
        {
            AiTransferMode = 0;
            int ret = 10;//AioGetAiTransferMode(Id, ref AiTransferMode);
            return ret;
        }
        public override int SetAiDeviceBufferMode(short Id, short AiDeviceBufferMode)
        {
            int ret = 10;//AioSetAiDeviceBufferMode(Id, AiDeviceBufferMode);
            return ret;
        }
        public override int GetAiDeviceBufferMode(short Id, out short AiDeviceBufferMode)
        {
            AiDeviceBufferMode = 0;
            int ret = 10;//AioGetAiDeviceBufferMode(Id, ref AiDeviceBufferMode);
            return ret;
        }
        public override int SetAiMemorySize(short Id, int AiMemorySize)
        {
            int ret = 10;//AioSetAiMemorySize(Id, AiMemorySize);
            return ret;
        }
        public override int GetAiMemorySize(short Id, out int AiMemorySize)
        {
            AiMemorySize = 0;
            int ret = 10;//AioGetAiMemorySize(Id, ref AiMemorySize);
            return ret;
        }
        public override int SetAiTransferData(short Id, int DataNumber, IntPtr Buffer)
        {
            int ret = 10;//AioSetAiTransferData(Id, DataNumber, Buffer);
            return ret;
        }
        public override int SetAiAttachedData(short Id, int AttachedData)
        {
            int ret = 10;//AioSetAiAttachedData(Id, AttachedData);
            return ret;
        }
        public override int GetAiSamplingDataSize(short Id, out short DataSize)
        {
            DataSize = 0;
            int ret = 10;//AioGetAiSamplingDataSize(Id, ref DataSize);
            return ret;
        }
        public override int SetAiMemoryType(short Id, short AiMemoryType)
        {
            //FIFO memory
            int ret = 0;//AioSetAiMemoryType(Id, AiMemoryType);
            return ret;
        }
        public override int GetAiMemoryType(short Id, out short AiMemoryType)
        {
            AiMemoryType = 0;
            int ret = 10;//AioGetAiMemoryType(Id, ref AiMemoryType);
            return ret;
        }
        public override int SetAiRepeatTimes(short Id, int AiRepeatTimes)
        {
            int ret = 10;//AioSetAiRepeatTimes(Id, AiRepeatTimes);
            return ret;
        }
        public override int GetAiRepeatTimes(short Id, out int AiRepeatTimes)
        {
            AiRepeatTimes = 0;
            int ret = 10;//AioGetAiRepeatTimes(Id, ref AiRepeatTimes);
            return ret;
        }
        public override int SetAiClockType(short Id, short AiClockType)
        {
            //Dummy is computer whatever
            int ret = 0;//AioSetAiClockType(Id, AiClockType);
            return ret;
        }
        public override int GetAiClockType(short Id, out short AiClockType)
        {
            AiClockType = 0;
            int ret = 10;//AioGetAiClockType(Id, ref AiClockType);
            return ret;
        }
        public override int SetAiSamplingClock(short Id, float AiSamplingClock)
        {
            state.timer_interval = (short) AiSamplingClock;
            int ret = 0;//AioSetAiSamplingClock(Id, AiSamplingClock);
            return ret;
        }
        public override int GetAiSamplingClock(short Id, out float AiSamplingClock)
        {
            AiSamplingClock = 0;
            int ret = 10;//AioGetAiSamplingClock(Id, ref AiSamplingClock);
            return ret;
        }
        public override int SetAiScanClock(short Id, float AiScanClock)
        {
            int ret = 10;//AioSetAiScanClock(Id, AiScanClock);
            return ret;
        }
        public override int GetAiScanClock(short Id, out float AiScanClock)
        {
            AiScanClock = 0;
            int ret = 10;//AioGetAiScanClock(Id, ref AiScanClock);
            return ret;
        }
        public override int SetAiClockEdge(short Id, short AiClockEdge)
        {
            int ret = 10;//AioSetAiClockEdge(Id, AiClockEdge);
            return ret;
        }
        public override int GetAiClockEdge(short Id, out short AiClockEdge)
        {
            AiClockEdge = 0;
            int ret = 10;//AioGetAiClockEdge(Id, ref AiClockEdge);
            return ret;
        }
        public override int SetAiStartTrigger(short Id, short AiStartTrigger)
        {
            //Dummy is computer whatever so doesn't matter
            int ret = 0;//AioSetAiStartTrigger(Id, AiStartTrigger);
            return ret;
        }
        public override int GetAiStartTrigger(short Id, out short AiStartTrigger)
        {
            AiStartTrigger = 0;
            int ret = 10;//AioGetAiStartTrigger(Id, ref AiStartTrigger);
            return ret;
        }
        public override int SetAiStartLevel(short Id, short AiChannel, int AiStartLevel, short AiDirection)
        {
            int ret = 10;//AioSetAiStartLevel(Id, AiChannel, AiStartLevel, AiDirection);
            return ret;
        }
        public override int SetAiStartLevelEx(short Id, short AiChannel, float AiStartLevel, short AiDirection)
        {
            int ret = 10;//AioSetAiStartLevelEx(Id, AiChannel, AiStartLevel, AiDirection);
            return ret;
        }
        public override int GetAiStartLevel(short Id, short AiChannel, out int AiStartLevel, out short AiDirection)
        {
            AiStartLevel = 0;
            AiDirection = 0;
            int ret = 10;//AioGetAiStartLevel(Id, AiChannel, ref AiStartLevel, ref AiDirection);
            return ret;
        }
        public override int GetAiStartLevelEx(short Id, short AiChannel, out float AiStartLevel, out short AiDirection)
        {
            AiStartLevel = 0;
            AiDirection = 0;
            int ret = 10;//AioGetAiStartLevelEx(Id, AiChannel, ref AiStartLevel, ref AiDirection);
            return ret;
        }
        public override int SetAiStartInRange(short Id, short AiChannel, int Level1, int Level2, int StateTimes)
        {
            int ret = 10;//AioSetAiStartInRange(Id, AiChannel, Level1, Level2, StateTimes);
            return ret;
        }
        public override int SetAiStartInRangeEx(short Id, short AiChannel, float Level1, float Level2, int StateTimes)
        {
            int ret = 10;//AioSetAiStartInRangeEx(Id, AiChannel, Level1, Level2, StateTimes);
            return ret;
        }
        public override int GetAiStartInRange(short Id, short AiChannel, out int Level1, out int Level2, out int StateTimes)
        {
            Level1 = 0;
            Level2 = 0;
            StateTimes = 0;
            int ret = 10;//AioGetAiStartInRange(Id, AiChannel, ref Level1, ref Level2, ref StateTimes);
            return ret;
        }
        public override int GetAiStartInRangeEx(short Id, short AiChannel, out float Level1, out float Level2, out int StateTimes)
        {
            Level1 = 0;
            Level2 = 0;
            StateTimes = 0;
            int ret = 10;//AioGetAiStartInRangeEx(Id, AiChannel, ref Level1, ref Level2, ref StateTimes);
            return ret;
        }
        public override int SetAiStartOutRange(short Id, short AiChannel, int Level1, int Level2, int StateTimes)
        {
            int ret = 10;//AioSetAiStartOutRange(Id, AiChannel, Level1, Level2, StateTimes);
            return ret;
        }
        public override int SetAiStartOutRangeEx(short Id, short AiChannel, float Level1, float Level2, int StateTimes)
        {
            int ret = 10;//AioSetAiStartOutRangeEx(Id, AiChannel, Level1, Level2, StateTimes);
            return ret;
        }
        public override int GetAiStartOutRange(short Id, short AiChannel, out int Level1, out int Level2, out int StateTimes)
        {
            Level1 = 0;
            Level2 = 0;
            StateTimes = 0;
            int ret = 10;//AioGetAiStartOutRange(Id, AiChannel, ref Level1, ref Level2, ref StateTimes);
            return ret;
        }
        public override int GetAiStartOutRangeEx(short Id, short AiChannel, out float Level1, out float Level2, out int StateTimes)
        {
            Level1 = 0;
            Level2 = 0;
            StateTimes = 0;
            int ret = 10;//AioGetAiStartOutRangeEx(Id, AiChannel, ref Level1, ref Level2, ref StateTimes);
            return ret;
        }
        public override int SetAiStopTrigger(short Id, short AiStopTrigger)
        {
            //Its on timer
            int ret = 0;//AioSetAiStopTrigger(Id, AiStopTrigger);
            return ret;
        }
        public override int GetAiStopTrigger(short Id, out short AiStopTrigger)
        {
            AiStopTrigger = 0;
            int ret = 10;//AioGetAiStopTrigger(Id, ref AiStopTrigger);
            return ret;
        }
        //TODO: Hacked: n_samples is read-only!
        int n_samples = 0;
        public override int SetAiStopTimes(short Id, int AiStopTimes)
        {
            n_samples = AiStopTimes;
            //sets the length of device sampling. But it is given as parameter of retrieveData
            int ret = 0;//AioSetAiStopTimes(Id, AiStopTimes);
            return ret;
        }
        public override int GetAiStopTimes(short Id, out int AiStopTimes)
        {
            AiStopTimes = 0;
            int ret = 10;//AioGetAiStopTimes(Id, ref AiStopTimes);
            return ret;
        }
        public override int SetAiStopLevel(short Id, short AiChannel, int AiStopLevel, short AiDirection)
        {
            int ret = 10;//AioSetAiStopLevel(Id, AiChannel, AiStopLevel, AiDirection);
            return ret;
        }
        public override int SetAiStopLevelEx(short Id, short AiChannel, float AiStopLevel, short AiDirection)
        {
            int ret = 10;//AioSetAiStopLevelEx(Id, AiChannel, AiStopLevel, AiDirection);
            return ret;
        }
        public override int GetAiStopLevel(short Id, short AiChannel, out int AiStopLevel, out short AiDirection)
        {
            AiStopLevel = 0;
            AiDirection = 0;
            int ret = 10;//AioGetAiStopLevel(Id, AiChannel, ref AiStopLevel, ref AiDirection);
            return ret;
        }
        public override int GetAiStopLevelEx(short Id, short AiChannel, out float AiStopLevel, out short AiDirection)
        {
            AiStopLevel = 0;
            AiDirection = 0;
            int ret = 10;//AioGetAiStopLevelEx(Id, AiChannel, ref AiStopLevel, ref AiDirection);
            return ret;
        }
        public override int SetAiStopInRange(short Id, short AiChannel, int Level1, int Level2, int StateTimes)
        {
            int ret = 10;//AioSetAiStopInRange(Id, AiChannel, Level1, Level2, StateTimes);
            return ret;
        }
        public override int SetAiStopInRangeEx(short Id, short AiChannel, float Level1, float Level2, int StateTimes)
        {
            int ret = 10;//AioSetAiStopInRangeEx(Id, AiChannel, Level1, Level2, StateTimes);
            return ret;
        }
        public override int GetAiStopInRange(short Id, short AiChannel, out int Level1, out int Level2, out int StateTimes)
        {
            Level1 = 0;
            Level2 = 0;
            StateTimes = 0;
            int ret = 10;//AioGetAiStopInRange(Id, AiChannel, ref Level1, ref Level2, ref StateTimes);
            return ret;
        }
        public override int GetAiStopInRangeEx(short Id, short AiChannel, out float Level1, out float Level2, out int StateTimes)
        {
            Level1 = 0;
            Level2 = 0;
            StateTimes = 0;
            int ret = 10;//AioGetAiStopInRangeEx(Id, AiChannel, ref Level1, ref Level2, ref StateTimes);
            return ret;
        }
        public override int SetAiStopOutRange(short Id, short AiChannel, int Level1, int Level2, int StateTimes)
        {
            int ret = 10;//AioSetAiStopOutRange(Id, AiChannel, Level1, Level2, StateTimes);
            return ret;
        }
        public override int SetAiStopOutRangeEx(short Id, short AiChannel, float Level1, float Level2, int StateTimes)
        {
            int ret = 10;//AioSetAiStopOutRangeEx(Id, AiChannel, Level1, Level2, StateTimes);
            return ret;
        }
        public override int GetAiStopOutRange(short Id, short AiChannel, out int Level1, out int Level2, out int StateTimes)
        {
            Level1 = 0;
            Level2 = 0;
            StateTimes = 0;
            int ret = 10;//AioGetAiStopOutRange(Id, AiChannel, ref Level1, ref Level2, ref StateTimes);
            return ret;
        }
        public override int GetAiStopOutRangeEx(short Id, short AiChannel, out float Level1, out float Level2, out int StateTimes)
        {
            Level1 = 0;
            Level2 = 0;
            StateTimes = 0;
            int ret = 10;//AioGetAiStopOutRangeEx(Id, AiChannel, ref Level1, ref Level2, ref StateTimes);
            return ret;
        }
        public override int SetAiStopDelayTimes(short Id, int AiStopDelayTimes)
        {
            int ret = 10;//AioSetAiStopDelayTimes(Id, AiStopDelayTimes);
            return ret;
        }
        public override int GetAiStopDelayTimes(short Id, out int AiStopDelayTimes)
        {
            AiStopDelayTimes = 0;
            int ret = 10;//AioGetAiStopDelayTimes(Id, ref AiStopDelayTimes);
            return ret;
        }

        int loop_handle = 0;
        public override int SetAiEvent(short Id, uint hWnd, int AiEvent)
        {
            //AiEvent is code for what messages to send (try send end event to keep simple!)
            loop_handle = (int)hWnd;
            int ret = 0;//AioSetAiEvent(Id, hWnd, AiEvent);
            return ret;
        }
        public override int GetAiEvent(short Id, out uint hWnd, out int AiEvent)
        {
            hWnd = 0;
            AiEvent = 0;
            int ret = 10;//AioGetAiEvent(Id, ref hWnd, ref AiEvent);
            return ret;
        }
        unsafe public override int SetAiCallBackProc(short Id, IntPtr pAiCallBack, int AiEvent, void* Param)
        {
            int ret = 10;//AioSetAiCallBackProc(Id, pAiCallBack, AiEvent, Param);
            return ret;
        }
        public override int SetAiEventSamplingTimes(short Id, int AiSamplingTimes)
        {
            //Receive event!
            int ret = 0;//AioSetAiEventSamplingTimes(Id, AiSamplingTimes);
            return ret;
        }
        public override int GetAiEventSamplingTimes(short Id, out int AiSamplingTimes)
        {
            AiSamplingTimes = 0;
            int ret = 10;//AioGetAiEventSamplingTimes(Id, ref AiSamplingTimes);
            return ret;
        }
        public override int SetAiEventTransferTimes(short Id, int AiTransferTimes)
        {
            int ret = 10;//AioSetAiEventTransferTimes(Id, AiTransferTimes);
            return ret;
        }
        public override int GetAiEventTransferTimes(short Id, out int AiTransferTimes)
        {
            AiTransferTimes = 0;
            int ret = 10;//AioGetAiEventTransferTimes(Id, ref AiTransferTimes);
            return ret;
        }
        public override int StartAi(short Id)
        {
            //CaioConst.AIE_END | CaioConst.AIE_DATA_NUM | CaioConst.AIE_DATA_TSF

            const uint MSG_END = 0x1002;
            const uint MSG_DATA = 0x1003;

            //Just for demo (not adding data)
            for (int i = 0; i < 40; i++)
            {
                for (long device_id = 0; device_id < 2; device_id++)
                {
                    SendMessage(loop_handle, MSG_DATA, device_id, 0);
                }
                Thread.Sleep(100);
            }

            for (long device_id = 0; device_id < 2; device_id++) {
                SendMessage(loop_handle, MSG_END, device_id, n_samples);
            }
            int ret = 0;//AioStartAi(Id);
            return ret;
        }
        public override int StartAiSync(short Id, int TimeOut)
        {
            int ret = 10;//AioStartAiSync(Id, TimeOut);
            return ret;
        }
        public override int StopAi(short Id)
        {
            int ret = 10;//AioStopAi(Id);
            return ret;
        }
        public override int GetAiStatus(short Id, out int AiStatus)
        {
            AiStatus = 0;
            int ret = 10;//AioGetAiStatus(Id, ref AiStatus);
            return ret;
        }
        public override int GetAiSamplingCount(short Id, out int AiSamplingCount)
        {
            AiSamplingCount = 0;
            int ret = 10;//AioGetAiSamplingCount(Id, ref AiSamplingCount);
            return ret;
        }
        public override int GetAiStopTriggerCount(short Id, out int AiStopTriggerCount)
        {
            AiStopTriggerCount = 0;
            int ret = 10;//AioGetAiStopTriggerCount(Id, ref AiStopTriggerCount);
            return ret;
        }
        public override int GetAiTransferCount(short Id, out int AiTransferCount)
        {
            AiTransferCount = 0;
            int ret = 10;//AioGetAiTransferCount(Id, ref AiTransferCount);
            return ret;
        }
        public override int GetAiTransferLap(short Id, out int Lap)
        {
            Lap = 0;
            int ret = 10;//AioGetAiTransferLap(Id, ref Lap);
            return ret;
        }
        public override int GetAiStopTriggerTransferCount(short Id, out int Count)
        {
            Count = 0;
            int ret = 10;//AioGetAiStopTriggerTransferCount(Id, ref Count);
            return ret;
        }
        public override int GetAiRepeatCount(short Id, out int AiRepeatCount)
        {
            AiRepeatCount = 0;
            int ret = 10;//AioGetAiRepeatCount(Id, ref AiRepeatCount);
            return ret;
        }
        public override int GetAiSamplingData(short Id, ref int AiSamplingTimes, ref int[] AiData)
        {
            int num_samples = AiSamplingTimes;
            int n_data = state.n_channels * num_samples;
            int pos = 0;
            for (int i=0;i< num_samples; i++)
            {
                for (int j = 0; j < state.n_channels; j++)
                {
                    pos = (i * state.n_channels) + j;
                    AiData[pos] = nextWord(Id, j);
                }
            }

            int ret = 0;//AioGetAiSamplingData(Id, ref AiSamplingTimes, AiData);
            return ret;
        }
        public override int GetAiSamplingDataEx(short Id, ref int AiSamplingTimes, ref float[] AiData)
        {
            int ret = 10;//AioGetAiSamplingDataEx(Id, ref AiSamplingTimes, AiData);
            return ret;
        }
        public override int ResetAiStatus(short Id)
        {
            int ret = 10;//AioResetAiStatus(Id);
            return ret;
        }
        public override int ResetAiMemory(short Id)
        {
            int ret = 0;//AioResetAiMemory(Id);
            return ret;
        }

        //Analog Output Function
        public override int SingleAo(short Id, short AoChannel, int AoData)
        {
            int ret = 10;//AioSingleAo(Id, AoChannel, AoData);
            return ret;
        }
        public override int SingleAoEx(short Id, short AoChannel, float AoData)
        {
            int ret = 10;//AioSingleAoEx(Id, AoChannel, AoData);
            return ret;
        }
        public override int MultiAo(short Id, short AoChannels, int[] AoData)
        {
            int ret = 10;//AioMultiAo(Id, AoChannels, AoData);
            return ret;
        }
        public override int MultiAoEx(short Id, short AoChannels, float[] AoData)
        {
            int ret = 10;//AioMultiAoEx(Id, AoChannels, AoData);
            return ret;
        }
        public override int GetAoResolution(short Id, out short AoResolution)
        {
            AoResolution = 0;
            int ret = 10;//AioGetAoResolution(Id, ref AoResolution);
            return ret;
        }
        public override int SetAoChannels(short Id, short AoChannels)
        {
            int ret = 10;//AioSetAoChannels(Id, AoChannels);
            return ret;
        }
        public override int GetAoChannels(short Id, out short AoChannels)
        {
            AoChannels = 0;
            int ret = 10;//AioGetAoChannels(Id, ref AoChannels);
            return ret;
        }
        public override int GetAoMaxChannels(short Id, out short AoMaxChannels)
        {
            AoMaxChannels = 0;
            int ret = 10;//AioGetAoMaxChannels(Id, ref AoMaxChannels);
            return ret;
        }
        public override int SetAoRange(short Id, short AoChannel, short AoRange)
        {
            int ret = 10;//AioSetAoRange(Id, AoChannel, AoRange);
            return ret;
        }
        public override int SetAoRangeAll(short Id, short AoRange)
        {
            int ret = 10;//AioSetAoRangeAll(Id, AoRange);
            return ret;
        }
        public override int GetAoRange(short Id, short AoChannel, out short AoRange)
        {
            AoRange = 0;
            int ret = 10;//AioGetAoRange(Id, AoChannel, ref AoRange);
            return ret;
        }
        public override int SetAoTransferMode(short Id, short AoTransferMode)
        {
            int ret = 10;//AioSetAoTransferMode(Id, AoTransferMode);
            return ret;
        }
        public override int GetAoTransferMode(short Id, out short AoTransferMode)
        {
            AoTransferMode = 0;
            int ret = 10;//AioGetAoTransferMode(Id, ref AoTransferMode);
            return ret;
        }
        public override int SetAoDeviceBufferMode(short Id, short AoDeviceBufferMode)
        {
            int ret = 10;//AioSetAoDeviceBufferMode(Id, AoDeviceBufferMode);
            return ret;
        }
        public override int GetAoDeviceBufferMode(short Id, out short AoDeviceBufferMode)
        {
            AoDeviceBufferMode = 0;
            int ret = 10;//AioGetAoDeviceBufferMode(Id, ref AoDeviceBufferMode);
            return ret;
        }
        public override int SetAoMemorySize(short Id, short AoMemorySize)
        {
            int ret = 10;//AioSetAoMemorySize(Id, AoMemorySize);
            return ret;
        }
        public override int GetAoMemorySize(short Id, out int AoMemorySize)
        {
            AoMemorySize = 0;
            int ret = 10;//AioGetAoMemorySize(Id, ref AoMemorySize);
            return ret;
        }
        public override int SetAoTransferData(short Id, int DataNumber, IntPtr Buffer)
        {
            int ret = 10;//AioSetAoTransferData(Id, DataNumber, Buffer);
            return ret;
        }
        public override int GetAoSamplingDataSize(short Id, out short DataSize)
        {
            DataSize = 0;
            int ret = 10;//AioGetAoSamplingDataSize(Id, ref DataSize);
            return ret;
        }
        public override int SetAoMemoryType(short Id, short AoMemoryType)
        {
            int ret = 10;//AioSetAoMemoryType(Id, AoMemoryType);
            return ret;
        }
        public override int GetAoMemoryType(short Id, out short AoMemoryType)
        {
            AoMemoryType = 0;
            int ret = 10;//AioGetAoMemoryType(Id, ref AoMemoryType);
            return ret;
        }
        public override int SetAoRepeatTimes(short Id, int AoRepeatTimes)
        {
            int ret = 10;//AioSetAoRepeatTimes(Id, AoRepeatTimes);
            return ret;
        }
        public override int GetAoRepeatTimes(short Id, out int AoRepeatTimes)
        {
            AoRepeatTimes = 0;
            int ret = 10;//AioGetAoRepeatTimes(Id, ref AoRepeatTimes);
            return ret;
        }
        public override int SetAoClockType(short Id, short AoClockType)
        {
            int ret = 10;//AioSetAoClockType(Id, AoClockType);
            return ret;
        }
        public override int GetAoClockType(short Id, out short AoClockType)
        {
            AoClockType = 0;
            int ret = 10;//AioGetAoClockType(Id, ref AoClockType);
            return ret;
        }
        public override int SetAoSamplingClock(short Id, float AoSamplingClock)
        {
            int ret = 10;//AioSetAoSamplingClock(Id, AoSamplingClock);
            return ret;
        }
        public override int GetAoSamplingClock(short Id, out float AoSamplingClock)
        {
            AoSamplingClock = 0;
            int ret = 10;//AioGetAoSamplingClock(Id, ref AoSamplingClock);
            return ret;
        }
        public override int SetAoClockEdge(short Id, short AoClockEdge)
        {
            int ret = 10;//AioSetAoClockEdge(Id, AoClockEdge);
            return ret;
        }
        public override int GetAoClockEdge(short Id, out short AoClockEdge)
        {
            AoClockEdge = 0;
            int ret = 10;//AioGetAoClockEdge(Id, ref AoClockEdge);
            return ret;
        }
        public override int SetAoSamplingData(short Id, int AoSamplingTimes, int[] AoData)
        {
            int ret = 10;//AioSetAoSamplingData(Id, AoSamplingTimes, AoData);
            return ret;
        }
        public override int SetAoSamplingDataEx(short Id, int AoSamplingTimes, float[] AoData)
        {
            int ret = 10;//AioSetAoSamplingDataEx(Id, AoSamplingTimes, AoData);
            return ret;
        }
        public override int GetAoSamplingTimes(short Id, out int AoSamplingTimes)
        {
            AoSamplingTimes = 0;
            int ret = 10;//AioGetAoSamplingTimes(Id, ref AoSamplingTimes);
            return ret;
        }
        public override int SetAoStartTrigger(short Id, short AoStartTrigger)
        {
            int ret = 10;//AioSetAoStartTrigger(Id, AoStartTrigger);
            return ret;
        }
        public override int GetAoStartTrigger(short Id, out short AoStartTrigger)
        {
            AoStartTrigger = 0;
            int ret = 10;//AioGetAoStartTrigger(Id, ref AoStartTrigger);
            return ret;
        }
        public override int SetAoStopTrigger(short Id, short AoStopTrigger)
        {
            int ret = 10;//AioSetAoStopTrigger(Id, AoStopTrigger);
            return ret;
        }
        public override int GetAoStopTrigger(short Id, out short AoStopTrigger)
        {
            AoStopTrigger = 0;
            int ret = 10;//AioGetAoStopTrigger(Id, ref AoStopTrigger);
            return ret;
        }
        public override int SetAoEvent(short Id, uint hWnd, int AoEvent)
        {
            int ret = 10;//AioSetAoEvent(Id, hWnd, AoEvent);
            return ret;
        }
        public override int GetAoEvent(short Id, out uint hWnd, out int AoEvent)
        {
            hWnd = 0;
            AoEvent = 0;
            int ret = 10;//AioGetAoEvent(Id, ref hWnd, ref AoEvent);
            return ret;
        }
        unsafe public override int SetAoCallBackProc(short Id, IntPtr pAoCallBack, int AoEvent, void* Param)
        {
            int ret = 10;//AioSetAoCallBackProc(Id, pAoCallBack, AoEvent, Param);
            return ret;
        }
        public override int SetAoEventSamplingTimes(short Id, int AoSamplingTimes)
        {
            int ret = 10;//AioSetAoEventSamplingTimes(Id, AoSamplingTimes);
            return ret;
        }
        public override int GetAoEventSamplingTimes(short Id, out int AoSamplingTimes)
        {
            AoSamplingTimes = 0;
            int ret = 10;//AioGetAoEventSamplingTimes(Id, ref AoSamplingTimes);
            return ret;
        }
        public override int SetAoEventTransferTimes(short Id, int AoTransferTimes)
        {
            int ret = 10;//AioSetAoEventTransferTimes(Id, AoTransferTimes);
            return ret;
        }
        public override int GetAoEventTransferTimes(short Id, out int AoTransferTimes)
        {
            AoTransferTimes = 0;
            int ret = 10;//AioGetAoEventTransferTimes(Id, ref AoTransferTimes);
            return ret;
        }
        public override int StartAo(short Id)
        {
            int ret = 10;//AioStartAo(Id);
            return ret;
        }
        public override int StopAo(short Id)
        {
            int ret = 10;//AioStopAo(Id);
            return ret;
        }
        public override int EnableAo(short Id, short AoChannel)
        {
            int ret = 10;//AioEnableAo(Id, AoChannel);
            return ret;
        }
        public override int DisableAo(short Id, short AoChannel)
        {
            int ret = 10;//AioDisableAo(Id, AoChannel);
            return ret;
        }
        public override int GetAoStatus(short Id, out int AoStatus)
        {
            AoStatus = 0;
            int ret = 10;//AioGetAoStatus(Id, ref AoStatus);
            return ret;
        }
        public override int GetAoSamplingCount(short Id, out int AoSamplingCount)
        {
            AoSamplingCount = 0;
            int ret = 10;//AioGetAoSamplingCount(Id, ref AoSamplingCount);
            return ret;
        }
        public override int GetAoTransferCount(short Id, out int AoTransferCount)
        {
            AoTransferCount = 0;
            int ret = 10;//AioGetAoTransferCount(Id, ref AoTransferCount);
            return ret;
        }
        public override int GetAoTransferLap(short Id, out int Lap)
        {
            Lap = 0;
            int ret = 10;//AioGetAoTransferLap(Id, ref Lap);
            return ret;
        }
        public override int GetAoRepeatCount(short Id, out int AoRepeatCount)
        {
            AoRepeatCount = 0;
            int ret = 10;//AioGetAoRepeatCount(Id, ref AoRepeatCount);
            return ret;
        }
        public override int ResetAoStatus(short Id)
        {
            int ret = 10;//AioResetAoStatus(Id);
            return ret;
        }
        public override int ResetAoMemory(short Id)
        {
            int ret = 10;//AioResetAoMemory(Id);
            return ret;
        }

        //Digital Input and Output Function
        public override int SetDiFilter(short Id, short Bit, float Value)
        {
            int ret = 10;//AioSetDiFilter(Id, Bit, Value);
            return ret;
        }
        public override int GetDiFilter(short Id, short Bit, out float Value)
        {
            Value = 0;
            int ret = 10;//AioGetDiFilter(Id, Bit, ref Value);
            return ret;
        }
        public override int InputDiBit(short Id, short DiBit, out short DiData)
        {
            DiData = 0;
            int ret = 10;//AioInputDiBit(Id, DiBit, ref DiData);
            return ret;
        }
        public override int OutputDoBit(short Id, short DoBit, short DoData)
        {
            int ret = 10;//AioOutputDoBit(Id, DoBit, DoData);
            return ret;
        }
        public override int InputDiByte(short Id, short DiPort, out short DiData)
        {
            DiData = 0;
            int ret = 10;//AioInputDiByte(Id, DiPort, ref DiData);
            return ret;
        }
        public override int OutputDoByte(short Id, short DoPort, short DoData)
        {
            int ret = 10;//AioOutputDoByte(Id, DoPort, DoData);
            return ret;
        }
        public override int SetDioDirection(short Id, int Dir)
        {
            int ret = 10;//AioSetDioDirection(Id, Dir);
            return ret;
        }
        public override int GetDioDirection(short Id, out int Dir)
        {
            Dir = 0;
            int ret = 10;//AioGetDioDirection(Id, ref Dir);
            return ret;
        }

        //Counter Function
        public override int GetCntMaxChannels(short Id, out short CntMaxChannels)
        {
            CntMaxChannels = 0;
            int ret = 10;//AioGetCntMaxChannels(Id, ref CntMaxChannels);
            return ret;
        }
        public override int SetCntComparisonMode(short Id, short CntChannel, short CntMode)
        {
            int ret = 10;//AioSetCntComparisonMode(Id, CntChannel, CntMode);
            return ret;
        }
        public override int GetCntComparisonMode(short Id, short CntChannel, out short CntMode)
        {
            CntMode = 0;
            int ret = 10;//AioGetCntComparisonMode(Id, CntChannel, ref CntMode);
            return ret;
        }
        public override int SetCntPresetReg(short Id, short CntChannel, int PresetNumber, int[] PresetData, short Flag)
        {
            int ret = 10;//AioSetCntPresetReg(Id, CntChannel, PresetNumber, PresetData, Flag);
            return ret;
        }
        public override int SetCntComparisonReg(short Id, short CntChannel, int ComparisonNumber, int[] ComparisonData, short Flag)
        {
            int ret = 10;//AioSetCntComparisonReg(Id, CntChannel, ComparisonNumber, ComparisonData, Flag);
            return ret;
        }
        public override int SetCntInputSignal(short Id, short CntChannel, short CntInputSignal)
        {
            int ret = 10;//AioSetCntInputSignal(Id, CntChannel, CntInputSignal);
            return ret;
        }
        public override int GetCntInputSignal(short Id, short CntChannel, out short CntInputSignal)
        {
            CntInputSignal = 0;
            int ret = 10;//AioGetCntInputSignal(Id, CntChannel, ref CntInputSignal);
            return ret;
        }
        public override int SetCntEvent(short Id, short CntChannel, uint hWnd, int CntEvent)
        {
            int ret = 10;//AioSetCntEvent(Id, CntChannel, hWnd, CntEvent);
            return ret;
        }
        public override int GetCntEvent(short Id, short CntChannel, out uint hWnd, out int CntEvent)
        {
            hWnd = 0;
            CntEvent = 0;
            int ret = 10;//AioGetCntEvent(Id, CntChannel, ref hWnd, ref CntEvent);
            return ret;
        }
        unsafe public override int SetCntCallBackProc(short Id, short CntChannel, IntPtr pCntCallBack, int CntEvent, void* Param)
        {
            int ret = 10;//AioSetCntCallBackProc(Id, CntChannel, pCntCallBack, CntEvent, Param);
            return ret;
        }
        public override int SetCntFilter(short Id, short CntChannel, short Signal, float Value)
        {
            int ret = 10;//AioSetCntFilter(Id, CntChannel, Signal, Value);
            return ret;
        }
        public override int GetCntFilter(short Id, short CntChannel, short Signal, out float Value)
        {
            Value = 0;
            int ret = 10;//AioGetCntFilter(Id, CntChannel, Signal, ref Value);
            return ret;
        }
        public override int StartCnt(short Id, short CntChannel)
        {
            int ret = 10;//AioStartCnt(Id, CntChannel);
            return ret;
        }
        public override int StopCnt(short Id, short CntChannel)
        {
            int ret = 10;//AioStopCnt(Id, CntChannel);
            return ret;
        }
        public override int PresetCnt(short Id, short CntChannel, int PresetData)
        {
            int ret = 10;//AioPresetCnt(Id, CntChannel, PresetData);
            return ret;
        }
        public override int GetCntStatus(short Id, short CntChannel, out int CntStatus)
        {
            CntStatus = 0;
            int ret = 10;//AioGetCntStatus(Id, CntChannel, ref CntStatus);
            return ret;
        }
        public override int GetCntCount(short Id, short CntChannel, out int Count)
        {
            Count = 0;
            int ret = 10;//AioGetCntCount(Id, CntChannel, ref Count);
            return ret;
        }
        public override int ResetCntStatus(short Id, short CntChannel, int CntStatus)
        {
            int ret = 10;//AioResetCntStatus(Id, CntChannel, CntStatus);
            return ret;
        }

        //Timer Function
        public override int SetTmEvent(short Id, short TimerId, uint hWnd, int TmEvent)
        {
            int ret = 10;//AioSetTmEvent(Id, TimerId, hWnd, TmEvent);
            return ret;
        }
        public override int GetTmEvent(short Id, short TimerId, out uint hWnd, out int TmEvent)
        {
            hWnd = 0;
            TmEvent = 0;
            int ret = 10;//AioGetTmEvent(Id, TimerId, ref hWnd, ref TmEvent);
            return ret;
        }
        unsafe public override int SetTmCallBackProc(short Id, short TimerId, IntPtr pTmCallBack, int TmEvent, void* Param)
        {
            int ret = 10;//AioSetTmCallBackProc(Id, TimerId, pTmCallBack, TmEvent, Param);
            return ret;
        }
        public override int StartTmTimer(short Id, short TimerId, float Interval)
        {
            int ret = 10;//AioStartTmTimer(Id, TimerId, Interval);
            return ret;
        }
        public override int StopTmTimer(short Id, short TimerId)
        {
            int ret = 10;//AioStopTmTimer(Id, TimerId);
            return ret;
        }
        public override int StartTmCount(short Id, short TimerId)
        {
            int ret = 10;//AioStartTmCount(Id, TimerId);
            return ret;
        }
        public override int StopTmCount(short Id, short TimerId)
        {
            int ret = 10;//AioStopTmCount(Id, TimerId);
            return ret;
        }
        public override int LapTmCount(short Id, short TimerId, out int Lap)
        {
            Lap = 0;
            int ret = 10;//AioLapTmCount(Id, TimerId, ref Lap);
            return ret;
        }
        public override int ResetTmCount(short Id, short TimerId)
        {
            int ret = 10;//AioResetTmCount(Id, TimerId);
            return ret;
        }
        public override int TmWait(short Id, short TimerId, int Wait)
        {
            int ret = 10;//AioTmWait(Id, TimerId, Wait);
            return ret;
        }

        //Event Controller
        public override int SetEcuSignal(short Id, short Destination, short Source)
        {
            int ret = 10;//AioSetEcuSignal(Id, Destination, Source);
            return ret;
        }
        public override int GetEcuSignal(short Id, short Destination, out short Source)
        {
            Source = 0;
            int ret = 10;//AioGetEcuSignal(Id, Destination, ref Source);
            return ret;
        }


        // Setting function (set)
        public override int GetCntmMaxChannels(short Id, out short CntmMaxChannels)
        {
            CntmMaxChannels = 0;
            int ret = 10;//AioGetCntmMaxChannels(Id, ref CntmMaxChannels);
            return ret;
        }
        public override int SetCntmZMode(short Id, short ChNo, short Mode)
        {
            int ret = 10;//AioSetCntmZMode(Id, ChNo, Mode);
            return ret;
        }
        public override int SetCntmZLogic(short Id, short ChNo, short ZLogic)
        {
            int ret = 10;//AioSetCntmZLogic(Id, ChNo, ZLogic);
            return ret;
        }
        public override int SelectCntmChannelSignal(short Id, short ChNo, short SigType)
        {
            int ret = 10;//AioSelectCntmChannelSignal(Id, ChNo, SigType);
            return ret;
        }
        public override int SetCntmCountDirection(short Id, short ChNo, short Dir)
        {
            int ret = 10;//AioSetCntmCountDirection(Id, ChNo, Dir);
            return ret;
        }
        public override int SetCntmOperationMode(short Id, short ChNo, short Phase, short Mul, short SyncClr)
        {
            int ret = 10;//AioSetCntmOperationMode(Id, ChNo, Phase, Mul, SyncClr);
            return ret;
        }
        public override int SetCntmDigitalFilter(short Id, short ChNo, short FilterValue)
        {
            int ret = 10;//AioSetCntmDigitalFilter(Id, ChNo, FilterValue);
            return ret;
        }
        public override int SetCntmPulseWidth(short Id, short ChNo, short PlsWidth)
        {
            int ret = 10;//AioSetCntmPulseWidth(Id, ChNo, PlsWidth);
            return ret;
        }
        public override int SetCntmDIType(short Id, short ChNo, short InputType)
        {
            int ret = 10;//AioSetCntmDIType(Id, ChNo, InputType);
            return ret;
        }
        public override int SetCntmOutputHardwareEvent(short Id, short ChNo, short OutputLogic, uint EventType, short PulseWidth)
        {
            int ret = 10;//AioSetCntmOutputHardwareEvent(Id, ChNo, OutputLogic, EventType, PulseWidth);
            return ret;
        }
        public override int SetCntmInputHardwareEvent(short Id, short ChNo, uint EventType, short RF0, short RF1, short Reserved)
        {
            int ret = 10;//AioSetCntmInputHardwareEvent(Id, ChNo, EventType, RF0, RF1, Reserved);
            return ret;
        }
        public override int SetCntmCountMatchHardwareEvent(short Id, short ChNo, short RegisterNo, uint EventType, short Reserved)
        {
            int ret = 10;//AioSetCntmCountMatchHardwareEvent(Id, ChNo, RegisterNo, EventType, Reserved);
            return ret;
        }
        public override int SetCntmPresetRegister(short Id, short ChNo, uint PresetData, short Reserved)
        {
            int ret = 10;//AioSetCntmPresetRegister(Id, ChNo, PresetData, Reserved);
            return ret;
        }
        public override int SetCntmTestPulse(short Id, short CntmInternal, short CntmOut, short CntmReserved)
        {
            int ret = 10;//AioSetCntmTestPulse(Id, CntmInternal, CntmOut, CntmReserved);
            return ret;
        }

        // Setting function (get)
        public override int GetCntmZMode(short Id, short ChNo, out short Mode)
        {
            Mode = 1;
            int ret = 10;//AioGetCntmZMode(Id, ChNo, ref Mode);
            return ret;
        }
        public override int GetCntmZLogic(short Id, short ChNo, out short ZLogic)
        {
            ZLogic = 1;
            int ret = 10;//AioGetCntmZLogic(Id, ChNo, ref ZLogic);
            return ret;
        }
        public override int GetCntmChannelSignal(short Id, short CntmChNo, out short CntmSigType)
        {
            CntmSigType = 1;
            int ret = 10;//AioGetCntmChannelSignal(Id, CntmChNo, ref CntmSigType);
            return ret;
        }
        public override int GetCntmCountDirection(short Id, short ChNo, out short Dir)
        {
            Dir = 1;
            int ret = 10;//AioGetCntmCountDirection(Id, ChNo, ref Dir);
            return ret;
        }
        public override int GetCntmOperationMode(short Id, short ChNo, out short Phase, out short Mul, out short SyncClr)
        {
            Phase = 1;
            Mul = 0;
            SyncClr = 0;
            int ret = 10;//AioGetCntmOperationMode(Id, ChNo, ref Phase, ref Mul, ref SyncClr);
            return ret;
        }
        public override int GetCntmDigitalFilter(short Id, short ChNo, out short FilterValue)
        {
            FilterValue = 0;
            int ret = 10;//AioGetCntmDigitalFilter(Id, ChNo, ref FilterValue);
            return ret;
        }
        public override int GetCntmPulseWidth(short Id, short ChNo, out short PlsWidth)
        {
            PlsWidth = 0;
            int ret = 10;//AioGetCntmPulseWidth(Id, ChNo, ref PlsWidth);
            return ret;
        }

        // Counter function
        public override int CntmStartCount(short Id, short[] ChNo, short ChNum)
        {
            int ret = 10;//AioCntmStartCount(Id, ChNo, ChNum);
            return ret;
        }
        public override int CntmStopCount(short Id, short[] ChNo, short ChNum)
        {
            int ret = 10;//AioCntmStopCount(Id, ChNo, ChNum);
            return ret;
        }
        public override int CntmPreset(short Id, short[] ChNo, short ChNum, uint[] PresetData)
        {
            int ret = 10;//AioCntmPreset(Id, ChNo, ChNum, PresetData);
            return ret;
        }
        public override int CntmZeroClearCount(short Id, short[] ChNo, short ChNum)
        {
            int ret = 10;//AioCntmZeroClearCount(Id, ChNo, ChNum);
            return ret;
        }
        public override int CntmReadCount(short Id, short[] ChNo, short ChNum, uint[] CntDat)
        {
            int ret = 10;//AioCntmReadCount(Id, ChNo, ChNum, CntDat);
            return ret;
        }
        public override int CntmReadStatus(short Id, short ChNo, out short Sts)
        {
            Sts = 0;
            int ret = 10;//AioCntmReadStatus(Id, ChNo, ref Sts);
            return ret;
        }
        public override int CntmReadStatusEx(short Id, short ChNo, out uint Sts)
        {
            Sts = 0;
            int ret = 10;//AioCntmReadStatusEx(Id, ChNo, ref Sts);
            return ret;
        }

        // Notify function
        public override int CntmNotifyCountUp(short Id, short ChNo, short RegNo, uint Count, int hWnd)
        {
            int ret = 10;//AioCntmNotifyCountUp(Id, ChNo, RegNo, Count, hWnd);
            return ret;
        }
        public override int CntmStopNotifyCountUp(short Id, short ChNo, short RegNo)
        {
            int ret = 10;//AioCntmStopNotifyCountUp(Id, ChNo, RegNo);
            return ret;
        }
        unsafe public override int CntmCountUpCallbackProc(short Id, IntPtr pAioCntmCountUpCallBack, void* Param)
        {
            int ret = 10;//AioCntmCountUpCallbackProc(Id, pAioCntmCountUpCallBack, Param);
            return ret;
        }
        public override int CntmNotifyCounterError(short Id, int hWnd)
        {
            int ret = 10;//AioCntmNotifyCounterError(Id, hWnd);
            return ret;
        }
        public override int CntmStopNotifyCounterError(short Id)
        {
            int ret = 10;//AioCntmStopNotifyCounterError(Id);
            return ret;
        }
        unsafe public override int CntmCounterErrorCallbackProc(short Id, IntPtr pAioCntmCounterErrorCallBack, void* Param)
        {
            int ret = 10;//AioCntmCounterErrorCallbackProc(Id, pAioCntmCounterErrorCallBack, Param);
            return ret;
        }
        public override int CntmNotifyCarryBorrow(short Id, int hWnd)
        {
            int ret = 10;//AioCntmNotifyCarryBorrow(Id, hWnd);
            return ret;
        }
        public override int CntmStopNotifyCarryBorrow(short Id)
        {
            int ret = 10;//AioCntmStopNotifyCarryBorrow(Id);
            return ret;
        }
        unsafe public override int CntmCarryBorrowCallbackProc(short Id, IntPtr pAioCntmCarryBorrowCallBack, void* Param)
        {
            int ret = 10;//AioCntmCarryBorrowCallbackProc(Id, pAioCntmCarryBorrowCallBack, Param);
            return ret;
        }
        public override int CntmNotifyTimer(short Id, uint TimeValue, int hWnd)
        {
            int ret = 10;//AioCntmNotifyTimer(Id, TimeValue, hWnd);
            return ret;
        }
        public override int CntmStopNotifyTimer(short Id)
        {
            int ret = 10;//AioCntmStopNotifyTimer(Id);
            return ret;
        }
        unsafe public override int CntmTimerCallbackProc(short Id, IntPtr pAioCntmTmCallBack, void* Param)
        {
            int ret = 10;//AioCntmTimerCallbackProc(Id, pAioCntmTmCallBack, Param);
            return ret;
        }

        // General purpose input function
        public override int CntmInputDIByte(short Id, short Reserved, out byte bData)
        {
            bData = 0;
            int ret = 10;//AioCntmInputDIByte(Id, Reserved, ref bData);
            return ret;
        }
        public override int CntmOutputDOBit(short Id, short AiomChNo, short Reserved, byte OutData)
        {
            int ret = 10;//AioCntmOutputDOBit(Id, AiomChNo, Reserved, OutData);
            return ret;
        }
    }
}