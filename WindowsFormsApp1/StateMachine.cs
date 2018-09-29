using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Maps indexes of type: event:state -> newstate, [callback]

//TODO: func return bool indicate whether to change state

namespace MultiDeviceAIO
{
    class StateMachine
    {
        private string log = "";

        public delegate void CallBack(string index);

        private const string ALLSTATECODE = "#######";     //any state matches

        public string state { get; set; }
        string name;

        //TODO: make this generic
        //restrict as best to Enum
        public bool IsState<T>(T enumtype) where T : struct, IConvertible
        {
            return state == enumtype.ToString();
        }

        public StateMachine(string name, object state)
        {
            this.name = name;
            this.state = state.ToString();
        }

        public void Save()
        {
            File.WriteAllText(PersistentLoggerState.ps.data.testpath + @"\" + name + "_state.txt", log);
        }

        void WriteLine(string line)
        {
            log += line + "\n";
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
            string debug = state + "->", match = "";
            string index = CreateIndex(stateevent, state);
            string allindex = CreateIndex(stateevent, ALLSTATECODE);

            if (rules.ContainsKey(index))
            {
                match = rules[index] + "(" + index + ")";
                state = rules[index];
            }
            else if (rules.ContainsKey(allindex))
            {
                match = rules[allindex] + "(" + allindex + ")";
                state = rules[allindex];
            }

            if (match == "")
            {
                match = "nomatch";
            }

            //if doesn't exist then null
            if (callbacks.ContainsKey(index))
            {
                match += "\tEXEC";
                callbacks[index](index);
            }
            else if (callbacks.ContainsKey(allindex))
            {
                match += "\tEXECANY";
                callbacks[allindex](index);
            }

            WriteLine("#" + this.GetHashCode() + "\t%" + stateevent.ToString() + "%\t" + debug + "\t" + match);

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
