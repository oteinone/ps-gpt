namespace PowershellGpt.ConsoleApp;

static class ClaimConsole
{
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    static extern bool AttachConsole(int dwProcessId);
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    static extern bool FreeConsole();

    public static bool Claim()
    {
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            bool freed = FreeConsole();
            bool attached = AttachConsole(-1);
            Console.SetIn(new StreamReader(Console.OpenStandardInput()));
            return true;
        }
        return false;
    }
}