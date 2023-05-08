// See https://aka.ms/new-console-template for more information
using PowershellGpt.ConsoleApp;
using Spectre.Console.Cli;

// Initialize application and configuration
var app = new CommandApp<GptCommand>();
app.Configure(config => {
    config.SetApplicationName("ps-gpt");
    config.ValidateExamples();
});

return app.Run(args);