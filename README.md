# PoemCreator

<p align="left">
  <img src="https://img.shields.io/badge/C%23-.NET%2010-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="C# .NET 10" />
  <img src="https://img.shields.io/badge/SQLite-Database-003B57?style=for-the-badge&logo=sqlite&logoColor=white" alt="SQLite" />
  <img src="https://img.shields.io/badge/Google-Gemini%20API-8E75B2?style=for-the-badge&logo=googlegemini&logoColor=white" alt="Google Gemini API" />
</p>

## Preview
<img width="377" height="292" alt="image" src="https://github.com/user-attachments/assets/32e99705-bfc6-4a6b-8989-07925e97c409" />
<img width="390" height="290" alt="image" src="https://github.com/user-attachments/assets/3be2e53b-cae8-403f-82b7-16b7a6d3d4e4" />

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
