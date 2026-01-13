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

## Phase 4: Web Frontend (Galaxy Map) (COMPLETE)

### Overview
Create a 2D interactive Starmap using SvelteKit, Bun, TailwindCSS, and D3.js. The map will plot stars based on their coordinates, allow navigation (pan/zoom), and display details on interaction.

### Tech Stack
- **Runtime:** Bun
- **Framework:** SvelteKit (Vite)
- **Styling:** Tailwind CSS
- **Visualization:** D3.js (SVG/Canvas)
- **API:** Local Python REST API (`http://localhost:8000`)

### 4.1: Project Scaffolding (COMPLETE)
- [x] Initialize SvelteKit project in `web/` using Bun.
- [x] Configure Tailwind CSS.
- [x] Configure `vite.config.ts` to proxy `/api` requests to `localhost:8000`.
- [x] Verify standard "Hello World" page loads.

### 4.2: Data Integration (COMPLETE)
- [x] Create TypeScript interfaces for Star/Planet data (matching API response).
- [x] Implement `ApiClient` to fetch `/api/stars`.
- [x] Verify data fetching in a simple text list.

### 4.3: Galaxy Map (D3.js Core) (COMPLETE)
- [x] Create `GalaxyMap.svelte` component.
- [x] Setup D3 SVG container with responsive sizing.
- [x] Implement X/Z coordinate projection (top-down view).
- [x] Draw basic circles for stars.

### 4.4: Visualization Enhancements (COMPLETE)
- [x] **Color Mapping:** Map spectral types (O, B, A, F, G, K, M) to star colors.
- [x] **Size Mapping:** Scale star radius based on luminosity or physical radius.
- [x] **Labels:** Add star names (optional, or visible on zoom).

### 4.5: Interactivity & Navigation (COMPLETE)
- [x] **Zoom/Pan:** Implement `d3-zoom` behavior.
- [x] **Hover:** Show tooltip with basic info (Name, Type).
- [x] **Click:** Open a "Star Details" sidebar/modal.
- [x] **Sidebar:** Show planet list and resource summary for selected star.

---

## Future Phases

### Phase 5: ILS Logistics View
- REST endpoint: `GET /api/logistics/stations`
- Station list with item storage progress bars
- Drone/ship utilization display

### Phase 6: Logistics Layer on Starmap
- Draw lines between stars representing active shipping routes.
- Filter lines by item type.

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
3. **Phase 4: Web Frontend** (CURRENT)
   - **4.1: Scaffolding**
   - 4.2: Data Integration
   - 4.3: Galaxy Map Core
   - 4.4: Visualization
   - 4.5: Interactivity
4. Phase 5+: Additional views

---

## Commands Reference

```bash
# Python REST server (using uv)
cd mcp-server
uv sync                                    # Install dependencies from pyproject.toml
uv run uvicorn server:api_app --host 0.0.0.0 --port 8000 --reload

# Frontend dev server (using bun)
cd web
bun install
bun run dev

# Build frontend
bun run build
```
