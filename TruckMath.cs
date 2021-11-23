using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckRemoteServer
{
    public static class TruckMath
    {
        public static T Max<T>(params T[] values)
        {
            return values.Max();
        }
    }
}
