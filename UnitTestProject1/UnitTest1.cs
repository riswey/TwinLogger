using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        public static string MergeObjectToString(object obj, string str)
        {
            Dictionary<string, string> swaps = MergeDictionary(obj);
            //do the map swaps
            foreach (KeyValuePair<string, string> pair in swaps)
            {
                str = str.Replace("{" + pair.Key + "}", pair.Value);
            }

            return str;
        }

        public static Dictionary<string, string> MergeDictionary(object obj)
        {
            Dictionary<string, string> swaps = new Dictionary<string, string>();
            //prepare the map
            PropertyInfo[] properties = obj.GetType().GetProperties();
            foreach (var prop in properties)
            {
                string propName = prop.Name;
                swaps[propName.ToUpper()] = obj.GetType().GetProperty(propName).GetValue(obj, null).ToString();
            }

            return swaps;
        }

        [TestMethod]
        public void TestMethod1()
        {
        }
    }
}
