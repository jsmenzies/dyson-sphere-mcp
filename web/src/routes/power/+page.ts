import type { PageLoad } from './$types';

export const load: PageLoad = async ({ fetch }) => {
    try {
        const response = await fetch('/api/power');
        if (response.ok) {
            const data = await response.json();
            return {
                planets: data.planets || []
            };
        }
    } catch (e) {
        console.error("API Fetch Error:", e);
    }
    
    return { planets: [] };
};
