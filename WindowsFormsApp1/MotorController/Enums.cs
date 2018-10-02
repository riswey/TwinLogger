using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;


//TODO: check that RA (GETADC) responds with "RA float"

namespace MotorController
{
    //packet enums
    public enum CMD                //sent packets
    {
        //Respond with "ACK CallingCMD"
        START,
        STOP,
        TRIGGER,
        SETFREQ,
        SETLOCK,
        SETUNLOCK,
        //SETPULSEDELAY,
        //SETPID,
        SETADC,
        SETPOWER,
        
        //Respond with "GetCode data"
        //GETPULSEDELAY,        //PW
        GETROTORFREQ,           //CF
        GETTARGETFREQ,          //FW
        //GETMINMAXPERIODS,     //MM
        GETPID,                 //PID
        GETLOCKABLE,
        GETADC,                 //RA
        GETHALLNO,              //0,1 which motor, 2 null
        GETPOWER                //current prev %power

    };

    public enum DATATYPES          //return packets
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
    public enum ARDUINOSTATE { Ready, Running, Lockable, Locked, Triggered }
    public enum ARDUINOEVENT { Next, Send_Start, Send_Stop, Send_Lock, Send_Unlock, Send_Trigger,
        Do_Start, Do_Stop, Do_Lock, Do_unlock, Do_Trigger, Do_SetPulseDelay, Do_SetPID, Do_SetFreq, Do_SetADC }

    public class Enums
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
            { "TL", DATATYPES.GETLOCKABLE },            //
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
            //{CMD.SETPID, "SP" },              //could cut I,D when stable and bump the p
            {CMD.SETADC, "SA" },
            {CMD.SETPOWER, "SW" },              //%

//            {CMD.GETPULSEDELAY, "RD" },
            {CMD.GETROTORFREQ, "RC" },
            {CMD.GETTARGETFREQ, "RF" },
            //{CMD.GETMINMAXPERIODS, "RM" },
            {CMD.GETPID, "RP" },
            {CMD.GETLOCKABLE, "RL" },           //Y min max %power over last 10secs
            {CMD.GETADC, "RA" },
            {CMD.GETHALLNO, "RH" },
            {CMD.GETPOWER, "RW" }               //current prev power levels

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
        public static readonly Dictionary<string, ARDUINOEVENT> ACKDecode = new Dictionary<string, ARDUINOEVENT>() {
            {"SB", ARDUINOEVENT.Do_Start },
            {"SE", ARDUINOEVENT.Do_Stop},
            {"SS", ARDUINOEVENT.Do_Trigger},
            {"SF", ARDUINOEVENT.Do_SetFreq},
            {"ST", ARDUINOEVENT.Do_Lock},
            {"SU", ARDUINOEVENT.Do_unlock},
            {"SD", ARDUINOEVENT.Do_SetPulseDelay},
            {"SP", ARDUINOEVENT.Do_SetPID},
            {"SA", ARDUINOEVENT.Do_SetADC},
        };
    }
}
