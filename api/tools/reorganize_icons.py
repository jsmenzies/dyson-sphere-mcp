#!/usr/bin/env python3
"""
Reorganize icons into buildings/items/research with high-res/low-res subdirectories.
Icons >= 80x80 go to high-res, smaller ones go to low-res.
"""

import json
import os
import shutil
import subprocess
import re
from pathlib import Path
from typing import Dict, Tuple, List

# Paths
SCRIPT_DIR = Path(__file__).parent
REPO_ROOT = SCRIPT_DIR.parent.parent
OLD_MATERIALS_DIR = REPO_ROOT / "web" / "static" / "icons" / "materials"
TECH_DIR = REPO_ROOT / "web" / "static" / "icons" / "tech"
ICONS_BASE = REPO_ROOT / "web" / "static" / "icons"
DATA_DIR = REPO_ROOT / "web" / "src" / "lib" / "data"

# Building item IDs and names based on DSP wiki categorization
# NOTE: Foundation, Logistics Drone, Logistics Vessel, and Small Carrier Rocket are ITEMS, not buildings
BUILDING_PATTERNS = [
    # Production buildings
    "assembling machine", "smelter", "chemical plant", "refinery", "fractionator",
    "particle collider", "matrix lab", "oil refinery", "arc smelter", "plane smelter",

    # Power buildings
    "wind turbine", "solar panel", "thermal power", "geothermal", "mini fusion",
    "artificial star", "ray receiver", "energy exchanger",

    # Mining/extraction
    "mining machine", "advanced mining", "water pump", "oil extractor", "orbital collector",

    # Storage
    "storage mk", "storage tank", "accumulator",

    # Logistics structures (belt/sorter/station are buildings, drone/vessel are items)
    "conveyor belt", "sorter", "splitter", "traffic monitor",
    "logistics station", "planetary logistics", "interstellar logistics",

    # Power distribution
    "tesla tower", "wireless power tower", "satellite substation",

    # Dyson sphere structures (ejector/silo are buildings, rocket is an item)
    "em-rail ejector", "vertical launching silo",
]

# Item ID ranges for buildings (rough guide)
BUILDING_ID_RANGES = [
    (2001, 2999),  # Buildings range
]

def is_building(name: str, item_id: int = None) -> bool:
    """Determine if an item is a building based on name and ID."""
    name_lower = name.lower()

    # Check name patterns
    for pattern in BUILDING_PATTERNS:
        if pattern in name_lower:
            return True

    # Check ID ranges
    if item_id:
        for start, end in BUILDING_ID_RANGES:
            if start <= item_id <= end:
                return True

    return False

def get_image_size(image_path: Path) -> Tuple[int, int]:
    """Get image dimensions using the file command."""
    try:
        result = subprocess.run(
            ["file", str(image_path)],
            capture_output=True,
            text=True,
            check=True
        )
        output = result.stdout
        # Parse output like "PNG image data, 128 x 128, ..."
        match = re.search(r'(\d+)\s*x\s*(\d+)', output)
        if match:
            width = int(match.group(1))
            height = int(match.group(2))
            return (width, height)
        return (0, 0)
    except Exception as e:
        print(f"  ⚠ Could not read image {image_path}: {e}")
        return (0, 0)

def is_high_res(width: int, height: int) -> bool:
    """Determine if image is high-res (>= 80x80)."""
    return width >= 80 and height >= 80

