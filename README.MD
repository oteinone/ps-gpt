# About

This project allows you to use Azure OpenAI conversational models directly from console.

The project has currently been tested with Windows 10 / .NET 6.0 / Windows Terminal / Powershell

# Installation

## Installation with `dotnet`
Install the latest published version of this tool from nuget repository using `dotnet tool install -g ps-gpt` or see development section to see how to pack and install from source code.
# Usage

- `gpt` initializes conversation
- First argument will be used as gpt model prompt
  - `gpt "You are a scottish assistant that speaks with a thick scottish accent"` initializes conversation with a scottish assistant
  - Default prompt value is "You are an AI assistant that helps people find information."
- First time you use the tool you will be asked to enter API information
  - Select between OpenAI Api and Azure OpenAI Api
  - Enter deployment name (Azure OpenAI) or model name (OpenAI)
- Your selections will be stored in app configuration file for next time
  - Your key is protected using DPAPI on windows but currently unprotected on other OS!
  - `gpt --clear` removes the local configuration
- To start a multiline question type one backtick (`) and press enter to start multiline entry
  - To finalize a multiline question type one backtick (`) again and press enter to finish multiline entry
- Input `exit` / `quit` / `q` / `done` (or ctrl+c for a more ... uncivilized solution) to quit the program

# Configuration
## Config parameters
- Use `gpt --prompt` to modify the default prompt
  - `gpt --prompt "New prompt value"` sets a new default prompt value
  - `gpt --prompt` clears any custom prompt values
## Clear configuration
- Use `gpt --clear` to remove existing configuration and you will be prompted for a new config the next time you run `gpt`


# Development

## Basic development commands
- `dotnet run` run the tool
- `dotnet pack` release as a tool (build a nuget package)
- `dotnet tool install -g --add-source .\nupkg\ ps-gpt` install the tool from locally built package
- `dotnet tool update -g --add-source .\nupkg\ ps-gpt` update instead of install    

## Build your own executable
You can also make a self-contained executable and locate it somewhere in your path
```
$rid = "win-x64" // {win/osx/linux}-{x64/arm64}
dotnet publish `
   -c Release `
   -r $rid `
   -p:PublishSingleFile=true `
   --self-contained true `
   --output "releases/$rid"
```
See ful list of runtime identifiers [here](https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.NETCore.Platforms/src/runtime.json)