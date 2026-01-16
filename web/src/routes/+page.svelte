<script lang="ts">
    import type { PageData } from './$types';
    import type { Star, Planet, ItemTransportResponse, RouteAggregation } from '$lib/types';
    import ItemSearch from '$lib/components/galaxy-map/ItemSearch.svelte';
    import GalaxyMap from '$lib/components/galaxy-map/GalaxyMap.svelte';

    export let data: PageData;

    let selectedItem: { id: number; name: string } | null = null;
    let transportData: ItemTransportResponse | null = null;
    let routes: RouteAggregation[] = [];
    let loading = false;

    // Build planet name to star name map
    function buildPlanetStarMap(planets: Planet[]): Map<string, string> {
        const map = new Map<string, string>();
        for (const planet of planets) {
            map.set(planet.name, planet.starName);
        }
        return map;
    }

    // Build star name to star map
    function buildStarNameMap(stars: Star[]): Map<string, Star> {
        const map = new Map<string, Star>();
        for (const star of stars) {
            map.set(star.name, star);
            if (star.displayName && star.displayName !== star.name) {
                map.set(star.displayName, star);
            }
        }
        return map;
    }

    // Aggregate ships into routes between stars
    function aggregateRoutes(
        transport: ItemTransportResponse,
        planets: Planet[],
        stars: Star[]
    ): RouteAggregation[] {
        const planetStarMap = buildPlanetStarMap(planets);
        const starNameMap = buildStarNameMap(stars);
        const routeMap = new Map<string, RouteAggregation>();

        for (const ship of transport.ships) {
            // Skip empty ships (returning or going to pick up)
            if (ship.itemCount === 0) continue;

            const originStarName = planetStarMap.get(ship.originPlanet);
            const destStarName = planetStarMap.get(ship.destPlanet);

            if (!originStarName || !destStarName) {
                console.warn(`Unknown planet: ${ship.originPlanet} or ${ship.destPlanet}`);
                continue;
            }

            // Skip intra-system routes
            if (originStarName === destStarName) continue;

            const fromStar = starNameMap.get(originStarName);
            const toStar = starNameMap.get(destStarName);

            if (!fromStar || !toStar) {
                console.warn(`Unknown star: ${originStarName} or ${destStarName}`);
                continue;
            }

            // Determine canonical order (lower ID first)
            const isForward = fromStar.id < toStar.id;
            const [star1, star2] = isForward ? [fromStar, toStar] : [toStar, fromStar];
            const key = `${star1.id}-${star2.id}`;

            // Determine if this ship is going forward or backward relative to canonical direction
            const shipGoingForward = (fromStar.id === star1.id);

            const existing = routeMap.get(key);
            if (existing) {
                existing.shipCount++;
                existing.totalItems += ship.itemCount;
                if (shipGoingForward) {
                    existing.forwardShips++;
                } else {
                    existing.backwardShips++;
                }
            } else {
                routeMap.set(key, {
                    fromStar: star1,
                    toStar: star2,
                    shipCount: 1,
                    totalItems: ship.itemCount,
                    forwardShips: shipGoingForward ? 1 : 0,
                    backwardShips: shipGoingForward ? 0 : 1
                });
            }
        }

        return Array.from(routeMap.values());
    }

    async function handleItemSelect(event: CustomEvent<{ id: number; name: string }>) {
        const item = event.detail;
        selectedItem = item;
        loading = true;

        try {
            const response = await fetch(`/api/transport/items/${item.id}`);
            if (response.ok) {
                transportData = await response.json();
                routes = aggregateRoutes(transportData, data.planets, data.stars);
            } else {
                console.error('Failed to fetch transport data');
                transportData = null;
                routes = [];
            }
        } catch (error) {
            console.error('Error fetching transport data:', error);
            transportData = null;
            routes = [];
        } finally {
            loading = false;
        }
    }
</script>

<div class="galaxy-container">
    <GalaxyMap
        stars={data.stars}
        planets={data.planets}
        {routes}
        ships={transportData?.ships ?? []}
    />

    <ItemSearch
        items={data.items}
        {selectedItem}
        shipCount={transportData?.ships?.filter(s => s.itemCount > 0).length ?? 0}
        on:select={handleItemSelect}
    />

    {#if loading}
        <div class="loading-indicator">
            Loading transport data...
        </div>
    {/if}
</div>

<style>
    .galaxy-container {
        position: relative;
        width: 100%;
        height: 100%;
        background: var(--bg-primary, #0d0d1a);
    }

    .loading-indicator {
        position: absolute;
        bottom: 1rem;
        left: 50%;
        transform: translateX(-50%);
        padding: 0.5rem 1rem;
        background: rgba(13, 13, 26, 0.9);
        border: 1px solid rgba(0, 212, 255, 0.3);
        border-radius: 6px;
        color: var(--accent-cyan, #00d4ff);
        font-size: 0.875rem;
    }
</style>