def reorganize_icons():
    """Reorganize icons into new structure."""

    # Create new directory structure
    print("Creating new directory structure...")
    for category in ["buildings", "items", "research"]:
        for quality in ["high-res", "low-res"]:
            dir_path = ICONS_BASE / category / quality
            dir_path.mkdir(parents=True, exist_ok=True)
            print(f"  ✓ Created {dir_path}")

    # Load existing material icons data
    material_icons_file = DATA_DIR / "material-icons.json"
    with open(material_icons_file) as f:
        material_icons = json.load(f)

    # Load tech icons data
    tech_icons_file = DATA_DIR / "tech-icons.json"
    with open(tech_icons_file) as f:
        tech_icons = json.load(f)

    # Categorize and move icons
    buildings_data = {}
    items_data = {}
    research_data = {}

    stats = {
        "buildings_highres": 0,
        "buildings_lowres": 0,
        "items_highres": 0,
        "items_lowres": 0,
        "research_highres": 0,
        "research_lowres": 0,
    }

    print("\n" + "=" * 60)
    print("Processing material icons...")
    print("=" * 60)

    for name, data in material_icons.items():
        old_icon_path = data["icon"]
        old_file_path = REPO_ROOT / "web" / "static" / old_icon_path.lstrip("/")

        if not old_file_path.exists():
            print(f"  ⚠ Icon not found: {old_file_path}")
            continue

        # Get image dimensions
        width, height = get_image_size(old_file_path)
        if width == 0 or height == 0:
            continue

        # Determine quality level
        quality = "high-res" if is_high_res(width, height) else "low-res"

        # Determine category
        item_id = data.get("itemId")
        is_bldg = is_building(name, item_id)
        category = "buildings" if is_bldg else "items"

        # Build new path
        filename = old_file_path.name
        new_file_path = ICONS_BASE / category / quality / filename
        new_icon_path = f"/icons/{category}/{quality}/{filename}"

        # Copy file
        shutil.copy2(old_file_path, new_file_path)

        # Update data
        new_data = data.copy()
        new_data["icon"] = new_icon_path
        new_data["iconHighRes"] = new_icon_path if quality == "high-res" else None
        new_data["iconLowRes"] = new_icon_path if quality == "low-res" else None

        if category == "buildings":
            buildings_data[name] = new_data
            stats[f"buildings_{quality.replace('-', '')}"] += 1
        else:
            items_data[name] = new_data
            stats[f"items_{quality.replace('-', '')}"] += 1

        print(f"  {category:10} | {quality:8} | {width:3}x{height:<3} | {name}")

    print("\n" + "=" * 60)
    print("Processing tech icons...")
    print("=" * 60)

    for tech_id, data in tech_icons.items():
        old_icon_path = data["icon"]
        old_file_path = REPO_ROOT / "web" / "static" / old_icon_path.lstrip("/")

        if not old_file_path.exists():
            print(f"  ⚠ Icon not found: {old_file_path}")
            continue

        # Get image dimensions
        width, height = get_image_size(old_file_path)

        # For WebP files (tech icons), file command can't get dimensions
        # Use file size as proxy: < 5KB = low-res, >= 5KB = high-res
        if width == 0 or height == 0:
            file_size = old_file_path.stat().st_size
            # Tech icons are typically small WebP files, treat as low-res
            quality = "high-res" if file_size >= 5000 else "low-res"
            width, height = 0, 0  # Unknown dimensions
        else:
            # Determine quality level from dimensions
            quality = "high-res" if is_high_res(width, height) else "low-res"

        # Build new path
        filename = old_file_path.name
        new_file_path = ICONS_BASE / "research" / quality / filename
        new_icon_path = f"/icons/research/{quality}/{filename}"

        # Copy file
        shutil.copy2(old_file_path, new_file_path)

        # Update data
        new_data = data.copy()
        new_data["icon"] = new_icon_path
        new_data["iconHighRes"] = new_icon_path if quality == "high-res" else None
        new_data["iconLowRes"] = new_icon_path if quality == "low-res" else None

        research_data[tech_id] = new_data
        stats[f"research_{quality.replace('-', '')}"] += 1

        name = data.get("name", tech_id)
        dims = f"{width:3}x{height:<3}" if width > 0 else "  ?x?  "
        print(f"  research   | {quality:8} | {dims} | {name}")

    # Save new JSON files
    print("\n" + "=" * 60)
    print("Saving new JSON files...")
    print("=" * 60)

    buildings_file = DATA_DIR / "buildings.json"
    with open(buildings_file, "w") as f:
        json.dump(buildings_data, f, indent=2, sort_keys=True)
    print(f"  ✓ Saved {buildings_file} ({len(buildings_data)} entries)")

    items_file = DATA_DIR / "items.json"
    with open(items_file, "w") as f:
        json.dump(items_data, f, indent=2, sort_keys=True)
    print(f"  ✓ Saved {items_file} ({len(items_data)} entries)")

    research_file = DATA_DIR / "research.json"
    with open(research_file, "w") as f:
        json.dump(research_data, f, indent=2, sort_keys=True)
    print(f"  ✓ Saved {research_file} ({len(research_data)} entries)")

    # Print statistics
    print("\n" + "=" * 60)
    print("Statistics")
    print("=" * 60)
    print(f"Buildings:")
    print(f"  High-res: {stats['buildings_highres']}")
    print(f"  Low-res:  {stats['buildings_lowres']}")
    print(f"  Total:    {stats['buildings_highres'] + stats['buildings_lowres']}")
    print()
    print(f"Items:")
    print(f"  High-res: {stats['items_highres']}")
    print(f"  Low-res:  {stats['items_lowres']}")
    print(f"  Total:    {stats['items_highres'] + stats['items_lowres']}")
    print()
    print(f"Research:")
    print(f"  High-res: {stats['research_highres']}")
    print(f"  Low-res:  {stats['research_lowres']}")
    print(f"  Total:    {stats['research_highres'] + stats['research_lowres']}")
    print()
    print(f"Grand Total: {sum(stats.values())} icons")

    print("\n" + "=" * 60)
    print("✓ Complete!")
    print("=" * 60)
    print("\nNext steps:")
    print("1. Update helper functions in TypeScript")
    print("2. Update code references to use new paths")
    print("3. Test the new icon system")
    print("4. Remove old material-icons.json and old icon directories")

if __name__ == "__main__":
    reorganize_icons()
