#!/usr/bin/env python3
"""
Download material icons from DSP Wiki and create material-icons.json mapping.
Merges data from Fandom Wiki item IDs and DSP Wiki icon URLs.
"""

import json
import os
import re
import time
from pathlib import Path
from typing import Dict, Optional
import urllib.request

# Paths
SCRIPT_DIR = Path(__file__).parent
REPO_ROOT = SCRIPT_DIR.parent.parent  # api/tools -> api -> repo root
ICONS_DIR = REPO_ROOT / "web" / "static" / "icons" / "materials"
ITEM_IDS_FILE = SCRIPT_DIR / "item-ids.json"
ICON_URLS_FILE = SCRIPT_DIR / "icon-urls.txt"
OUTPUT_FILE = REPO_ROOT / "web" / "src" / "lib" / "data" / "material-icons.json"

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
    """Load item IDs from JSON file. Returns {normalized_name: id}."""
    with open(ITEM_IDS_FILE) as f:
        data = json.load(f)

    # Reverse mapping: name -> id
    name_to_id = {}
    for item_id, name in data.items():
        name_to_id[normalize_name(name)] = int(item_id)

    return name_to_id

def load_icon_urls() -> Dict[str, str]:
    """Load icon URLs from text file. Returns {normalized_name: url}."""
    name_to_url = {}

    with open(ICON_URLS_FILE) as f:
        for line in f:
            line = line.strip()
            if not line:
                continue

            parts = line.split('|')
            if len(parts) != 2:
                continue

            icon_name, url = parts
            # Extract item name from "Icon Item Name.png"
            item_name = re.sub(r'^Icon\s+', '', icon_name)
            item_name = re.sub(r'\.png$', '', item_name, flags=re.IGNORECASE)

            name_to_url[normalize_name(item_name)] = url

    return name_to_url

def merge_data(name_to_id: Dict[str, int], name_to_url: Dict[str, str]) -> Dict[str, Dict]:
    """Merge item IDs and icon URLs by name."""
    merged = {}
    matched = 0
    unmatched_ids = []
    unmatched_urls = []

    # Find matches
    for norm_name, item_id in name_to_id.items():
        if norm_name in name_to_url:
            # Get original name from item_ids for display
            original_name = None
            with open(ITEM_IDS_FILE) as f:
                data = json.load(f)
                original_name = data[str(item_id)]

            merged[original_name] = {
                "itemId": item_id,
                "iconUrl": name_to_url[norm_name],
                "normalizedName": norm_name
            }
            matched += 1
        else:
            unmatched_ids.append((item_id, norm_name))

    # Find URLs without matching IDs
    for norm_name, url in name_to_url.items():
        if norm_name not in name_to_id:
            unmatched_urls.append((norm_name, url))

    print(f"✓ Matched {matched} items")
    if unmatched_ids:
        print(f"⚠ {len(unmatched_ids)} items have IDs but no icon URLs:")
        for item_id, name in sorted(unmatched_ids)[:10]:
            print(f"  - {item_id}: {name}")
        if len(unmatched_ids) > 10:
            print(f"  ... and {len(unmatched_ids) - 10} more")

    if unmatched_urls:
        print(f"⚠ {len(unmatched_urls)} icons have no matching item ID:")
        for name, url in sorted(unmatched_urls)[:10]:
            print(f"  - {name}")
        if len(unmatched_urls) > 10:
            print(f"  ... and {len(unmatched_urls) - 10} more")

    return merged

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

def download_all_icons(merged_data: Dict[str, Dict]) -> None:
    """Download all icons to local directory."""
    ICONS_DIR.mkdir(parents=True, exist_ok=True)

    print(f"\nDownloading {len(merged_data)} icons to {ICONS_DIR}")

    success = 0
    failed = 0

    for item_name, data in sorted(merged_data.items()):
        filename = to_snake_case(item_name) + ".png"
        filepath = ICONS_DIR / filename

        if filepath.exists():
            print(f"  ✓ {filename} (already exists)")
            success += 1
            continue

        print(f"  ⬇ {filename}...", end='', flush=True)

        if download_icon(data['iconUrl'], filepath):
            print(" ✓")
            success += 1
            # Rate limiting
            time.sleep(0.1)
        else:
            print(" ✗")
            failed += 1

    print(f"\n✓ Downloaded {success} icons")
    if failed > 0:
        print(f"✗ Failed to download {failed} icons")

def generate_material_icons_json(merged_data: Dict[str, Dict]) -> None:
    """Generate the final material-icons.json file."""
    output = {}

    for item_name, data in sorted(merged_data.items()):
        filename = to_snake_case(item_name) + ".png"

        # Construct wiki URL
        wiki_url_name = item_name.replace(' ', '_')
        wiki_url = f"https://dsp-wiki.com/{wiki_url_name}"

        output[item_name] = {
            "name": item_name,
            "itemId": data['itemId'],
            "icon": f"/icons/materials/{filename}",
            "wikiUrl": wiki_url
        }

    # Ensure output directory exists
    OUTPUT_FILE.parent.mkdir(parents=True, exist_ok=True)

    with open(OUTPUT_FILE, 'w') as f:
        json.dump(output, f, indent=2, sort_keys=True)

    print(f"\n✓ Generated {OUTPUT_FILE}")
    print(f"  Total items: {len(output)}")

def main():
    print("=" * 60)
    print("Dyson Sphere Program - Material Icons Downloader")
    print("=" * 60)

    print("\n1. Loading item IDs...")
    name_to_id = load_item_ids()
    print(f"   Loaded {len(name_to_id)} items with IDs")

    print("\n2. Loading icon URLs...")
    name_to_url = load_icon_urls()
    print(f"   Loaded {len(name_to_url)} icon URLs")

    print("\n3. Merging datasets...")
    merged_data = merge_data(name_to_id, name_to_url)

    print("\n4. Downloading icons...")
    download_all_icons(merged_data)

    print("\n5. Generating material-icons.json...")
    generate_material_icons_json(merged_data)

    print("\n" + "=" * 60)
    print("✓ Complete!")
    print("=" * 60)

if __name__ == "__main__":
    main()
