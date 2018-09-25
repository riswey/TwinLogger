using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MultiDeviceAIO
{
    public static class TypeExtensions
    {
        public static PropertyInfo[] GetFilteredProperties<Atr>(this Type type)
        {
            return type.GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(Atr))).ToArray();
        }
    }
}
