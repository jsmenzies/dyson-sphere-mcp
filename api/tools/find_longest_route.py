import urllib.request
import json
import concurrent.futures

API_URL = "http://localhost:8000/api"

def get_json(url):
    try:
        with urllib.request.urlopen(url) as response:
            if response.status == 200:
                return json.loads(response.read().decode())
    except Exception as e:
        # Ignore errors for planets without routes or connection issues
        pass
    return None

def get_planet_routes(planet):
    planet_id = planet['id']
    url = f"{API_URL}/planets/{planet_id}/routes"
    data = get_json(url)
    return data

def main():
    print("Fetching planet list...")
    planets = get_json(f"{API_URL}/planets")
    
    if not planets:
        print("Failed to get planet list.")
        return

    print(f"Scanning {len(planets)} planets for shipping routes...")
    
    longest_route = {
        "distance": -1,
        "details": None
    }

    # Use ThreadPoolExecutor for faster concurrent fetching
    with concurrent.futures.ThreadPoolExecutor(max_workers=20) as executor:
        future_to_planet = {executor.submit(get_planet_routes, planet): planet for planet in planets}
        
        for future in concurrent.futures.as_completed(future_to_planet):
            data = future.result()
            if not data:
                continue

            # Check Outgoing routes (we only need to check outgoing to avoid double counting, 
            # as every incoming is someone else's outgoing)
            if 'outgoing' in data:
                for ship in data['outgoing']:
                    # Some ships might be in 'Departure' stage with 0 distance calculated yet, or local logistics
                    dist = ship.get('distance', 0)
                    if dist > longest_route['distance']:
                        longest_route['distance'] = dist
                        longest_route['details'] = {
                            "origin": data.get('planetName', 'Unknown'),
                            "dest": ship.get('destPlanetName', 'Unknown'),
                            "item": ship.get('itemName', 'Unknown'),
                            "count": ship.get('itemCount', 0),
                            "distance": dist
                        }

    if longest_route['details']:
        d = longest_route['details']
        # Distance in DSP is usually in meters. Convert to Light Years (LY) or AU if large?
        # 1 LY approx 60*60*60 ticks * ... game units. 
        # Actually, let's just print the raw value and maybe assume meters (40000m = 1 planet width approx?)
        # Game uses Light Years for stars. 1 LY = 2,400,000 meters? No, usually 60 AU. 
        # Let's just print the raw number.
        print("\n--- Longest Active Shipping Route ---")
        print(f"Origin:     {d['origin']}")
        print(f"Destination: {d['dest']}")
        print(f"Cargo:      {d['item']} (x{d['count']})")
        print(f"Distance:   {d['distance']:,.2f}")
    else:
        print("\nNo active routes found with distance data.")

if __name__ == "__main__":
    main()
