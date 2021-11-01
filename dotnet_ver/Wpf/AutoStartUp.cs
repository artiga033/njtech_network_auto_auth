// Modified from
// https://github.com/shadowsocks/shadowsocks-windows/blob/main/Shadowsocks.WPF/Utils/AutoStartup.cs
// License: GPLv3
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Wpf
{
    public static class AutoStartup
    {
        private static readonly string registryRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private static readonly string Key = "NJTechAutoAuth" + Utils.ExecutablePath.GetHashCode();

        public static bool Set(bool enabled)
        {
            RegistryKey? runKey = null;
            try
            {
                runKey = Registry.CurrentUser.CreateSubKey(registryRunKey, RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (runKey == null)
                {
                    return false;
                }
                if (enabled)
                {
                    runKey.SetValue(Key, Utils.ExecutablePath);
                }
                else
                {
                    runKey.DeleteValue(Key);
                }
                // When autostartup setting change, change RegisterForRestart state to avoid start 2 times
                RegisterForRestart(!enabled);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                if (runKey != null)
                {
                    try
                    {
                        runKey.Close();
                        runKey.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        public static bool Check()
        {
            RegistryKey? runKey = null;
            try
            {
                runKey = Registry.CurrentUser.CreateSubKey(registryRunKey, RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (runKey == null)
                {
                    return false;
                }
                var check = false;
                foreach (var valueName in runKey.GetValueNames())
                {
                    if (valueName.Equals(Key, StringComparison.InvariantCultureIgnoreCase))
                    {
                        check = true;
                        continue;
                    }
                    // Remove other startup keys with the same executable path. fixes #3011 and also assures compatibility with older versions
                    if (Utils.ExecutablePath.Equals(runKey.GetValue(valueName)?.ToString(), StringComparison.InvariantCultureIgnoreCase)
                        is bool matchedDuplicate && matchedDuplicate)
                    {
                        runKey.DeleteValue(valueName);
                        runKey.SetValue(Key, Utils.ExecutablePath);
                        check = true;
                    }
                }
                return check;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                if (runKey != null)
                {
                    try
                    {
                        runKey.Close();
                        runKey.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int RegisterApplicationRestart([MarshalAs(UnmanagedType.LPWStr)] string commandLineArgs, int Flags);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int UnregisterApplicationRestart();

        [Flags]
        enum ApplicationRestartFlags
        {
            RESTART_ALWAYS = 0,
            RESTART_NO_CRASH = 1,
            RESTART_NO_HANG = 2,
            RESTART_NO_PATCH = 4,
            RESTART_NO_REBOOT = 8,
        }

        // register restart after system reboot/update
        public static void RegisterForRestart(bool register)
        {
            // requested register and not autostartup
            if (register && !Check())
            {
                // escape command line parameter
                string[] args = new List<string>(Environment.GetCommandLineArgs())
                    .Select(p => p.Replace("\"", "\\\""))                   // escape " to \"
                    .Select(p => p.IndexOf(" ") >= 0 ? "\"" + p + "\"" : p) // encapsule with "
                    .ToArray();
                string cmdline = string.Join(" ", args);
                // first parameter is process command line parameter
                // needn't include the name of the executable in the command line
                RegisterApplicationRestart(cmdline, (int)(ApplicationRestartFlags.RESTART_NO_CRASH | ApplicationRestartFlags.RESTART_NO_HANG));
            }
            // requested unregister, which has no side effect
            else if (!register)
            {
                UnregisterApplicationRestart();
            }
        }
    }
}
