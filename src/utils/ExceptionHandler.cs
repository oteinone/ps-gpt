namespace PowershellGpt.Exceptions;

public static class ExceptionHandler
{
    public static void HandleException(Exception e)
    {
        if (e is UserException)
        {
            Console.WriteLine(e.Message);
        }
        else if (e is Spectre.Console.Cli.CommandParseException)
        {
            Console.WriteLine("Error parsing the command: " + e.Message);
        }
        else DebugStackTrace(e);
    }

    static void DebugStackTrace(Exception e, int stackCount = 10)
    {
        if (stackCount < 0) return;
        Console.Error.WriteLine($"Error: {e.GetType()}{Environment.NewLine}Message: {e.Message}{Environment.NewLine}{e.StackTrace}");
        if (e.InnerException != null) DebugStackTrace(e.InnerException, stackCount - 1);
    }
}