/**
 * Icon utility functions for DSP Manager
 * Provides unified icon lookup with high-res/low-res fallback
 */

import buildingsData from '$lib/data/buildings.json';
import itemsData from '$lib/data/items.json';
import researchData from '$lib/data/research.json';

export interface IconData {
	name: string;
	itemId?: number;
	icon: string;
	iconHighRes?: string | null;
	iconLowRes?: string | null;
	wikiUrl?: string;
}

type BuildingsData = Record<string, IconData>;
type ItemsData = Record<string, IconData>;
type ResearchData = Record<string, IconData>;

const buildings = buildingsData as BuildingsData;
const items = itemsData as ItemsData;
const research = researchData as ResearchData;

// Generator type to building name mapping (for power page)
const generatorTypeToBuilding: Record<string, string> = {
	solar: 'Solar Panel',
	wind: 'Wind Turbine',
	thermal: 'Thermal Power Plant',
	geothermal: 'Thermal Power Plant', // DSP uses same building
	fusion: 'Mini Fusion Power Plant',
	artificial_star: 'Artificial star',
	gamma: 'Ray Receiver'
};

// Create lookup maps
const itemIdToBuildingName = new Map<number, string>();
const itemIdToItemName = new Map<number, string>();
const techIdToResearchName = new Map<string, string>();

// Build reverse lookups
for (const [name, data] of Object.entries(buildings)) {
	if (data.itemId) {
		itemIdToBuildingName.set(data.itemId, name);
	}
}

for (const [name, data] of Object.entries(items)) {
	if (data.itemId) {
		itemIdToItemName.set(data.itemId, name);
	}
}

for (const [techId, data] of Object.entries(research)) {
	techIdToResearchName.set(techId, data.name);
}

/**
 * Get icon path for an item/building by ID or name
 * Prefers high-res, falls back to low-res if high-res not available
 */
export function getIcon(itemIdOrName: number | string, preferHighRes = true): string | null {
	const data = getIconData(itemIdOrName);
	if (!data) return null;

	if (preferHighRes && data.iconHighRes) {
		return data.iconHighRes;
	}

	if (data.iconLowRes) {
		return data.iconLowRes;
	}

	// Fallback to the default icon path
	return data.icon;
}

/**
 * Get high-res icon path, or null if not available
 */
export function getHighResIcon(itemIdOrName: number | string): string | null {
	const data = getIconData(itemIdOrName);
	return data?.iconHighRes || null;
}

/**
 * Get low-res icon path, or null if not available
 */
export function getLowResIcon(itemIdOrName: number | string): string | null {
	const data = getIconData(itemIdOrName);
	return data?.iconLowRes || null;
}

/**
 * Get full icon data for an item/building
 */
export function getIconData(itemIdOrName: number | string): IconData | null {
	if (typeof itemIdOrName === 'number') {
		// Look up by ID
		const buildingName = itemIdToBuildingName.get(itemIdOrName);
		if (buildingName) {
			return buildings[buildingName];
		}

		const itemName = itemIdToItemName.get(itemIdOrName);
		if (itemName) {
			return items[itemName];
		}

		return null;
	}

	// Check if this is a generator type that needs mapping
	const mappedName = generatorTypeToBuilding[itemIdOrName];
	if (mappedName) {
		return buildings[mappedName] || null;
	}

	// Look up by name (case-insensitive)
	const normalizedName = itemIdOrName.toLowerCase();

	for (const [name, data] of Object.entries(buildings)) {
		if (name.toLowerCase() === normalizedName) {
			return data;
		}
	}

	for (const [name, data] of Object.entries(items)) {
		if (name.toLowerCase() === normalizedName) {
			return data;
		}
	}

	return null;
}

/**
 * Get item/building name by ID
 */
export function getItemName(itemId: number): string | null {
	const buildingName = itemIdToBuildingName.get(itemId);
	if (buildingName) return buildingName;

	const itemName = itemIdToItemName.get(itemId);
	if (itemName) return itemName;

	return null;
}

/**
 * Check if an icon exists for the given item/building
 */
export function hasIcon(itemIdOrName: number | string): boolean {
	return getIconData(itemIdOrName) !== null;
}

/**
 * Get wiki URL for an item/building
 */
export function getWikiUrl(itemIdOrName: number | string): string | null {
	const data = getIconData(itemIdOrName);
	return data?.wikiUrl || null;
}

/**
 * Get all available item IDs
 */
export function getAllItemIds(): number[] {
	return Array.from(new Set([...itemIdToBuildingName.keys(), ...itemIdToItemName.keys()]));
}

/**
 * Get all available item names
 */
export function getAllItemNames(): string[] {
	return [...Object.keys(buildings), ...Object.keys(items)];
}

/**
 * Check if an item is a building
 */
export function isBuilding(itemIdOrName: number | string): boolean {
	if (typeof itemIdOrName === 'number') {
		return itemIdToBuildingName.has(itemIdOrName);
	}

	const normalizedName = itemIdOrName.toLowerCase();
	for (const name of Object.keys(buildings)) {
		if (name.toLowerCase() === normalizedName) {
			return true;
		}
	}

	return false;
}

/**
 * Get research/tech icon by tech ID
 */
export function getResearchIcon(techId: string, preferHighRes = true): string | null {
	const data = research[techId];
	if (!data) return null;

	if (preferHighRes && data.iconHighRes) {
		return data.iconHighRes;
	}

	if (data.iconLowRes) {
		return data.iconLowRes;
	}

	return data.icon;
}

/**
 * Get research data by tech ID
 */
export function getResearchData(techId: string): IconData | null {
	return research[techId] || null;
}

/**
 * Get all research/tech IDs
 */
export function getAllResearchIds(): string[] {
	return Object.keys(research);
}
