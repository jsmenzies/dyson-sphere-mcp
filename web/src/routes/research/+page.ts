import type { PageLoad } from './$types';

export const load: PageLoad = async ({ fetch }) => {
    try {
        const [researchRes, queueRes, upgradesRes, byPlanetRes] = await Promise.all([
            fetch('/api/research'),
            fetch('/api/research/tech-queue'),
            fetch('/api/research/upgrades'),
            fetch('/api/research/by-planet')
        ]);

        return {
            research: researchRes.ok ? await researchRes.json() : null,
            queue: queueRes.ok ? (await queueRes.json()).queue : [],
            upgrades: upgradesRes.ok ? await upgradesRes.json() : null,
            byPlanet: byPlanetRes.ok ? await byPlanetRes.json() : null
        };
    } catch (e) {
        console.error("API Fetch Error:", e);
    }

    return { research: null, queue: [], upgrades: null, byPlanet: null };
};
