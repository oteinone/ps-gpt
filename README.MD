# About

This project allows you to use Azure OpenAI conversational models directly from console.

The project has currently been tested with 
- Windows 10 / .NET 7.0 / Windows Terminal / Powershell
- WSL / Ubuntu 22.04 / .NET 7.0 / Windows Terminal / bash

https://user-images.githubusercontent.com/4796019/235936234-0c1329ac-1d33-4fe1-b501-77741b436da8.mp4


# Installation

## Installation with `dotnet`
Install the latest published version of this tool from nuget repository using `dotnet tool install -g ps-gpt` or see development section to see how to pack and install from source code.
# Usage

- `gpt` initializes conversation
- First argument will be used as gpt model message
  - Default prompt value is "You are an AI assistant that helps people find information."
- First time you use the tool you will be asked to enter API information
  - Select between OpenAI Api and Azure OpenAI Api
  - Enter deployment name (Azure OpenAI) or model name (OpenAI)
- Your selections will be stored in app configuration file for next time
  - Your key is protected using DPAPI on windows but currently unprotected on other OS!
  - `gpt --clear` removes the local configuration
- To start a multiline question type one backtick (`) and press enter to start multiline entry
  - To finalize a multiline question type one backtick (`) again and press enter to finish multiline entry
- If you pipe input into the application (`echo "Give me a list of wine grapes" | gpt`) it will query the gpt model with the default/parameter settings and pipe the input to stdout.
- You can also use a "system prompt" to send a "system" type message to the model
  - `gpt --system-prompt "You are a scottish assistant that speaks with a thick scottish accent"` initializes conversation with a scottish assistant using a system message
- Input `exit` / `quit` / `q` / `done` (or ctrl+c for a more ... uncivilized solution) to quit the program

# Configuration
## Manage configuraiton
- Use `gpt --clear` to remove existing configuration and you will be prompted for a new config the next time you run `gpt`
- Use `gpt --get-profile` to see current profile settings
- Use `gpt --set-profile model=gpt3.5` to set an individual profile setting
- Use `-c, --chat` to continue the conversation even when receiving input from parameters, e.g. `gpt "Tell me about youself" -c`
  - Note! On Windows you can even use `echo "Tell me about yourself | gpt -c` to pipe in a question and still enter chat mode
- Use `gpt --system-prompt "You are Albert Einstein, the famous physician"` to initialize the conversation with a system type message
## Templates
- You can set a prompt template for the profile using `gpt --set-profile default_prompt_template="template"` to set a prompt template that will be applied to every first message in a conversation
  - If your template contains the string `{text}`, it will be replaced by the input text 
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
