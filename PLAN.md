# Implementation Plan

## Phase 1: Core Connectivity (COMPLETE)

### Objective 1: Connect Python Server to Game Mod (COMPLETE)
**Goal:** Establish a stable, bidirectional WebSocket connection between the Python "Sidecar" and the C# Game Mod.

**Details:**
- ~~The C# Mod is listening on `http://localhost:18181/` (verified).~~
- ~~The Python script (`mcp-server/server.py`) must initiate a WebSocket handshake.~~
- ~~Must handle JSON-RPC 2.0 formatting for messages sent to the game.~~
- ~~**Critical Fix:** Migrated from `HttpListener` to `WebSocketSharp` library for proper WebSocket support.~~

**Success Criteria:**
- [x] Python script runs without error.
- [x] BepInEx Log shows "Client connected".
- [x] Python script sends `{"method": "ping"}` and receives `"pong"` response from the game.
- [x] Connection persists (does not drop immediately).

### Objective 2: MCP Server Proof-of-Concept (COMPLETE)
**Goal:** Expose the Game Connection as a standard MCP Server that an AI Agent can use.

**Details:**
- ~~Use `fastmcp` to create an MCP server in `mcp-server/server.py`.~~
- ~~Implement simple tools: `get_game_status()`, `get_resources()`.~~
- ~~**Flow:** Agent -> MCP (Python) -> WebSocket -> Game (C#) -> WebSocket -> MCP (Python) -> Agent.~~
- ~~Verified using Claude Code CLI with `/mcp` reconnect.~~

**Success Criteria:**
- [x] `python mcp-server/server.py` runs and starts the MCP server.
- [x] Claude Code CLI connects to the MCP server (via stdio).
- [x] The Client lists `get_game_status` and `get_resources` in its capabilities.
- [x] Invoking tools returns JSON response from the game.

---

## Phase 2: Game Data Integration (COMPLETE)

### Objective 3: Implement Real Game Data (COMPLETE)
**Goal:** Make the C# plugin return actual game information instead of generic responses.

**Implemented:**
- [x] Research data: get_research_progress, get_research_by_planet, get_tech_queue, get_upgrades, get_lab_details
- [x] Planet data: list_planets, get_planet_resources
- [x] Galaxy data: get_galaxy_details
- [x] Star data: get_stars
- [x] Power grid: get_power_grid_status, get_power_grids_by_planet
- [x] **Logistics:** 
    - `list_ils_per_planet`: Summary of all ILS stations.
    - `get_ils_details(planetId)`: Detailed station config and storage.
    - `get_shipping_routes_for_ils(stationId)`: Specific shipping routes for a station.
    - `get_planet_routes(planetId)`: All incoming/outgoing routes for a planet.
    - `find_item_transport(itemId)`: Track specific items in transit globally.
- [x] **Production:**
    - `get_production_stats(planetId, timeLevel)`: Production/consumption rates.
    - `get_assembler_details(planetId)`: Machine working/idle status.

### Discovery: Using UI Statistics Objects Instead of Manual Calculation

**Finding (January 2026):**
While implementing power grid tools, we discovered that the game has precomputed statistics objects used by the UI, accessible via `GameMain.data.statistics`. This is significantly more efficient than manually iterating through game objects.

**ProductionStatistics Pattern:**
```csharp
var stats = GameMain.data.statistics.production;

// Refresh for specific planet (or 0 for all planets)
stats.RefreshPowerGenerationCapacites(planetId);
stats.RefreshPowerConsumptionDemands(planetId);

// Access precomputed values (already in Watts!)
long generationCapacityW = stats.totalGenCapacity;
long consumptionDemandW = stats.totalConDemand;
```

**Benefits:**
1. **Matches UI Exactly** - Uses same data source as game UI, ensuring consistency
2. **More Efficient** - Precomputed aggregates instead of manual iteration
3. **Already Converted** - Values in display units (Watts), not internal per-tick values
4. **Per-Planet Filtering** - `RefreshPowerGenerationCapacites(planetId)` filters to specific planet

**Implementation:**
- PowerGridHandler.cs now uses ProductionStatistics for generation/consumption totals
- Still manually aggregates some network-level values (energyServed, energyStored) not in ProductionStatistics
- Response format simplified by removing internal `_raw_*` fields

**Question for Future Work:**
The `GameMain.data.statistics` object appears to have multiple subsystems:
- `statistics.production` (used for power grid)
- `statistics.kill` (possibly combat stats?)
- `statistics.traffic` (possibly logistics/transport stats?)
- `statistics.charts` (possibly historical time-series data?)

**TODO:** Investigate if similar precomputed statistics exist for:
- Production rates (item/min across factories) - Could eliminate manual factory iteration in production tools
- Logistics throughput (ILS/PLS item flow rates) - Could provide transport statistics
- Belt utilization - Might have precomputed belt saturation data
- Building efficiency - May track working/idle assembler counts

If these exist, we should refactor existing tools to use them instead of manual calculations. This pattern discovery could significantly improve performance and accuracy across all tools.

---

## Phase 3: Proposed MCP Tools

### Bottleneck Analysis Tools

1. **get_production_summary** - Get production/consumption rates per item across all factories
   - [x] Implemented as `get_production_stats`
   - Returns: item ID, production rate/min, consumption rate/min, net rate
   - Use: Identify items with negative net rates (bottlenecks)

2. **get_logistics_stations** - List all Planetary/Interstellar Logistics Stations
   - [x] Implemented as `list_ils_per_planet` and `get_ils_details`
   - Returns: station ID, planet, position, item slots (item, mode, current/max stock, demand/supply rates)
   - Use: Find stations with unfulfilled demand or idle supply

3. **get_power_grid_status** - Get power network status per planet
   - [x] Implemented
   - Returns: planet, total generation, total consumption, satisfaction %, storage level
   - Use: Identify planets with power deficits causing production slowdowns

4. **get_assembler_efficiency** - Get working/idle status of production buildings
   - [x] Implemented as `get_assembler_details`
   - Returns: planet, building type, total count, working count, idle count, idle %
   - Use: Find machines starved for input or blocked on output

5. **get_belt_throughput** - Get belt utilization at key junctions
   - Returns: belt segments with current items/min vs max capacity
   - Use: Identify saturated belts limiting throughput

6. **get_storage_levels** - Get fill levels of all storage containers
   - Returns: planet, storage type, item, current/max, fill %
   - Use: Find full storages (output blocked) or empty storages (input starved)

### Distance & Factory Placement Tools

7. **get_star_systems** - List all star systems with positions
   - [x] Implemented as `get_stars`
   - Returns: star ID, name, type, position (x,y,z), luminosity, planets count
   - Use: Calculate inter-system distances for logistics planning

8. **get_planets** - List all planets with details
   - [x] Implemented as `list_planets`
   - Returns: planet ID, name, star, type, position, radius. Resource details are now available via `get_planet_resources`.
   - Use: Find planets with specific resources

9. **get_planet_resources** - Get vein/resource deposits on a specific planet
    - [x] Implemented
    - Returns: resource type, vein count, total amount, positions
    - Use: Evaluate resource richness for factory placement

10. **get_logistics_network_graph** - Get logistics vessel routes and distances
    - [x] Partially implemented via `get_planet_routes` and `find_item_transport`
    - Returns: station pairs, distance (AU/LY), travel time, vessel count, throughput
    - Use: Identify long routes that could benefit from relay stations

11. **get_distance_matrix** - Calculate distances between specified planets/stations
    - Returns: matrix of distances (AU for intra-system, LY for inter-system)
    - Use: Optimize factory placement to minimize transport distances

12. **get_resource_distribution** - Summarize where resources are concentrated
    - Returns: resource type, planets with deposits, total amounts, average distance from home
    - Use: Plan which resources to import vs produce locally

13. **get_dyson_sphere_status** - Get Dyson swarm/shell construction status
    - Returns: star, swarm count, shell nodes/frames, power output, sail consumption rate
    - Use: Track megastructure progress and sail production needs

### Utility Tools

14. **get_player_location** - Get current player/mecha position
    - [x] Included in `get_game_info`
    - Returns: current planet, position, current star system
    - Use: Context for relative distance calculations

15. **get_game_tick** - Get current game time/tick
    - Returns: game tick, UPS, play time
    - Use: Timestamp data for rate calculations over time

---

## Phase 4: Web Frontend

### Overview
Add a web frontend to display game data visually. The frontend connects to a REST API exposed by the Python server.

### Architecture
```
┌─────────────────┐     HTTP :8000    ┌─────────────────┐   WebSocket   ┌─────────────────┐
│  SvelteKit      │◄─────────────────►│  Python Server  │◄─────────────►│  C# Game Mod    │
│  Frontend       │   REST API        │  (FastAPI +     │  :18181       │  (DSPMCP.dll)   │
│  /frontend      │                   │   FastMCP)      │               │                 │
└─────────────────┘                   └─────────────────┘               └─────────────────┘
```

### Technology Choices
| Component | Choice |
|-----------|--------|
| Frontend Framework | SvelteKit + TypeScript |
| Frontend Runtime | Bun |
| REST Server | FastAPI (port 8000) |
| Python Package Manager | uv |
| Charts | Chart.js / svelte-chartjs |
| Starmap (future) | D3.js 2D |
| Update Mode | Manual refresh |

### MVP Scope: Research Dashboard
- Total research progress (current tech, hash rate, progress %)
- Per-planet breakdown table (labs, working/idle, hash/sec)
- Lab details on planet click

### Phase 4.1: Python REST API

**Files to modify:**
- `mcp-server/server.py` - Add FastAPI app
- `mcp-server/pyproject.toml` - Add fastapi, uvicorn (via `uv add`)

**REST Endpoints:**
```
GET /api/health                    → {"status": "ok", "game_connected": bool}
GET /api/research/progress         → Research progress data
GET /api/research/planets          → Per-planet research breakdown
GET /api/research/labs/{planet_id} → Lab details for specific planet
```

**Implementation:**
1. Add FastAPI + uvicorn deps: `uv add fastapi uvicorn[standard]`
2. Create shared `call_game(method, params)` async function
3. Add FastAPI routes that call game via WebSocket
4. Add CORS middleware for localhost:5173
5. Run FastAPI on port 8000

### Phase 4.2: SvelteKit Project Setup

**New directory:** `frontend/`

**Steps:**
1. Initialize SvelteKit: `bun create svelte@latest frontend`
2. Install deps: `bun add chart.js svelte-chartjs d3`
3. Configure VITE_API_URL=http://localhost:8000

**Project structure:**
```
frontend/
├── src/
│   ├── lib/
│   │   ├── api/client.ts          # API client
│   │   ├── components/            # Svelte components
│   │   └── types/index.ts         # TypeScript interfaces
│   ├── routes/
│   │   ├── +layout.svelte
│   │   ├── +page.svelte           # Dashboard
│   │   └── research/+page.svelte  # Research view
│   └── app.css
└── package.json
```

### Phase 4.3: Frontend Components

**Components to build:**
- `ResearchProgress.svelte` - Current tech, progress bar, hash rate
- `PlanetTable.svelte` - Sortable table of planets with lab stats
- `LabDetails.svelte` - Modal showing individual lab info
- `RefreshButton.svelte` - Manual refresh with loading state

**Styling:** Dark theme matching DSP aesthetic
- Background: #0a0a1a
- Accents: Cyan (#00d4ff), Orange (#ff8800)

### Phase 4.4: Integration Testing

**Verification checklist:**
- [ ] `curl http://localhost:8000/api/health` returns ok
- [ ] `curl http://localhost:8000/api/research/progress` returns data
- [ ] Frontend loads at http://localhost:5173
- [ ] Research dashboard displays correctly
- [ ] Planet table is sortable
- [ ] Lab details modal works
- [ ] MCP tools still work via CLI

---

## Future Phases

### Phase 5: ILS Logistics View
- REST endpoint: `GET /api/logistics/stations`
- Station list with item storage progress bars
- Drone/ship utilization display

### Phase 6: Interactive Starmap
- REST endpoints: `GET /api/stars`, `GET /api/planets`
- D3.js 2D force-directed starmap
- Click stars to see details
- Logistics routes as connecting lines

### Phase 7: Production Statistics
- New C# `ProductionHandler.cs`
- Production table with sparkline graphs

### Phase 8: Power Grid View
- New C# handler for power data
- Circular gauge for satisfaction %

---

## Implementation Order

**IMPORTANT:** Complete in this order:
1. ~~Phase 1: Core Connectivity~~ (DONE)
2. ~~Phase 2: Game Data Integration~~ (DONE)
3. **Phase 4: Web Frontend** (NEXT)
   - 4.1: Python REST API
   - 4.2: SvelteKit setup
   - 4.3: Research dashboard components
   - 4.4: Integration testing
4. Phase 5+: Additional views

---

## Commands Reference

```bash
# Python REST server (using uv)
cd mcp-server
uv sync                                    # Install dependencies from pyproject.toml
uv run uvicorn server:api_app --host 0.0.0.0 --port 8000 --reload

# Frontend dev server (using bun)
cd frontend
bun install
bun run dev

# Build frontend
bun run build
```

---

## Original Architecture (MCP Only)

```
┌─────────────────┐     stdio      ┌─────────────────┐   WebSocket   ┌─────────────────┐
│  Claude Code    │◄──────────────►│  Python MCP     │◄─────────────►│  C# Game Mod    │
│  (MCP Client)   │                │  (server.py)    │  :18181       │  (DSPMCP.dll)   │
└─────────────────┘                └─────────────────┘               └─────────────────┘
                                                                              │
                                                                              ▼
                                                                     ┌─────────────────┐
                                                                     │  Dyson Sphere   │
                                                                     │  Program        │
                                                                     └─────────────────┘
```