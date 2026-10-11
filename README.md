# PoemCreator

A C# console application for creating, customizing, and saving poems with AI assistance.

PoemCreator uses the Gemini API to generate original poems based on a selected mood and user-provided seed lines. If the API is unavailable or not configured, the application uses a local collection of poems as a fallback.

## Features

- 🎭 **Multiple moods** such as Romantic, Melancholic, Hopeful, Dark, and Peaceful
- ✍️ **Custom poem generation** with selectable seed lines and adjustable length
- 🤖 **Gemini AI integration** with a local fallback
- 💾 **SQLite database** for saving, viewing, and deleting poems
- 🎨 **Console UI** with colors, interactive menus, and animated progress feedback

## Tech Stack

- C# / .NET 10
- SQLite
- Google Gemini API
- `Microsoft.Data.Sqlite`

## Getting Started

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A Gemini API key for AI-powered generation (optional)

### Installation

Clone the repository and navigate to the project directory:

```bash
git clone https://github.com/TranceMeli/PoemCreator.git
cd PoemCreator
dotnet restore
```

### Configuration

Create an `appsettings.json` file in the project directory:

```json
{
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY"
  }
}
```

Alternatively, configure the `GEMINI_API_KEY` environment variable.

**Security:** Never commit your real API key. Keep `appsettings.json` out of version control.

### Run the Application

```bash
dotnet run
```

The SQLite database is created automatically when the application starts.

## Project Status

This project is a learning and personal development project focused on C#, console UI design, API integration, and database persistence.

## License

MIT