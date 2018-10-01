using MotorController;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiDeviceAIO
{
    /// <summary>
    /// There will be several attempts to get the trigger right
    /// (1) over sample and trim
    /// (2) lock, mod, unlock, ->
    /// etc
    /// 
    /// so needs control of motor sm, app sm
    /// needs to send the trigger signal
    /// needs stats
    /// 
    /// </summary>
    class TriggerLogic
    {
        MovingStatsCrosses mac;
        StateMachine appsm, rsm;
        LoggerState state;

        TriggerLogic(StateMachine appsm, StateMachine rsm, LoggerState state)
        {
            
            this.appsm = appsm;
            this.rsm = rsm;
            this.state = state;
        }

        void Reset()
        {
            this.mac = new MovingStatsCrosses(state.target_speed);
        }

        void AddRotorSpeed(float speed)
        {
            mac.Add(speed);

        }



    }
}
