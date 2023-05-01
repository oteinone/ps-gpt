# About

This project allows you to use Azure OpenAI conversational models directly from console.

The project has currently been tested with Windows 10 / .NET 6.0 / Windows Terminal / Powershell

# Usage
- Define Azure OpenAI key in env variable AZURE_OPENAI_API_KEY to avoid inputting it manually every time
- First argument will be used as gpt model prompt
  - Default prompt value is "You are an AI assistant that helps people find information."
- `gpt` initializes conversation
- `gpt "You are a scottish assistant that speaks with a thick scottish accent"` initializes conversation with a scottish assistant
- To start a multiline question type one backtick (`) and press enter to start multiline entry
  - To finalize a multiline question type one backtick (`) again and press enter to finish multiline entry
- Input `exit` / `quit` / `q` / `done` (or ctrl+c for a more ... uncivilized solution) to quit the program

# Environment variables
## Available variables
- `AZURE_OPENAI_ENDPOINT_URL`
  - Url to your Azure OpenAI instance
  - e.g. `https://my-openai-instance.openai.azure.com/`
- `AZURE_OPENAI_MODEL_NAME`
  - The deployed model used to converse with openai
  - e.g. `gpt-test-deployment`
  - **Note! Only gpt-35 has been tested by the author**
- `AZURE_OPENAI_API_KEY`
  - The KEY 1 from your Azure OpenAI instance
  - e.g. `319868aa671247f67a9baa0000000000`
- `AZURE_OPENAI_CHAT_PROMPT`
  - Persist a custom prompt between sessions
  - If you set a custom prompt with env variable, you will not be shown the prompt response every time.
  - Note that gpt model might refuse to accept your prompt so test your prompts first using 
- To set the keys in Powershell use e.g. `$env:AZURE_OPENAI_ENDPOINT_URL = "https://my-openai-instance.openai.azure.com/";`

## About variables
`ps-gpt` will ask you for these credentials on launch (save for chat prompt). If you want to really use the tool, set them as env variables.

Better/persistent solution and secure storage for the key might be coming in the future. Or not...

# Development

- `dotnet run` run the tool
- `dotnet pack` release as a tool (nuget package)
- `dotnet tool install -g --add-source .\nupkg\ ps-gpt` install the tool in your local toolchain
- `dotnet tool update -g --add-source .\nupkg\ ps-gpt` update instead of install    