import type { Star } from './types';

export async function fetchStars(): Promise<Star[]> {
    const response = await fetch('/api/stars');
    if (!response.ok) {
        throw new Error(`Failed to fetch stars: ${response.statusText}`);
    }
    const data = await response.json();
    return data.stars || []; // Assuming response is { stars: [...] }
}
