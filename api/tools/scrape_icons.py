#!/usr/bin/env python3
"""
Scrapes building/item icons from the Dyson Sphere Program wiki.
Outputs JSON files for use in the web frontend.

Usage:
    python scrape_icons.py [--generators] [--items]

    --generators  Scrape generator building icons (default if no flag)
    --items       Scrape all item icons (not implemented yet)
"""

import json
import re
import sys
import urllib.request
import urllib.error
from pathlib import Path

# Generator buildings to scrape
GENERATOR_BUILDINGS = {
    "solar": {
        "name": "Solar Panel",
        "wiki_page": "Solar_Panel",
        "buildingId": 2205,
    },
    "wind": {
        "name": "Wind Turbine",
        "wiki_page": "Wind_Turbine",
        "buildingId": 2203,
    },
    "geothermal": {
        "name": "Geothermal Power Station",
        "wiki_page": "Geothermal_Power_Station",
        "buildingId": 2213,
    },
    "thermal": {
        "name": "Thermal Power Station",
        "wiki_page": "Thermal_Power_Station",
        "buildingId": 2204,
    },
    "fusion": {
        "name": "Mini Fusion Power Station",
        "wiki_page": "Mini_Fusion_Power_Station",
        "buildingId": 2211,
    },
    "artificial_star": {
        "name": "Artificial Star",
        "wiki_page": "Artificial_Star",
        "buildingId": 2210,
    },
    "gamma": {
        "name": "Ray Receiver",
        "wiki_page": "Ray_Receiver",
        "buildingId": 2208,
    },
}

WIKI_BASE = "https://dyson-sphere-program.fandom.com/wiki/"
OUTPUT_DIR = Path(__file__).parent.parent.parent / "web" / "src" / "lib" / "data"


def fetch_page(url: str) -> str | None:
    """Fetch a wiki page and return its HTML content."""
    try:
        req = urllib.request.Request(
            url,
            headers={"User-Agent": "DSP-MCP-IconScraper/1.0"}
        )
        with urllib.request.urlopen(req, timeout=10) as response:
            return response.read().decode("utf-8")
    except urllib.error.URLError as e:
        print(f"  Error fetching {url}: {e}")
        return None


def extract_infobox_image(html: str) -> str | None:
    """Extract the main infobox image URL from wiki page HTML."""
    # Look for the infobox image - usually in a figure or img tag with data-image-key
    # Pattern for modern Fandom wiki structure
    patterns = [
        # Modern Fandom: image in figure with data-src or src
        r'<figure[^>]*class="[^"]*pi-image[^"]*"[^>]*>.*?<a[^>]*href="([^"]+)"',
        # Alternative: direct img src in infobox
        r'<img[^>]*class="[^"]*pi-image-thumbnail[^"]*"[^>]*src="([^"]+)"',
        # data-src variant
        r'<img[^>]*class="[^"]*pi-image-thumbnail[^"]*"[^>]*data-src="([^"]+)"',
        # Fallback: any img in aside (infobox) with high resolution
        r'<aside[^>]*>.*?<img[^>]*src="([^"]+/revision/latest[^"]*)"',
    ]

    for pattern in patterns:
        match = re.search(pattern, html, re.DOTALL | re.IGNORECASE)
        if match:
            url = match.group(1)
            # Clean up URL - remove sizing parameters to get full resolution
            url = re.sub(r'/scale-to-width-down/\d+', '', url)
            url = re.sub(r'/revision/latest/scale-to-width[^?]*', '/revision/latest', url)
            # Ensure https
            if url.startswith("//"):
                url = "https:" + url
            return url

    # Final fallback: look for any image with the building name
    return None


def scrape_generator_icons() -> dict:
    """Scrape icons for all generator buildings."""
    print("Scraping generator building icons from wiki...")
    results = {}

    for gen_type, info in GENERATOR_BUILDINGS.items():
        wiki_url = WIKI_BASE + info["wiki_page"]
        print(f"  Fetching {info['name']}...")

        html = fetch_page(wiki_url)
        if not html:
            print(f"    Failed to fetch page")
            continue

        icon_url = extract_infobox_image(html)
        if icon_url:
            results[gen_type] = {
                "name": info["name"],
                "buildingId": info["buildingId"],
                "icon": icon_url,
                "wikiUrl": wiki_url,
            }
            print(f"    Found icon: {icon_url[:60]}...")
        else:
            print(f"    Could not extract icon URL")
            # Use a placeholder
            results[gen_type] = {
                "name": info["name"],
                "buildingId": info["buildingId"],
                "icon": None,
                "wikiUrl": wiki_url,
            }

    return results


def main():
    args = sys.argv[1:]

    # Default to generators if no args
    if not args:
        args = ["--generators"]

    if "--generators" in args:
        icons = scrape_generator_icons()

        # Ensure output directory exists
        OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
        output_file = OUTPUT_DIR / "generator-icons.json"

        with open(output_file, "w", encoding="utf-8") as f:
            json.dump(icons, f, indent=2)

        print(f"\nSaved {len(icons)} generator icons to {output_file}")

        # Print summary
        found = sum(1 for v in icons.values() if v.get("icon"))
        print(f"Found icons: {found}/{len(icons)}")

    if "--items" in args:
        print("Item icon scraping not yet implemented")


if __name__ == "__main__":
    main()
