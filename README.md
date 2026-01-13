# AI Agent for Dyson Sphere Program

This project enables an AI agent to analyze and interact with "Dyson Sphere Program" (DSP) using the Model Context Protocol (MCP).

## Architecture

1.  **Game Mod (`plugin/`)**: A C# BepInEx mod that embeds a WebSocket server inside the game. It exposes internal game data (production stats, planet info) via JSON-RPC.
2.  **MCP Server (`mcp-server/`)**: A Python server using `fastmcp`. It connects to the Game Mod via WebSocket and exposes high-level MCP Tools to the AI (e.g., Claude, IDEs).

## Setup & Usage

### 1. The Game Mod (Plugin)
*   **Source:** `plugin/`
*   **Build:** `cd plugin && dotnet build`
    *   This automatically copies the DLL to your r2modman profile (configured in `src/Directory.Build.props`).
*   **Run:** Start Dyson Sphere Program (via r2modman/Steam).
    *   Verify it works: Check `%AppData%\r2modmanPlus-local\DysonSphereProgram\profiles\Default\BepInEx\LogOutput.log` for "MCP Server started".

### 2. The MCP Server (Python)
*   **Source:** `mcp-server/`
*   **Install:** `pip install -r mcp-server/requirements.txt`
*   **Run:** `python mcp-server/server.py`
    *   This server connects to the running game and exposes MCP tools.

## Available MCP Tools

The MCP server exposes the following tools for AI agents to interact with the game:

### Game Information
- **list_tools**: List all available MCP tools and their descriptions
- **get_game_info**: Get basic game info including version, current planet, and star count

### Research & Technology
- **get_research_progress**: Get current research tech, progress percentage, and total hash rate across all planets
- **get_research_by_planet**: Get research hash rate breakdown by planet, showing lab counts, working/idle labs, and hash/sec per planet
- **get_tech_queue**: Get the current research queue with progress on each queued technology
- **get_upgrades**: Get mecha upgrades, research speed multipliers, logistics capacities, and Dyson sphere unlocks
- **get_lab_details(planetId)**: Get details for all labs on a specific planet, including individual research speed and the globally researched tech

### Logistics
- **list_ils_per_planet**: List all Interstellar Logistics Stations (ILS) grouped by planet, including drone/ship counts, warper status, energy levels, and item storage with logistics modes (supply/demand)

## Development
See `AGENTS.md` for CLI instructions and debugging tips.