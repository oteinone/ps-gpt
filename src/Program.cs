// See https://aka.ms/new-console-template for more information
using PowershellGpt.ConsoleApp;
using Spectre.Console.Cli;

// Initialize application and configuration
var app = new CommandApp<GptCommand>();
app.Configure(config => {
    config.SetApplicationName("ps-gpt");
    config.AddExample(new string[0]);
    config.AddExample(new [] {"\"Give me a list of 10 fruit\""});
    config.AddExample(new [] { "--set-profile", "model=gpt-3.5-turbo"});

    config.ValidateExamples();
    #if DEBUG
    {
        config.SetExceptionHandler(e => DebugStackTrace(e));
    }
    #endif
});

return app.Run(args);

void DebugStackTrace(Exception e, int stackCount = 10)
{
    if (stackCount < 0) return;
    Console.Error.WriteLine($"Message: {e.Message}{Environment.NewLine}{e.StackTrace}");
    if (e.InnerException != null) DebugStackTrace(e.InnerException, stackCount - 1);
}