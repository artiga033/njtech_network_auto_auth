using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf
{
    public static class Utils
    {
        public static readonly string ExecutablePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
    }
}
