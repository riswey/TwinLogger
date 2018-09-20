using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

namespace MotorController
{
    //packet enums
    enum CMD                //sent packets
    {
        //Respond with ACK CMD
        START,
        STOP,               
        SETFREQ,            
        SETLOCK,
        SETUNLOCK,
        SETPULSEDELAY,
        SETPID,
        //Respond with data
        GETPULSEDELAY,      //PW
        GETROTORFREQ,       //CF
        GETTARGETFREQ,      //FW
        GETMINMAXPERIODS,   //MM
        GETPID,             //PID
        GETLOCKABLE
    };

    enum DATATYPES          //return packets
    {
        ACK,                 //ACK (response to S commands)
        GETPULSEDELAY,      //PW
        GETROTORFREQ,       //CF
        GETTARGETFREQ,      //FW
        GETMINMAXPERIODS,   //MM
        GETPID,             //PID
        GETLOCKABLE         //Ready to be locked
    }

    //State enums
    enum STATE { Ready = 0, Running, Lockable, Locked }       //2 bit
    enum EVENT { Start = 0, Stop, Lock, Unlock }              //2 bit

    class Enums
    {
        //Should split into get/set commands
        //then dictionary translated the get commands to their returning code
        //e.g. GETPULSEDELAY (RD) -> (PW)

        //These are the serial codes for packets returned by the calling GET statements
        public static readonly Dictionary<string, DATATYPES> DATATYPEDecode = new Dictionary<string, DATATYPES>()
        {
            { "ACK", DATATYPES.ACK },
            { "PW", DATATYPES.GETPULSEDELAY },
            { "CF", DATATYPES.GETROTORFREQ },
            { "FW", DATATYPES.GETTARGETFREQ },
            { "PID", DATATYPES.GETPID },
            { "MM", DATATYPES.GETMINMAXPERIODS },
            { "TL", DATATYPES.GETLOCKABLE },
            //TL/RL

        };

        public static readonly Dictionary<CMD, string> CMDEncode = new Dictionary<CMD, string>()
        {
            {CMD.START, "SB" },
            {CMD.STOP, "SE" },
            {CMD.SETFREQ, "SF" },
            {CMD.SETLOCK, "ST" },
            {CMD.SETUNLOCK, "SU" },
            {CMD.SETPULSEDELAY, "SD" },
            {CMD.SETPID, "SP" },

            {CMD.GETPULSEDELAY, "RD" },
            {CMD.GETROTORFREQ, "RC" },
            {CMD.GETTARGETFREQ, "RF" },
            {CMD.GETMINMAXPERIODS, "RM" },
            {CMD.GETPID, "RP" },
            {CMD.GETLOCKABLE, "RL" }
        };

        private static Dictionary<string, CMD> _CMDDecode { get; set; } = null;
        public static Dictionary<string, CMD> CMDDecode {
            get
            {
                if (_CMDDecode == null)
                {
                    _CMDDecode = Enums.CMDEncode.ToDictionary(kp => kp.Value, kp => kp.Key);
                }
                return _CMDDecode;
            }
        }

    }
}
