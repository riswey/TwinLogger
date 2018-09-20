using System;

namespace MotorController
{
    class SerialEventsController
    {
        static string SIGNATURE = "\x0E\x0E";
        static string TERMINAL = "\n";

        public static string EncodePacket(byte cmd, string data = "")
        {
            return SIGNATURE + (char)cmd + data + TERMINAL;
        }

        /*
         * @return  false if packet is not recognised
         */
        public static int DecodePacket(string packet, out byte cmd, out string data)
        {
            cmd = 0; data = "";
            if (packet.Length < 3) return 1;                                        //header too small (min 3)
            if (packet.Substring(0, 2) != SIGNATURE) return 2;                      //packet signature failed
            string contents = packet.TrimEnd(Environment.NewLine.ToCharArray());    //ensure packet \n removed
            cmd = (byte)contents[2];
            data = contents.Substring(3);
            return 0;
        }


    }
}
