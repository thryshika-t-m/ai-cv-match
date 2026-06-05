# ai-cv-match

.NET 8 Web API and Angular UI that compare a CV (PDF) to a job description and return a structured match report.

## Features

- `POST /api/cv-match` — multipart form upload (`cvPdf` + `jobDescription`)
- Extracts text from PDF via [PdfPig](https://github.com/UglyToad/PdfPig)
- **Pluggable AI providers** (config-driven):
  - **Ollama** (local, default) — no cloud quota
  - **Groq** (cloud, free tier) — one API key
  - **Google Gemini** (cloud, free tier) — use short inputs to save quota
- Returns JSON: `matchScore`, `matchedSkills`, `skillGaps`, `recommendations`

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) 18+ (for the Angular UI)
- **Ollama** (default provider): [ollama.com](https://ollama.com) + `ollama pull llama3.2`
- **Groq** (optional): [console.groq.com](https://console.groq.com) API key
- **Gemini** (optional): [Google AI Studio](https://aistudio.google.com/apikey) API key

## AI provider configuration

Set the active provider in `appsettings.json` or `appsettings.Development.json`:

```json
"Analysis": {
  "Provider": "Ollama"
}
```

Allowed values: `Ollama`, `Groq`, `Gemini`.

### Ollama (default — portfolio demos)

```json
"Ollama": {
  "BaseUrl": "http://localhost:11434",
  "Model": "llama3.2"
}
```

```bash
ollama pull llama3.2
ollama serve   # if not already running
```

### Groq (cloud)

```json
"Analysis": { "Provider": "Groq" },
"Groq": {
  "ApiKey": "",
  "Model": "llama-3.1-8b-instant"
}
```

```bash
dotnet user-secrets set "Groq:ApiKey" "YOUR_GROQ_KEY"
# or environment variable GROQ_API_KEY
```

### Gemini (cloud)

```json
"Analysis": { "Provider": "Gemini" },
"Gemini": {
  "ApiKey": "",
  "Model": "gemini-2.0-flash-lite"
}
```

```bash
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_KEY"
# or GEMINI_API_KEY
```

Use a **short PDF** and **2–3 sentence** job description for Gemini free-tier demos. Try `gemini-2.5-flash` or `gemini-1.5-flash` if one model hits quota limits.

## Run locally

**API**

```bash
cd src/AiCvMatch.Api
dotnet run
```

**Angular UI**

```bash
cd client/ai-cv-match-ui
npm install
npm start
```

Open `http://localhost:4200` (proxies `/api` to `http://localhost:5196`).

## API

### `POST /api/cv-match`

**Content-Type:** `multipart/form-data`

| Field | Type | Description |
|-------|------|-------------|
| `cvPdf` | file | PDF resume (max 10 MB) |
| `jobDescription` | string | Job description text |

**Example (curl):**

```bash
curl -X POST "http://localhost:5196/api/cv-match" \
  -F "cvPdf=@resume.pdf" \
  -F "jobDescription=Senior .NET developer with Azure and REST API experience."
```

**Example response:**

```json
{
  "matchScore": 78,
  "matchedSkills": ["ASP.NET Core", "REST APIs", "C#"],
  "skillGaps": ["Azure Kubernetes Service", "GraphQL"],
  "recommendations": [
    "Highlight any Azure deployments in project descriptions.",
    "Add metrics or outcomes to backend API work."
  ]
}
```

## Project structure

```
src/AiCvMatch.Api/
  Controllers/CvMatchController.cs
  Services/IMatchAnalysisService.cs
  Services/OllamaMatchService.cs
  Services/GroqMatchService.cs
  Services/GeminiMatchService.cs
  Services/Analysis/MatchAnalysisPrompt.cs
client/ai-cv-match-ui/
  src/app/components/cv-match/
```

## Notes

- PDFs must contain extractable text (scanned image-only PDFs are not supported without OCR).
- Uploaded files are not stored; processing is in-memory per request.
