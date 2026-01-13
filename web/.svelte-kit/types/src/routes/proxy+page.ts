// @ts-nocheck
import { fetchStars } from '$lib/api';
import type { PageLoad } from './$types';

export const load = async ({ fetch }: Parameters<PageLoad>[0]) => {
    // We pass the special 'fetch' from SvelteKit to handle requests correctly
    // But our api.ts uses global fetch. Let's rewrite api.ts or just inline for now.
    
    try {
        const response = await fetch('/api/stars');
        if (response.ok) {
            const data = await response.json();
            return {
                stars: data.stars || []
            };
        }
    } catch (e) {
        console.error("API Fetch Error:", e);
    }
    
    return { stars: [] };
};
