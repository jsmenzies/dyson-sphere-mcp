#!/usr/bin/env python3
"""
Download high-quality (128x128) material icons from dyson-calculator.com
and replace the existing low-quality icons.
"""

import json
import os
import re
import time
from pathlib import Path
from typing import Dict, Optional, List
import urllib.request

# Paths
SCRIPT_DIR = Path(__file__).parent
REPO_ROOT = SCRIPT_DIR.parent.parent  # api/tools -> api -> repo root
ICONS_DIR = REPO_ROOT / "web" / "static" / "icons" / "materials"
ITEM_IDS_FILE = SCRIPT_DIR / "item-ids.json"
OUTPUT_FILE = REPO_ROOT / "web" / "src" / "lib" / "data" / "material-icons.json"

# High-quality icon data from dyson-calculator.com (128x128px)
HQ_ICONS = {
    # Items
    "Iron Ore": "https://dyson-calculator.com/img/gameUI/iron-ore.png",
    "Copper Ore": "https://dyson-calculator.com/img/gameUI/copper-ore.png",
    "Stone Ore": "https://dyson-calculator.com/img/gameUI/stone-ore.png",
    "Coal Ore": "https://dyson-calculator.com/img/gameUI/coal-ore.png",
    "Silicon Ore": "https://dyson-calculator.com/img/gameUI/silicium-ore.png",
    "Organic Crystal": "https://dyson-calculator.com/img/gameUI/crystal-rubber.png",
    "Spiniform Stalagmite Crystal": "https://dyson-calculator.com/img/gameUI/bamboo-crystal.png",
    "Kimberlite Ore": "https://dyson-calculator.com/img/gameUI/diamond-ore.png",
    "Fractal Silicon": "https://dyson-calculator.com/img/gameUI/fractal-silica.png",
    "Fire Ice": "https://dyson-calculator.com/img/gameUI/gas-hydrate.png",
    "Optical Grating Crystal": "https://dyson-calculator.com/img/gameUI/grating-ore.png",
    "Unipolar Magnet": "https://dyson-calculator.com/img/gameUI/mono-mag-ore.png",
    "Titanium Ore": "https://dyson-calculator.com/img/gameUI/titanium-ore.png",
    "Crude Oil": "https://dyson-calculator.com/img/gameUI/oil.png",
    "Refined Oil": "https://dyson-calculator.com/img/gameUI/refined-oil.png",
    "Water": "https://dyson-calculator.com/img/gameUI/water.png",
    "Sulfuric Acid": "https://dyson-calculator.com/img/gameUI/sulphuric-acid.png",
    "Iron Ingot": "https://dyson-calculator.com/img/gameUI/iron-plate.png",
    "Magnet": "https://dyson-calculator.com/img/gameUI/magnet.png",
    "Magnetic Coil": "https://dyson-calculator.com/img/gameUI/magnetism-wire.png",
    "Copper Ingot": "https://dyson-calculator.com/img/gameUI/copper-plate.png",
    "Stone Brick": "https://dyson-calculator.com/img/gameUI/stone-brick.png",
    "Glass": "https://dyson-calculator.com/img/gameUI/glass.png",
    "Prism": "https://dyson-calculator.com/img/gameUI/prism.png",
    "Diamond": "https://dyson-calculator.com/img/gameUI/diamond.png",
    "High-Purity Silicon": "https://dyson-calculator.com/img/gameUI/silicium-single-crystal.png",
    "Crystal Silicon": "https://dyson-calculator.com/img/gameUI/silicium-high-purity.png",
    "Steel": "https://dyson-calculator.com/img/gameUI/steel-plate.png",
    "Plastic": "https://dyson-calculator.com/img/gameUI/plastic.png",
    "Graphene": "https://dyson-calculator.com/img/gameUI/graphene.png",
    "Carbon Nanotube": "https://dyson-calculator.com/img/gameUI/nanotube.png",
    "Titanium Ingot": "https://dyson-calculator.com/img/gameUI/titanium-plate.png",
    "Titanium Alloy": "https://dyson-calculator.com/img/gameUI/titanium-alloy.png",
    "Titanium Crystal": "https://dyson-calculator.com/img/gameUI/titan-crystal.png",
    "Titanium Glass": "https://dyson-calculator.com/img/gameUI/titan-glass.png",
    "Hydrogen": "https://dyson-calculator.com/img/gameUI/hydrogen.png",
    "Deuterium": "https://dyson-calculator.com/img/gameUI/deuterium.png",
    "Critical Photon": "https://dyson-calculator.com/img/gameUI/photon-capacitor-full.png",
    "Antimatter": "https://dyson-calculator.com/img/gameUI/anti-matter.png",
    "Strange Matter": "https://dyson-calculator.com/img/gameUI/strange-matter-generator.png",
    "Frame Material": "https://dyson-calculator.com/img/gameUI/frame-material.png",
    "Casimir Crystal": "https://dyson-calculator.com/img/gameUI/casimir-crystal.png",
    "Gear": "https://dyson-calculator.com/img/gameUI/gear-wheel.png",
    "Circuit Board": "https://dyson-calculator.com/img/gameUI/circuit-board.png",
    "Microcrystalline Component": "https://dyson-calculator.com/img/gameUI/micro-component.png",
    "Plasma Exciter": "https://dyson-calculator.com/img/gameUI/plasma-generator.png",
    "Photon Combiner": "https://dyson-calculator.com/img/gameUI/photo-shifter.png",
    "Electric Motor": "https://dyson-calculator.com/img/gameUI/electric-motor.png",
    "Electromagnetic Turbine": "https://dyson-calculator.com/img/gameUI/mag-turbine.png",
    "Processor": "https://dyson-calculator.com/img/gameUI/processor.png",
    "Plane Filter": "https://dyson-calculator.com/img/gameUI/plane-filter.png",
    "Particle Container": "https://dyson-calculator.com/img/gameUI/partical-capacitor.png",
    "Super-Magnetic Ring": "https://dyson-calculator.com/img/gameUI/hyper-magnetism-ring.png",
    "Graviton Lens": "https://dyson-calculator.com/img/gameUI/gravity-lens.png",
    "Particle Broadband": "https://dyson-calculator.com/img/gameUI/particle-wide-band.png",
    "Quantum Chip": "https://dyson-calculator.com/img/gameUI/quantum-processor.png",
    "Annihilation Constraint Sphere": "https://dyson-calculator.com/img/gameUI/fusion-capacitor.png",
    "Thruster": "https://dyson-calculator.com/img/gameUI/fuel-thruster.png",
    "Reinforced Thruster": "https://dyson-calculator.com/img/gameUI/ion-thruster.png",
    "Space Warper": "https://dyson-calculator.com/img/gameUI/space-warper.png",
    "Plant Fuel": "https://dyson-calculator.com/img/gameUI/plant-fuel.png",
    "Log": "https://dyson-calculator.com/img/gameUI/wood.png",
    "Energetic Graphite": "https://dyson-calculator.com/img/gameUI/graphite.png",
    "Hydrogen Fuel Rod": "https://dyson-calculator.com/img/gameUI/hydrogen-energy-fuel.png",
    "Deuteron Fuel Rod": "https://dyson-calculator.com/img/gameUI/deuterium-energy-fuel.png",
    "Antimatter Fuel Rod": "https://dyson-calculator.com/img/gameUI/antimatter-energy-fuel.png",
    "Electromagnetic Matrix": "https://dyson-calculator.com/img/gameUI/t-matrix.png",
    "Energy Matrix": "https://dyson-calculator.com/img/gameUI/e-matrix.png",
    "Structure Matrix": "https://dyson-calculator.com/img/gameUI/c-matrix.png",
    "Information Matrix": "https://dyson-calculator.com/img/gameUI/i-matrix.png",
    "Gravity Matrix": "https://dyson-calculator.com/img/gameUI/g-matrix.png",
    "Universe Matrix": "https://dyson-calculator.com/img/gameUI/u-matrix.png",
    "Solar Sail": "https://dyson-calculator.com/img/gameUI/solar-collector.png",
    "Dyson Sphere Component": "https://dyson-calculator.com/img/gameUI/dyson-sphere-component.png",
    # Buildings
    "Mining Machine": "https://dyson-calculator.com/img/gameUI/mining-drill.png",
    "Oil Extractor": "https://dyson-calculator.com/img/gameUI/oil-extractor.png",
    "Water Pump": "https://dyson-calculator.com/img/gameUI/water-pump.png",
    "Orbital Collector": "https://dyson-calculator.com/img/gameUI/orbital-collector.png",
    "Smelter": "https://dyson-calculator.com/img/gameUI/smelter.png",
    "Plane Smelter": "https://dyson-calculator.com/img/gameUI/plane-smelter.png",
    "Assembling Machine Mk.I": "https://dyson-calculator.com/img/gameUI/assembler-1.png",
    "Assembling Machine Mk.II": "https://dyson-calculator.com/img/gameUI/assembler-2.png",
    "Assembling Machine Mk.III": "https://dyson-calculator.com/img/gameUI/assembler-3.png",
    "Chemical Plant": "https://dyson-calculator.com/img/gameUI/chemical-plant.png",
    "Fractionator": "https://dyson-calculator.com/img/gameUI/fractionator.png",
    "Miniature Particle Collider": "https://dyson-calculator.com/img/gameUI/hadron-collider.png",
    "Oil Refinery": "https://dyson-calculator.com/img/gameUI/oil-refinery.png",
    "Matrix Lab": "https://dyson-calculator.com/img/gameUI/lab.png",
    "Wind Turbine": "https://dyson-calculator.com/img/gameUI/wind-turbine.png",
    "Solar Panel": "https://dyson-calculator.com/img/gameUI/solar-panel.png",
    "Thermal Power Plant": "https://dyson-calculator.com/img/gameUI/fuel-plant.png",
    "Ray Receiver": "https://dyson-calculator.com/img/gameUI/ray-receiver.png",
    "Mini Fusion Power Plant": "https://dyson-calculator.com/img/gameUI/fusion-power-station.png",
    "Artificial star": "https://dyson-calculator.com/img/gameUI/fusion-reactor.png",
    "Storage Mk.I": "https://dyson-calculator.com/img/gameUI/storage-1.png",
    "Storage Mk.II": "https://dyson-calculator.com/img/gameUI/storage-2.png",
    "Storage tank": "https://dyson-calculator.com/img/gameUI/storage-tank.png",
    "Accumulator": "https://dyson-calculator.com/img/gameUI/accumulator.png",
    "Accumulator (Full)": "https://dyson-calculator.com/img/gameUI/accumulator-full.png",
    "EM-Rail Ejector": "https://dyson-calculator.com/img/gameUI/em-rail-ejector.png",
    "Vertical Launching Silo": "https://dyson-calculator.com/img/gameUI/vertical-launching-silo.png",
    "Small Carrier Rocket": "https://dyson-calculator.com/img/gameUI/rocket.png",
    # Structures
    "Conveyor Belt Mk.I": "https://dyson-calculator.com/img/gameUI/belt-1.png",
    "Conveyor Belt Mk.II": "https://dyson-calculator.com/img/gameUI/belt-2.png",
    "Conveyor Belt Mk.III": "https://dyson-calculator.com/img/gameUI/belt-3.png",
    "Splitter": "https://dyson-calculator.com/img/gameUI/splitter-4dir.png",
    "Traffic Monitor": "https://dyson-calculator.com/img/gameUI/traffic-monitor.png",
    "Sorter Mk.I": "https://dyson-calculator.com/img/gameUI/inserter-1.png",
    "Sorter Mk.II": "https://dyson-calculator.com/img/gameUI/inserter-2.png",
    "Sorter Mk.III": "https://dyson-calculator.com/img/gameUI/inserter-3.png",
    "Planetary Logistics Station": "https://dyson-calculator.com/img/gameUI/logistic-station.png",
    "Logistics Drone": "https://dyson-calculator.com/img/gameUI/logistic-drone.png",
    "Interstellar Logistics Station": "https://dyson-calculator.com/img/gameUI/interstellar-logistic-station.png",
    "Logistics Vessel": "https://dyson-calculator.com/img/gameUI/logistic-vessel.png",
    "Tesla Tower": "https://dyson-calculator.com/img/gameUI/tesla-coil.png",
    "Wireless Power Tower": "https://dyson-calculator.com/img/gameUI/charging-pole.png",
    "Energy Exchanger": "https://dyson-calculator.com/img/gameUI/energy-exchanger.png",
    "Satellite Substation": "https://dyson-calculator.com/img/gameUI/orbital-substation.png",
    "Foundation": "https://dyson-calculator.com/img/gameUI/terrain-tool.png",
}

