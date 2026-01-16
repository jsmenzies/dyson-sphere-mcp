import type { PageLoad } from './$types';

export const load: PageLoad = async ({ fetch }) => {
    const [starsRes, planetsRes] = await Promise.all([
        fetch('/api/stars'),
        fetch('/api/planets')
    ]);

    return {
        stars: starsRes.ok ? await starsRes.json() : [],
        planets: planetsRes.ok ? await planetsRes.json() : [],
        // Hardcoded item list for now
        items: [
            { id: 1208, name: 'Critical Photon' }
        ]
    };
};
