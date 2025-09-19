# Playground — AI + Aspire

This repo is a personal playground for experimenting with AI features and bleeding-edge .NET stuff (Aspire, Dev Containers, Blazor, gRPC, .NET 10). It is intentionally informal — not a best-practices example. Shortcuts were taken, opinions were ignored, and duct tape may be present.

## What this is
- A small app to play with LLM's and AI features.
- Built with Aspire for local orchestration and devcontainers for a consistent environment.
- Not production-ready; useful for learning and tinkering.
- Developed with Visual Studio Code but should also work in Visual Studio.

## Prerequisites
- Devcontainer ready.
- Aspire CLI available in the container (project uses Aspire to run services).
- .NET SDK (included in the devcontainer).
- If you want local OpenAI access, an OpenAI account with credits and an API key (see below).

## Sample data (.sampledata)
This repo includes a `.sampledata` folder with sample CSV/JSON data and helper scripts.

To prepare the sample data:
1. Extract the archive (example):
```bash
cd .sampledata
tar -xzvf games.tar.gz
```

2. Generate the SQL insert script (example):
```bash
# Make script executable once:
chmod +x createGameInsertScript.sh

# Create the SQL file (adjust paths as needed)
./createGameInsertScript.sh games.csv ../GameCatalog/Playground.GameCatalog.Migrations/DataSeeding/01_game_inserts.sql
```

The above will produce `.sql` files that your migrations/seeding logic can pick up.

## Run the app
Start everything with Aspire:
```bash
aspire run
```

For hot reload during development:
```bash
aspire run -w
```

- F5 in VS Code will also launch the Aspire configuration (if you use the provided debug profile) and attach the debugger.

## First run
On first run you must configure your OpenAI API key:
- Open the Aspire dashboard, go to the **Resources** screen for the API service, there should be an error banner on top to configure the missing key.

## Debugging tips
- Set breakpoints in VS Code and press F5.
- Use `aspire run -w` for hot reload so code changes are picked up faster.

## Notes & caveats
- This is a playground: expect shortcuts, simplified error handling, and experiments.
- Sensitive values should live in Aspire resources, user secrets, or environment variables — not in this repo.

Have fun exploring — and remember: if something breaks, you probably learned something useful first. Enjoy the chaos.