def normalize_name(name: str) -> str:
    """Normalize item names for matching."""
    return name.strip().lower()

def to_snake_case(name: str) -> str:
    """Convert item name to snake_case for filenames."""
    # Remove "Icon " prefix if present
    name = re.sub(r'^icon\s+', '', name, flags=re.IGNORECASE)
    # Remove file extension if present
    name = re.sub(r'\.png$', '', name, flags=re.IGNORECASE)
    # Convert to snake_case
    name = name.replace('-', ' ').replace('_', ' ')
    name = re.sub(r'[^a-zA-Z0-9\s]', '', name)
    name = re.sub(r'\s+', '_', name.strip())
    return name.lower()

def load_item_ids() -> Dict[str, int]:
    """Load item IDs from JSON file. Returns {item_name: id}."""
    if not ITEM_IDS_FILE.exists():
        return {}

    with open(ITEM_IDS_FILE) as f:
        data = json.load(f)

    # Reverse mapping: id -> name becomes name -> id
    name_to_id = {}
    for item_id, name in data.items():
        name_to_id[name] = int(item_id)

    return name_to_id

def download_icon(url: str, filepath: Path) -> bool:
    """Download icon from URL to filepath."""
    try:
        with urllib.request.urlopen(url, timeout=10) as response:
            data = response.read()

        with open(filepath, 'wb') as f:
            f.write(data)

        return True
    except Exception as e:
        print(f"  ✗ Failed to download {url}: {e}")
        return False

