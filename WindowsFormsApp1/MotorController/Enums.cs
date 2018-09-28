using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;


//TODO: check that RA (GETADC) responds with "RA float"

namespace MotorController
{
    //packet enums
    enum CMD                //sent packets
    {
        //Respond with "ACK CallingCMD"
        START,
        STOP,
        TRIGGER,
        SETFREQ,
        SETLOCK,
        SETUNLOCK,
        //SETPULSEDELAY,
        SETPID,
        SETADC,
        //Respond with "GetCode data"
        //GETPULSEDELAY,      //PW
        GETROTORFREQ,       //CF
        GETTARGETFREQ,      //FW
        //GETMINMAXPERIODS,   //MM
        GETPID,             //PID
        GETLOCKABLE,
        GETADC              //RA         
    };

    enum DATATYPES          //return packets
    {
        ACK,                //ACK (response to S commands)
        GETPULSEDELAY,      //PW
        GETROTORFREQ,       //CF
        GETTARGETFREQ,      //FW
        GETMINMAXPERIODS,   //MM
        GETPID,             //PID
        GETLOCKABLE,        //Ready to be locked
        GETADC              //Get Clock Frequency
    }

    //State enums
    enum STATE { Ready, Running, Lockable, Locked, Triggered }
    enum EVENT { Next, Send_Start, Send_Stop, Send_Lock, Send_Unlock, Send_Trigger,
        Do_Start, Do_Stop, Do_Lock, Do_unlock, Do_Trigger, Do_SetPulseDelay, Do_SetPID, Do_SetFreq, Do_SetADC }

    class Enums
    {
        //Should split into get/set commands
        //then dictionary translated the get commands to their returning code
        //e.g. GETPULSEDELAY (RD) -> (PW)

        //Convert GET statement return codes to Calling GET statements
        public static readonly Dictionary<string, DATATYPES> DATATYPEDecode = new Dictionary<string, DATATYPES>()
        {
            { "ACK", DATATYPES.ACK },
            //{ "PW", DATATYPES.GETPULSEDELAY },
            { "CF", DATATYPES.GETROTORFREQ },
            { "FW", DATATYPES.GETTARGETFREQ },
            { "PID", DATATYPES.GETPID },
            //{ "MM", DATATYPES.GETMINMAXPERIODS },
            { "TL", DATATYPES.GETLOCKABLE },
            { "RA", DATATYPES.GETADC },
            //TL/RL

        };

        //Convert Command to Serial Code
        public static readonly Dictionary<CMD, string> CMDEncode = new Dictionary<CMD, string>()
        {
            {CMD.START, "SB" },
            {CMD.STOP, "SE" },
            {CMD.TRIGGER, "SS" },
            {CMD.SETFREQ, "SF" },
            {CMD.SETLOCK, "ST" },
            {CMD.SETUNLOCK, "SU" },
            //{CMD.SETPULSEDELAY, "SD" },
            {CMD.SETPID, "SP" },
            {CMD.SETADC, "SA" },

//            {CMD.GETPULSEDELAY, "RD" },
            {CMD.GETROTORFREQ, "RC" },
            {CMD.GETTARGETFREQ, "RF" },
            //{CMD.GETMINMAXPERIODS, "RM" },
            {CMD.GETPID, "RP" },
            {CMD.GETLOCKABLE, "RL" },
            {CMD.GETADC, "AF" }
        };

        //Convert Serial Code to Command (ACK return codes = Calling codes)
        private static Dictionary<string, CMD> _CMDDecode { get; set; } = null;
        public static Dictionary<string, CMD> CMDDecode
        {
            get
            {
                if (_CMDDecode == null)
                {
                    _CMDDecode = Enums.CMDEncode.ToDictionary(kp => kp.Value, kp => kp.Key);
                }
                return _CMDDecode;
            }
        }

        //TODO: check that only these ACKS exist
        public static readonly Dictionary<string, EVENT> ACKDecode = new Dictionary<string, EVENT>() {
            {"SB", EVENT.Do_Start },
            {"SE", EVENT.Do_Stop},
            {"SS", EVENT.Do_Trigger},
            {"SF", EVENT.Do_SetFreq},
            {"ST", EVENT.Do_Lock},
            {"SU", EVENT.Do_unlock},
            {"SD", EVENT.Do_SetPulseDelay},
            {"SP", EVENT.Do_SetPID},
            {"SA", EVENT.Do_SetADC},
        };
    }
}
