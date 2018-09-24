using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Maps indexes of type: event:state -> newstate, [callback]

namespace MultiDeviceAIO
{
    class StateMachine
    {
        public delegate void CallBack(string index);

        private const string ALLSTATECODE = "#######";     //any state matches

        public string state { get; set; }

        public StateMachine(object state)
        {
            this.state = state.ToString();
        }

        Dictionary<string, string> rules = new Dictionary<string, string>();
        Dictionary<string, CallBack> callbacks = new Dictionary<string, CallBack>();

        static string CreateIndex(object stateevent, object state)
        {
            return stateevent.ToString() + ":" + state.ToString();
        }

        public static string OR(params object[] args)
        {
            string res = "";
            foreach (object arg in args)
            {
                res += arg.ToString() + ":";
            }
            return res.Substring(0, res.Length - 1);
        }

        public void Clear()
        {
            rules.Clear();
            callbacks.Clear();
        }

        public void Event(object stateevent)
        {
            string index = CreateIndex(stateevent, state);
            string allindex = CreateIndex(stateevent, ALLSTATECODE);

            Console.WriteLine("EVENT: " + stateevent);

            if (rules.ContainsKey(index))
            {
                state = rules[index];
                Console.WriteLine("EXEC");
            }
            else if (rules.ContainsKey(allindex))
            {
                state = rules[allindex];
                Console.WriteLine("EXEC");
            }

            //if doesn't exist then null
            if (callbacks.ContainsKey(index))
            {
                callbacks[index](index);
                Console.WriteLine("EXEC");
            }
            else if (callbacks.ContainsKey(allindex))
            {
                callbacks[allindex](index);
                Console.WriteLine("EXEC");
            }
        }

        public void AddRule(object state, object stateevent, object newstate, CallBack func = null)
        {
            string[] arrSE = stateevent.ToString().Split(':');
            string[] arrS = (state ?? ALLSTATECODE).ToString().Split(':');
            foreach (string se in arrSE)
            {
                foreach (string s in arrS)
                {
                    string index = CreateIndex(se, s);

                    Console.WriteLine("Add Rule: " + index + "->" + newstate + " : " + func);
                    rules.Add(index, newstate.ToString());

                    if (func != null) callbacks.Add(index, func);
                }
            }
        }

        public void AddRule(object state, object stateevent, CallBack func = null)
        {
            string[] arrSE = stateevent.ToString().Split(':');
            string[] arrS = (state ?? ALLSTATECODE).ToString().Split(':');
            foreach (string se in arrSE)
            {
                foreach (string s in arrS)
                {
                    string index = CreateIndex(se, s);
                    callbacks.Add(index, func);
                }
            }
        }


    }
}