def download_all_icons() -> Dict[str, str]:
    """Download all high-quality icons. Returns mapping of item_name -> filepath."""
    ICONS_DIR.mkdir(parents=True, exist_ok=True)

    print(f"\nDownloading {len(HQ_ICONS)} high-quality icons (128x128) to {ICONS_DIR}")

    success = 0
    failed = 0
    downloaded = {}

    for item_name, url in sorted(HQ_ICONS.items()):
        filename = to_snake_case(item_name) + ".png"
        filepath = ICONS_DIR / filename

        print(f"  ⬇ {filename}...", end='', flush=True)

        if download_icon(url, filepath):
            print(" ✓")
            success += 1
            downloaded[item_name] = filename
            # Rate limiting
            time.sleep(0.1)
        else:
            print(" ✗")
            failed += 1

    print(f"\n✓ Downloaded {success} icons")
    if failed > 0:
        print(f"✗ Failed to download {failed} icons")

    return downloaded

def generate_material_icons_json(downloaded: Dict[str, str], name_to_id: Dict[str, int]) -> None:
    """Generate the final material-icons.json file."""
    output = {}

    for item_name, filename in sorted(downloaded.items()):
        # Try to find item ID
        item_id = name_to_id.get(item_name)

        # Construct wiki URL
        wiki_url_name = item_name.replace(' ', '_')
        wiki_url = f"https://dsp-wiki.com/{wiki_url_name}"

        entry = {
            "name": item_name,
            "icon": f"/icons/materials/{filename}",
            "wikiUrl": wiki_url
        }

        if item_id is not None:
            entry["itemId"] = item_id

        output[item_name] = entry

    # Ensure output directory exists
    OUTPUT_FILE.parent.mkdir(parents=True, exist_ok=True)

    with open(OUTPUT_FILE, 'w') as f:
        json.dump(output, f, indent=2, sort_keys=True)

    print(f"\n✓ Generated {OUTPUT_FILE}")
    print(f"  Total items: {len(output)}")
    print(f"  Items with IDs: {sum(1 for v in output.values() if 'itemId' in v)}")
    print(f"  Items without IDs: {sum(1 for v in output.values() if 'itemId' not in v)}")

def main():
    print("=" * 60)
    print("Dyson Sphere Program - High-Quality Icon Downloader")
    print("Source: dyson-calculator.com (128x128px)")
    print("=" * 60)

    print("\n1. Loading item IDs...")
    name_to_id = load_item_ids()
    print(f"   Loaded {len(name_to_id)} items with IDs from {ITEM_IDS_FILE.name}")

    print("\n2. Downloading high-quality icons...")
    downloaded = download_all_icons()

    print("\n3. Generating material-icons.json...")
    generate_material_icons_json(downloaded, name_to_id)

    print("\n" + "=" * 60)
    print("✓ Complete!")
    print("=" * 60)

if __name__ == "__main__":
    main()
