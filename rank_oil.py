import urllib.request
import json
import concurrent.futures
import time

API_URL = "http://localhost:8000/api"
CRUDE_OIL_ID = 1007
REFINED_OIL_ID = 1114

def get_json(url):
    try:
        with urllib.request.urlopen(url) as response:
            if response.status == 200:
                return json.loads(response.read().decode())
    except Exception as e:
        print(f"Error fetching {url}: {e}")
    return None

def get_planet_oil_stats(planet):
    planet_id = planet['id']
    planet_name = planet['name']
    
    url = f"{API_URL}/production?planet_id={planet_id}&time_level=0"
    data = get_json(url)
    
    crude_rate = 0.0
    refined_rate = 0.0
    
    if data and 'stats' in data:
        for item in data['stats']:
            if item['itemId'] == CRUDE_OIL_ID:
                crude_rate = item['productionRate']
            elif item['itemId'] == REFINED_OIL_ID:
                refined_rate = item['productionRate']
    
    return {
        'id': planet_id,
        'name': planet_name,
        'crude_oil': crude_rate,
        'refined_oil': refined_rate
    }

def main():
    print("Fetching planet list...")
    planets = get_json(f"{API_URL}/planets")
    
    if not planets:
        print("Failed to get planet list.")
        return

    print(f"Found {len(planets)} planets. Fetching production stats...")
    
    results = []
    
    # Use ThreadPoolExecutor to speed up requests
    with concurrent.futures.ThreadPoolExecutor(max_workers=10) as executor:
        future_to_planet = {executor.submit(get_planet_oil_stats, planet): planet for planet in planets}
        for future in concurrent.futures.as_completed(future_to_planet):
            try:
                data = future.result()
                if data['crude_oil'] > 0 or data['refined_oil'] > 0:
                    results.append(data)
            except Exception as exc:
                print(f"Planet processing generated an exception: {exc}")

    # Sort by Crude Oil production descending
    results.sort(key=lambda x: x['crude_oil'], reverse=True)
    
    print("\n--- Planet Oil Production Ranking (Per Minute) ---")
    print(f"{ 'Rank':<5} {'Planet':<25} {'Crude Oil':<15} {'Refined Oil':<15}")
    print("-" * 65)
    
    for rank, r in enumerate(results, 1):
        print(f"{rank:<5} {r['name']:<25} {r['crude_oil']:<15.2f} {r['refined_oil']:<15.2f}")

if __name__ == "__main__":
    main()
