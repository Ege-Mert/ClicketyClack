# ClicketyClack

ClicketyClack is a small Windows app that adds click rings and optional sounds to left and right mouse clicks.

This project is unintentionally overkill. WPF is far more than this app needs. It was made entirely for entertainment purposes.

And Heavily insperied by Bandicam's mouse effects.

## What it does

- Shows a visual ring when you click
- Lets you set separate colors for left and right click
- Adds an optional cursor highlight
- Plays optional click sounds that you could play anything you want

## Run locally

```powershell
dotnet run
```

## Prerequisites

- Windows (WPF is Windows-only)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Development

```powershell
git clone <your-repo-url>
cd ClicketyClack
dotnet restore
dotnet run
```

You can work on the project in either:

- VS Code: open the folder and use the C# extension
- Visual Studio: open `WPF.sln`

## Build a release

```powershell
powershell -ExecutionPolicy Bypass -File .\Publish-Release.ps1
```

That creates a self-contained `win-x64` build and a zip file in `publish\`.

## Settings

Settings are stored in:

`%AppData%\ClicketyClack\settings.json`
