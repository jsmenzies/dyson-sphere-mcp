import os
import json
import requests
import shutil
from pathlib import Path
from datetime import datetime

# Configuration
API_BASE = "http://localhost:8000/api"
PROJECT_ROOT = Path(__file__).parent.parent.parent
MOCK_BASE = PROJECT_ROOT / "api/mock"
TOOLS_DIR = PROJECT_ROOT / "api/tools"
PROGRESS_FILE = MOCK_BASE / "MOCK_PROGRESS.md"

# Ensure directories exist
MOCK_BASE.mkdir(parents=True, exist_ok=True)
(MOCK_BASE / "planets").mkdir(exist_ok=True)
(MOCK_BASE / "stations").mkdir(exist_ok=True)
(MOCK_BASE / "items").mkdir(exist_ok=True)

def load_item_ids():
    """Load item IDs from JSON file."""
    try:
        with open(TOOLS_DIR / "item-ids.json", "r", encoding="utf-8") as f:
            data = json.load(f)
            return data
    except Exception as e:
        print(f"Error loading item-ids.json: {e}")
        return {}

def save_json(path, data):
    """Save data to JSON file."""
    try:
        with open(path, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2)
        return True
    except Exception as e:
        print(f"Error saving {path}: {e}")
        return False

def log_progress(message, category="INFO"):
    """Append log message to progress file."""
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    log_line = f"| {timestamp} | {category} | {message} |"
    
    # Check if we need to initialize the log table
    needs_header = False
    if not PROGRESS_FILE.exists():
        needs_header = True
    else:
        with open(PROGRESS_FILE, 'r') as f:
            content = f.read()
            if "| Timestamp | Category | Message |" not in content:
                # Add a new section for logs if it doesn't exist (or just append if file exists but no table)
                # But to keep it clean, let's just append to end
                pass

    with open(PROGRESS_FILE, 'a', encoding='utf-8') as f:
        # If the file ends with the table header or we just want to append
        # Let's just append a simple line for now or formatted table row
        f.write(f"\n{log_line}")

def fetch_and_save(url_suffix, relative_path, description):
    """Fetch data from API and save to file."""
    url = f"{API_BASE}{url_suffix}"
    try:
        print(f"Fetching {description} ({url})...")
        resp = requests.get(url)
        resp.raise_for_status()
        data = resp.json()
        
        full_path = MOCK_BASE / relative_path
        full_path.parent.mkdir(parents=True, exist_ok=True)
        
        if save_json(full_path, data):
            log_progress(f"Success: {description} -> {relative_path}", "SUCCESS")
            return data
        else:
            log_progress(f"Failed to save: {description}", "ERROR")
            return None
    except Exception as e:
        error_msg = str(e)
        print(f"Failed: {description} - {error_msg}")
        log_progress(f"Failed: {description} ({error_msg})", "ERROR")
        return None

def main():
    print("Starting mock data generation...")
    
    # Initialize log section in MOCK_PROGRESS.md if needed
    with open(PROGRESS_FILE, 'a') as f:
        f.write("\n\n## Automated Collection Log\n| Timestamp | Category | Message |\n|---|---|---|")

    # 1. Global Tools
    global_tools = {
        "/info": "get_game_info.json",
        "/research": "get_research_progress.json",
        "/planets": "list_planets.json",
        "/stars": "get_stars.json",
        "/research/by-planet": "get_research_by_planet.json",
        "/research/tech-queue": "get_tech_queue.json",
        "/research/upgrades": "get_upgrades.json",
        "/ils": "list_ils_per_planet.json",
        "/galaxy": "get_galaxy_details.json",
        "/power": "get_power_grid_status.json",
        "/production?planet_id=-1&time_level=0": "get_production_stats.json"
    }

    print("\n--- Processing Global Tools ---")
    for suffix, filename in global_tools.items():
        fetch_and_save(suffix, filename, f"Global: {filename}")

    # 2. Get Planet IDs
    planets_data = fetch_and_save("/planets", "list_planets.json", "Global: list_planets (Refetch)")
    planet_ids = []
    if planets_data and isinstance(planets_data, list):
        planet_ids = [p["id"] for p in planets_data]
        print(f"Found {len(planet_ids)} planets: {planet_ids}")
    else:
        print("Could not fetch planets list. Skipping planet-specific tools.")

    # 3. Planet Specific Tools
    print("\n--- Processing Planet Tools ---")
    for pid in planet_ids:
        planet_tools = {
            f"/planets/{pid}/resources": f"planets/{pid}/resources.json",
            f"/planets/{pid}/labs": f"planets/{pid}/labs.json",
            f"/planets/{pid}/ils": f"planets/{pid}/ils.json",
            f"/planets/{pid}/power": f"planets/{pid}/power.json",
            f"/production?planet_id={pid}&time_level=0": f"planets/{pid}/production.json",
            f"/planets/{pid}/assemblers": f"planets/{pid}/assemblers.json",
            f"/planets/{pid}/routes": f"planets/{pid}/routes.json"
        }
        for suffix, filename in planet_tools.items():
            fetch_and_save(suffix, filename, f"Planet {pid}: {Path(filename).name}")

    # 4. Station Routes
    print("\n--- Processing Station Routes ---")
    ils_data = fetch_and_save("/ils", "list_ils_per_planet.json", "Global: list_ils_per_planet (Refetch)")
    if ils_data and "planets" in ils_data:
        count = 0
        for planet_ils in ils_data["planets"]:
            # Check for 'ilsStations' (based on actual JSON structure) or fallback to 'stations'
            stations_list = planet_ils.get("ilsStations") or planet_ils.get("stations") or []
            
            for station in stations_list:
                # Try to find ID - prefer gid for global uniqueness if available
                sid = station.get("gid") or station.get("id") or station.get("stationId")
                if sid:
                    fetch_and_save(f"/ils/{sid}/routes", f"stations/{sid}_routes.json", f"Station {sid} Routes")
                    count += 1
        print(f"Processed {count} stations.")
    else:
        print("No station data found.")

    # 5. Items
    print("\n--- Processing Item Tools ---")
    items_map = load_item_ids()
    if items_map:
        print(f"Found {len(items_map)} items in definition file.")
        count = 0
        for iid, name in items_map.items():
            # fetch_and_save returns data or None. We only care if it works.
            # Some items might not have transport info, but the API should return empty list, not error.
            fetch_and_save(f"/transport/items/{iid}", f"items/{iid}_transport.json", f"Item {iid} ({name})")
            count += 1
            if count % 10 == 0:
                print(f"Processed {count} items...")
    else:
        print("No items found or failed to load item-ids.json")

    print("\nMock generation complete.")

if __name__ == "__main__":
    main()
