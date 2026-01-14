import type { PageLoad } from './$types';

export const load: PageLoad = async ({ fetch }) => {
    try {
        const response = await fetch('/api/production');
        if (response.ok) {
            const data = await response.json();
            return {
                stats: data.stats || [],
                planetId: data.planetId,
                timeLevel: data.timeLevel
            };
        }
    } catch (e) {
        console.error("API Fetch Error:", e);
    }
    
    return { stats: [], planetId: -1, timeLevel: 0 };
};
