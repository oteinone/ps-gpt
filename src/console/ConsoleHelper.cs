using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PowershellGpt.ConsoleApp;

static class ConsoleHelper
{
    public static bool ReclaimConsole()
    {
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return RedirectConsole("CON");
        }
        else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            || System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RedirectConsole("/dev/tty");
        }
        
        return false;
    }

    private static bool RedirectConsole(string fileStream)
    {
        var stream = new FileStream(fileStream, FileMode.Open, FileAccess.Read);
        var reader = new StreamReader(stream);
        Console.SetIn(reader);
        return true;
    }
}