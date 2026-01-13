import os
import json
import logging
import asyncio
import sys
import threading
from typing import Optional, List, Dict, Any

import websockets
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastmcp import FastMCP
import uvicorn

# Configure logging to stderr to avoid interfering with MCP stdio
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
    stream=sys.stderr
)
logger = logging.getLogger("DSP-API")

# Environment variables for configuration
USE_MOCK = os.getenv("DSP_USE_MOCK", "false").lower() == "true"
GAME_WS_URI = os.getenv("DSP_WS_URI", "ws://localhost:18181/")
MOCK_DIR = os.path.join(os.path.dirname(__file__), "mock")

# Create the MCP Server
mcp = FastMCP("DSP Agent")

# Create the FastAPI App
app = FastAPI(title="Dyson Sphere Program API")

# Add CORS middleware for frontend accessibility
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

async def query_game(method: str, params: dict = None) -> Any:
    """Query the game via WebSocket or load from mock data."""
    if USE_MOCK:
        return load_mock_data(method, params)
    return await query_game_ws(method, params)

def load_mock_data(method: str, params: dict = None) -> Any:
    """Load data from the mock directory."""
    filename = f"{method}.json"
    
    # Special handling for methods with planetId/starId/itemId to look for specific mocks
    if params:
        if "planetId" in params:
            specific_file = f"{method}_{params['planetId']}.json"
            if os.path.exists(os.path.join(MOCK_DIR, specific_file)):
                filename = specific_file
        elif "itemId" in params:
            specific_file = f"{method}_{params['itemId']}.json"
            if os.path.exists(os.path.join(MOCK_DIR, specific_file)):
                filename = specific_file
            
    filepath = os.path.join(MOCK_DIR, filename)
    
    try:
        if not os.path.exists(filepath):
            logger.warning(f"Mock file not found: {filepath}. Falling back to empty or generic response.")
            if "list" in method: return []
            return {"error": "Mock data not found"}
            
        with open(filepath, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception as e:
        logger.error(f"Error reading mock file {filepath}: {e}")
        return {"error": str(e)}

async def query_game_ws(method: str, params: dict = None) -> Any:
    """Helper to send a JSON-RPC request to the game via WebSocket."""
    try:
        async with websockets.connect(GAME_WS_URI) as ws:
            request = {
                "jsonrpc": "2.0",
                "method": method,
                "params": params or {},
                "id": 1
            }
            await ws.send(json.dumps(request))
            response = await ws.recv()
            data = json.loads(response)
            
            if "error" in data:
                logger.error(f"Game returned error for {method}: {data['error']}")
                return {"error": data["error"]}

            return data.get("result")
            
    except Exception as e:
        logger.error(f"WebSocket error for {method}: {e}")
        return {"error": f"Connection error: {str(e)}"}

# ===== MCP Tools (Wrapper around query_game) =====

@mcp.tool()
async def get_game_info() -> str:
    """Get basic game info: version, current planet, and star count."""
    result = await query_game("get_game_info")
    return json.dumps(result)

@mcp.tool()
async def get_research_progress() -> str:
    """Get current research tech, progress percentage, and total hash rate."""
    result = await query_game("get_research_progress")
    return json.dumps(result)

@mcp.tool()
async def list_planets() -> str:
    """List all planets with basic details."""
    result = await query_game("list_planets")
    return json.dumps(result)

@mcp.tool()
async def get_planet_resources(planetId: int) -> str:
    """Get resources for a specific planet."""
    result = await query_game("get_planet_resources", {"planetId": planetId})
    return json.dumps(result)

@mcp.tool()
async def get_stars() -> str:
    """List all stars with details."""
    result = await query_game("get_stars")
    return json.dumps(result)

@mcp.tool()
async def get_research_by_planet() -> str:
    """Get research hash rate breakdown by planet, showing lab counts, working/idle labs, and hash/sec per planet."""
    result = await query_game("get_research_by_planet")
    return json.dumps(result)

@mcp.tool()
async def get_tech_queue() -> str:
    """Get the current research queue with progress on each queued technology."""
    result = await query_game("get_tech_queue")
    return json.dumps(result)

@mcp.tool()
async def get_upgrades() -> str:
    """Get mecha upgrades, research speed multipliers, logistics capacities, and Dyson sphere unlocks."""
    result = await query_game("get_upgrades")
    return json.dumps(result)

@mcp.tool()
async def get_lab_details(planetId: int) -> str:
    """Get details for all labs on a specific planet, including individual research speed and the globally researched tech."""
    result = await query_game("get_lab_details", {"planetId": planetId})
    return json.dumps(result)

@mcp.tool()
async def list_ils_per_planet() -> str:
    """List all Interstellar Logistics Stations (ILS) grouped by planet, including drone/ship counts, warper status, energy levels, and item storage with logistics modes (supply/demand)."""
    result = await query_game("list_ils_per_planet")
    return json.dumps(result)

@mcp.tool()
async def get_ils_details(planetId: int) -> str:
    """Get detailed information for all ILS stations on a specific planet."""
    result = await query_game("get_ils_details", {"planetId": planetId})
    return json.dumps(result)

@mcp.tool()
async def get_shipping_routes_for_ils(stationId: int) -> str:
    """Get shipping routes for a given ILS (by stationId/gid), showing outgoing and incoming ships."""
    result = await query_game("get_shipping_routes_for_ils", {"stationId": stationId})
    return json.dumps(result)

@mcp.tool()
async def get_galaxy_details() -> str:
    """Get galaxy-wide details and statistics."""
    result = await query_game("get_galaxy_details")
    return json.dumps(result)

@mcp.tool()
async def get_power_grid_status() -> str:
    """Get power grid status across all planets, including energy generation, consumption, and satisfaction."""
    result = await query_game("get_power_grid_status")
    return json.dumps(result)

@mcp.tool()
async def get_power_grids_by_planet(planetId: int) -> str:
    """Get detailed power network information for a specific planet, including individual network stats with generators, consumers, and accumulators."""
    result = await query_game("get_power_grids_by_planet", {"planetId": planetId})
    return json.dumps(result)

@mcp.tool()
async def get_production_stats(planetId: int = -1, timeLevel: int = 0) -> str:
    """Get production and consumption rates for a planet (-1 for global). timeLevel: 0=1m, 1=10m, 2=1h, 3=10h, 4=100h, 5=Total."""
    result = await query_game("get_production_stats", {"planetId": planetId, "timeLevel": timeLevel})
    return json.dumps(result)

@mcp.tool()
async def get_assembler_details(planetId: int) -> str:
    """Get detailed status of all assemblers on a specific planet, including proliferation and actual speed."""
    result = await query_game("get_assembler_details", {"planetId": planetId})
    return json.dumps(result)

@mcp.tool()
async def get_planet_routes(planetId: int) -> str:
    """Get all incoming and outgoing shipping routes for a specific planet."""
    result = await query_game("get_planet_routes", {"planetId": planetId})
    return json.dumps(result)

@mcp.tool()
async def find_item_transport(itemId: int) -> str:
    """Find all ships currently transporting a specific item (by ID)."""
    result = await query_game("find_item_transport", {"itemId": itemId})
    return json.dumps(result)

# ===== FastAPI Endpoints =====

@app.get("/api/info")
async def api_get_info():
    return await query_game("get_game_info")

@app.get("/api/research")
async def api_get_research():
    return await query_game("get_research_progress")

@app.get("/api/planets")
async def api_list_planets():
    return await query_game("list_planets")

@app.get("/api/planets/{planet_id}/resources")
async def api_get_planet_resources(planet_id: int):
    return await query_game("get_planet_resources", {"planetId": planet_id})

@app.get("/api/stars")
async def api_get_stars():
    return await query_game("get_stars")

@app.get("/api/ils")
async def api_list_ils():
    return await query_game("list_ils_per_planet")

@app.get("/api/research/by-planet")
async def api_get_research_by_planet():
    return await query_game("get_research_by_planet")

@app.get("/api/research/tech-queue")
async def api_get_tech_queue():
    return await query_game("get_tech_queue")

@app.get("/api/research/upgrades")
async def api_get_upgrades():
    return await query_game("get_upgrades")

@app.get("/api/planets/{planet_id}/labs")
async def api_get_lab_details(planet_id: int):
    return await query_game("get_lab_details", {"planetId": planet_id})

@app.get("/api/planets/{planet_id}/ils")
async def api_get_ils_details(planet_id: int):
    return await query_game("get_ils_details", {"planetId": planet_id})

@app.get("/api/ils/{station_id}/routes")
async def api_get_shipping_routes(station_id: int):
    return await query_game("get_shipping_routes_for_ils", {"stationId": station_id})

@app.get("/api/galaxy")
async def api_get_galaxy_details():
    return await query_game("get_galaxy_details")

@app.get("/api/power")
async def api_get_power_grid_status():
    return await query_game("get_power_grid_status")

@app.get("/api/planets/{planet_id}/power")
async def api_get_power_grids_by_planet(planet_id: int):
    return await query_game("get_power_grids_by_planet", {"planetId": planet_id})

@app.get("/api/production")
async def api_get_production_stats(planet_id: int = -1, time_level: int = 0):
    return await query_game("get_production_stats", {"planetId": planet_id, "timeLevel": time_level})

@app.get("/api/planets/{planet_id}/assemblers")
async def api_get_assembler_details(planet_id: int):
    return await query_game("get_assembler_details", {"planetId": planet_id})

@app.get("/api/planets/{planet_id}/routes")
async def api_get_planet_routes(planet_id: int):
    return await query_game("get_planet_routes", {"planetId": planet_id})

@app.get("/api/transport/items/{item_id}")
async def api_find_item_transport(item_id: int):
    return await query_game("find_item_transport", {"itemId": item_id})

@app.get("/api/config")
async def get_config():
    return {
        "use_mock": USE_MOCK,
        "ws_uri": GAME_WS_URI,
        "mock_dir": MOCK_DIR
    }

def run_fastapi():
    logger.info(f"Starting FastAPI server on http://localhost:8000 (Mock: {USE_MOCK})...")
    uvicorn.run(app, host="0.0.0.0", port=8000, log_level="info")

def main():
    api_thread = threading.Thread(target=run_fastapi, daemon=True)
    api_thread.start()
    
    logger.info(f"Starting MCP server (Mock: {USE_MOCK})...")
    mcp.run(transport="http", host="0.0.0.0", port=8001)

if __name__ == "__main__":
    main()