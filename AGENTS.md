# Agent Instructions

## Git Workflow

### When User Says "Commit"
Execute the following steps:
1. **Stage changes:** `git add <files>`
2. **Commit with semantic message:** `git commit -m "<type>: <description>"`
   - Use conventional commit types: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, etc.
   - Write clear, concise commit messages describing what changed
   - Do NOT sign commits with Claude attribution
3. **Push to remote:** `git push`

**Example:**
```bash
git add README.md
git commit -m "docs: update architecture diagram with data flow"
git push
```

---

## Development Workflow

**Important:** Always use the scripts in `cmds/` for building and running services. The scripts automatically kill any existing processes on the target ports, so there's no need to manually stop them first.

### 1. Build and Deploy Plugin
Build the C# BepInEx plugin and deploy it to the game:
```bash
./cmds/build-plugin.sh
```

### 2. Check Game Logs
View the BepInEx log to verify the plugin loaded correctly:
```bash
./cmds/check-log.sh      # last 30 lines
./cmds/check-log.sh 50   # last 50 lines
```

### 3. Start API and MCP Server
Start the Python API server (port 8000) and MCP server (port 8001):
```bash
./cmds/run-api.sh
```

**Environment Variables:**
- `DSP_USE_MOCK=true` - Use mock data instead of connecting to the game
- `DSP_WS_URI=ws://localhost:18181/` - WebSocket URI for game connection (default)

**Example with mock data:**
```bash
DSP_USE_MOCK=true ./cmds/run-api.sh
```

### 4. Start Web Frontend
Start the SvelteKit web frontend (port 5173):
```bash
./cmds/run-web.sh
```

### 5. Launch Chrome for DevTools MCP
Launch Chrome with remote debugging enabled (port 9222):
```bash
./cmds/run-chrome.sh
```

### 6. Check Server Status
Check if Chrome, API, and Web servers are running:
```bash
./cmds/check-servers.sh
```

### 7. Stop All Servers
Stop all running servers:
```bash
./cmds/stop-all.sh
```

---

## MCP Server Setup

### Add Dyson Sphere MCP Server
Add the HTTP-based Dyson Sphere MCP server to Claude Code:
```bash
claude mcp add --transport http DysonSphereMCP http://localhost:8001/mcp
```

### Add Decompiler MCP Server
Add the stdio-based .NET Decompiler MCP server to Claude Code:
```bash
claude mcp add --transport stdio DecompilerServer /mnt/c/Users/James/git/DecompilerServer/bin/Debug/net8.0/DecompilerServer.exe
```

### Add Chrome DevTools MCP Server
Add the Chrome DevTools MCP server to Claude Code (requires Chrome running with `./cmds/run-chrome.sh`):
```bash
claude mcp add chrome-devtools --command npx --args chrome-devtools-mcp@latest --browserUrl http://127.0.0.1:9222
```

Or add manually to your Claude Code config:
```json
"chrome-devtools": {
  "command": "npx",
  "args": [
    "chrome-devtools-mcp@latest",
    "--browserUrl",
    "http://127.0.0.1:9222"
  ]
}
```

### Reconnect to MCP Servers
After updating an MCP server, reconnect without restarting Claude Code:
```
/mcp
```
Then select the server to reconnect (e.g., "DysonSphereMCP", "DecompilerServer", or "chrome-devtools").

---

## Available MCP Tools

### Dyson Sphere MCP (12 tools)

**Game Information:**
- `get_game_info` - Game version, current planet, star count
- `get_galaxy_details` - Galaxy seed, star count, habitable planets

**Planets & Stars:**
- `list_planets` - All planets with details
- `get_planet_resources(planetId)` - Resource veins on a planet
- `get_stars` - All stars with positions and types

**Research & Technology:**
- `get_research_progress` - Current tech, progress %, total hash rate
- `get_research_by_planet` - Per-planet research breakdown
- `get_tech_queue` - Research queue with progress
- `get_upgrades` - Mecha upgrades, research speed, logistics capacities
- `get_lab_details(planetId)` - Individual lab details for a planet

**Logistics:**
- `list_ils_per_planet` - All ILS stations grouped by planet
- `get_ils_details(planetId)` - Detailed ILS info for a planet

---

## Project Structure

```
dyson-sphere-mcp/
├── cmds/                 # Utility scripts (always use these!)
│   ├── build-plugin.sh   # Build and deploy plugin
│   ├── check-log.sh      # View game logs
│   ├── run-api.sh        # Start API + MCP server
│   ├── run-web.sh        # Start web frontend
│   ├── run-chrome.sh     # Launch Chrome with debugging
│   ├── check-servers.sh  # Check server status
│   └── stop-all.sh       # Stop all servers
├── plugin/               # C# BepInEx game mod
│   └── src/DSPMCP/       # Plugin source code
├── api/                  # Python MCP server
│   ├── server.py         # FastMCP + FastAPI server
│   ├── mock/             # Mock data for testing
│   └── pyproject.toml    # Python dependencies
├── web/                  # SvelteKit web frontend
└── README.md             # Project documentation
```

---

## Troubleshooting

### Plugin Not Loading
1. Check if the plugin DLL exists in the game directory
2. View BepInEx logs: `./cmds/check-log.sh`
3. Rebuild and redeploy: `./cmds/build-plugin.sh`

### MCP Server Connection Issues
1. Check server status: `./cmds/check-servers.sh`
2. Verify the game is running with the plugin loaded
3. Check if the API is responding: `curl http://localhost:8000/api/config`
4. Test with mock data: `DSP_USE_MOCK=true ./cmds/run-api.sh`

### WebSocket Connection Failed
1. Ensure the game is running
2. Verify the plugin started the WebSocket server (check logs for "MCP Server started")
3. Check if port 18181 is available
