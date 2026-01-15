#!/usr/bin/env python3
"""
Scrapes technology and upgrade icons from the Dyson Sphere Program wiki using the MediaWiki API.
Downloads icons locally and creates a JSON mapping file.

Usage:
    python scrape_tech_icons.py

Output:
    - web/static/icons/tech/*.png - Downloaded tech icons
    - web/src/lib/data/tech-icons.json - Mapping of tech name to icon path
"""

import json
import re
import time
import urllib.request
import urllib.error
import urllib.parse
from pathlib import Path

WIKI_API = "https://dyson-sphere-program.fandom.com/api.php"
WIKI_BASE = "https://dyson-sphere-program.fandom.com/wiki/"

# Output paths
SCRIPT_DIR = Path(__file__).parent
PROJECT_ROOT = SCRIPT_DIR.parent.parent
ICONS_DIR = PROJECT_ROOT / "web" / "static" / "icons" / "tech"
DATA_DIR = PROJECT_ROOT / "web" / "src" / "lib" / "data"


def api_request(params: dict) -> dict | None:
    """Make a request to the MediaWiki API."""
    params["format"] = "json"
    url = f"{WIKI_API}?{urllib.parse.urlencode(params)}"

    try:
        req = urllib.request.Request(
            url,
            headers={"User-Agent": "DSP-MCP-TechScraper/1.0"}
        )
        with urllib.request.urlopen(req, timeout=15) as response:
            return json.loads(response.read().decode("utf-8"))
    except Exception as e:
        print(f"  API error: {e}")
        return None


def download_image(url: str, save_path: Path) -> bool:
    """Download an image from URL to local path."""
    try:
        req = urllib.request.Request(
            url,
            headers={"User-Agent": "DSP-MCP-TechScraper/1.0"}
        )
        with urllib.request.urlopen(req, timeout=15) as response:
            with open(save_path, "wb") as f:
                f.write(response.read())
        return True
    except Exception as e:
        print(f"    Download error: {e}")
        return False


def get_category_members(category: str) -> list[str]:
    """Get all page titles in a category using the API."""
    pages = []
    params = {
        "action": "query",
        "list": "categorymembers",
        "cmtitle": f"Category:{category}",
        "cmlimit": "500",
    }

    while True:
        data = api_request(params)
        if not data or "query" not in data:
            break

        for member in data["query"].get("categorymembers", []):
            title = member.get("title", "")
            # Skip category pages and list pages
            if not title.startswith("Category:") and title != "List of technologies":
                pages.append(title)

        # Handle pagination
        if "continue" in data:
            params["cmcontinue"] = data["continue"]["cmcontinue"]
        else:
            break

    return pages


def get_page_images(title: str) -> list[str]:
    """Get all image filenames for a page."""
    params = {
        "action": "query",
        "titles": title,
        "prop": "images",
        "imlimit": "50",
    }

    data = api_request(params)
    if not data or "query" not in data:
        return []

    images = []
    pages = data["query"].get("pages", {})
    for page_data in pages.values():
        for img in page_data.get("images", []):
            images.append(img.get("title", ""))

    return images


def get_image_url(filename: str) -> str | None:
    """Get the direct URL for an image file."""
    params = {
        "action": "query",
        "titles": filename,
        "prop": "imageinfo",
        "iiprop": "url",
    }

    data = api_request(params)
    if not data or "query" not in data:
        return None

    pages = data["query"].get("pages", {})
    for page_data in pages.values():
        imageinfo = page_data.get("imageinfo", [])
        if imageinfo:
            return imageinfo[0].get("url")

    return None


def find_tech_icon(images: list[str]) -> str | None:
    """Find the tech icon among page images (filename is numeric like '2904.png')."""
    for img in images:
        # Tech icons are named with just numbers like "File:2904.png"
        match = re.match(r"File:(\d+)\.png$", img)
        if match:
            return img
    return None


def sanitize_filename(name: str) -> str:
    """Convert a tech name to a safe filename."""
    name = name.lower()
    name = re.sub(r'[^a-z0-9]+', '_', name)
    name = name.strip('_')
    return name


def scrape_tech_icons():
    """Main function to scrape all tech and upgrade icons."""
    print("=" * 60)
    print("DSP Tech Icon Scraper (API-based)")
    print("=" * 60)

    # Ensure output directories exist
    ICONS_DIR.mkdir(parents=True, exist_ok=True)
    DATA_DIR.mkdir(parents=True, exist_ok=True)

    # Get all pages from both categories
    print("\nFetching Technologies category...")
    tech_pages = get_category_members("Technologies")
    print(f"  Found {len(tech_pages)} pages")

    print("\nFetching Upgrades category...")
    upgrade_pages = get_category_members("Upgrades")
    print(f"  Found {len(upgrade_pages)} pages")

    # Combine and deduplicate
    all_pages = list(dict.fromkeys(tech_pages + upgrade_pages))
    print(f"\nTotal unique pages: {len(all_pages)}")

    # Process each page
    results = {}
    success_count = 0

    for i, title in enumerate(all_pages):
        print(f"\n[{i+1}/{len(all_pages)}] {title}")

        # Get images for this page
        images = get_page_images(title)
        if not images:
            print("  No images found")
            continue

        # Find the tech icon (numeric filename)
        tech_icon = find_tech_icon(images)
        if not tech_icon:
            print(f"  No tech icon found (images: {', '.join(images[:3])}...)")
            continue

        # Get the image URL
        icon_url = get_image_url(tech_icon)
        if not icon_url:
            print(f"  Could not get URL for {tech_icon}")
            continue

        # Generate local filename
        filename = sanitize_filename(title) + ".png"
        local_path = ICONS_DIR / filename

        # Download the icon
        print(f"  Downloading {tech_icon}...")
        if download_image(icon_url, local_path):
            success_count += 1

            # Extract tech ID from icon filename
            match = re.match(r"File:(\d+)\.png$", tech_icon)
            tech_id = int(match.group(1)) if match else None

            results[title] = {
                "name": title,
                "techId": tech_id,
                "icon": f"/icons/tech/{filename}",
                "wikiUrl": WIKI_BASE + urllib.parse.quote(title.replace(" ", "_")),
            }
            print(f"  Saved: {filename} (ID: {tech_id})")
        else:
            print("  Download failed")

        # Be nice to the wiki server
        time.sleep(0.2)

    # Save the mapping JSON
    output_file = DATA_DIR / "tech-icons.json"
    with open(output_file, "w", encoding="utf-8") as f:
        json.dump(results, f, indent=2)

    print("\n" + "=" * 60)
    print(f"Complete! Downloaded {success_count}/{len(all_pages)} icons")
    print(f"Icons saved to: {ICONS_DIR}")
    print(f"Mapping saved to: {output_file}")
    print("=" * 60)

    return results


if __name__ == "__main__":
    scrape_tech_icons()
