# About

This project allows you to use Azure OpenAI conversational models directly from console.

The project has currently been tested with 
- Windows 10 / .NET 7.0 / Windows Terminal / Powershell
- WSL / Ubuntu 22.04 / .NET 7.0 / Windows Terminal / bash

https://github.com/oteinone/ps-gpt/assets/4796019/77a83014-7816-47b3-92df-97125c448ff4

# Installation

## Installation with `dotnet`
Install the latest published version of this tool from nuget repository using `dotnet tool install -g ps-gpt` or see development section to see how to pack and install from source code.

## Installation using binary
Drop the binary somewhere in your path. The binaries are named ps-gpt by default so the commands are `ps-gpt` instead of `gpt` unless you rename or add an alias.

# Usage
- `gpt` initializes conversation
- Profile:
  - First time you use the tool you will be asked to enter your configuration
  - Select between OpenAI Api and Azure OpenAI Api
  - Enter deployment name (Azure) or model name (OpenAI), endpoint url (Azure) and api key (both)
  - Profile is persisted in a configuration file
    - On Windows your key is protected using DPAPI but it's currently stored unprotected in the config file on Linux/Mac
- Prompt:
  - You can have the prompt text as the first positional argument: `gpt "Give me a list of wine grapes"`
  - You can use content piped from a previous command as the prompt text: `echo "Give me a list of wine grapes" | gpt`
  - If no prompt is provided, "chat mode" is initialized and you can type in the prompt in the chat
- Chat mode
  - If you do not offer a prompt to the command line tool, it will enter chat mode and allow you to type your prompt in-app.
  - If you input your prompt as a parameter, the application will by default write the result into stdout and exit. If you want to continue the conversation afterwards use chat mode (`-c`)
  - Multiline input is started and ended with a line with only one backtick (`)
  - Input `exit` / `quit` / `q` / `done` (or ctrl+c for a more ... uncivilized solution) to quit the program
- Templates
  - Templates are a way to reuse instructions that work very well
  - A template should contain the characters `{0}` somewhere which will be replaced by the prompt read from arguments, pipeline or chat mode.
  - You can either read a template from a file with `-t ~/templatefile.txt` or set a default template with `gpt --set-profile default_prompt_template` to leverage templates
  - You can also use named templates without worrying about the file location using colon: `:mytemplate`
    - To set a template with name mytemplate use `echo "template content" | gpt --set-template :mytemplate` and you can then use the template with `gpt -t :mytemplate "This is my question to use with the template"`
- System prompt is a way to give persistent instructions for a conversation
  - `gpt --system-prompt "You are a scottish assistant that speaks with a thick scottish accent"` initializes conversation with a scottish assistant using a system message 
  - System prompt works poorly in gpt 3.5 but better in gpt-4. Generally using templates ("grounding") is a better idea, though.

# Pipeline examples
- Read a file and set it's contents as default prompt template (works with any profile setting)
  -  `cat midjourney_prompts.txt | gpt --set-profile default_prompt_template`
- Read a file and use it's contents as the prompt
  - `cat data-file.txt | gpt`
- Read a file and set a named template with it's contents
  - `cat template-file.txt | gpt --set-template :newtemplate`
- One-liners to write content in text editor and use it as a prompt
  - Linux: `TMPFILE=$(mktemp); nano $TMPFILE; cat $TMPFILE | gpt`
  - Powershell: `$tmpfile = New-TemporaryFile; notepad $tmpfile | Out-Null; Get-Content $tmpfile | gpt`

# Configuration
## Manage configuration
- Use `gpt --clear` to remove existing configuration and you will be prompted for a full config the next time you run `gpt`
- Use `gpt --get-profile` to see current profile settings
- Use `gpt --set-profile model=gpt3.5` or `echo "gpt3.5" | gpt --set-profile model` to set an individual profile setting
- Use `-c, --chat` to continue the conversation even when receiving input from parameters, e.g. `gpt "Tell me about youself" -c` or `echo "How many hands do crabs have?" | gpt -c`
- Use `gpt --system-prompt "You are Albert Einstein, the famous physician"` to initialize the conversation with a system prompt

## Templates
- You can set a prompt template for the profile using `gpt --set-profile default_prompt_template="Instruction: {0}"` . Template will be used to every first message in a conversation
  - `{0}` in the template will be replaced with the text content either from 
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
