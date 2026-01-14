import { fetchStars } from '$lib/api';
import type { PageLoad } from './$types';

export const load: PageLoad = async ({ fetch }) => {
    try {
        const [starsRes, routesRes, ilsRes] = await Promise.all([
            fetch('/api/stars'),
            fetch('/api/logistics/routes'),
            fetch('/api/ils')
        ]);

        const starsData = await starsRes.json();
        const routesData = routesRes.ok ? await routesRes.json() : { routes: [] };
        const ilsData = ilsRes.ok ? await ilsRes.json() : { planets: [] };

        // Extract stars that have at least one planet with an ILS
        const starsWithILS = new Set<string>();
        if (ilsData.planets) {
            ilsData.planets.forEach((p: any) => {
                if (p.ilsStations && p.ilsStations.length > 0) {
                    starsWithILS.add(p.starName);
                }
            });
        }

        return {
            stars: Array.isArray(starsData) ? starsData : (starsData.stars || []),
            routes: routesData.routes || [],
            activeStars: Array.from(starsWithILS)
        };
    } catch (e) {
        console.error("API Fetch Error:", e);
    }
    
    return { stars: [], routes: [], activeStars: [] };
};
