<script lang="ts">
    import {onDestroy, onMount} from 'svelte';
    import {browser} from '$app/environment';
    import * as d3 from 'd3';
    import type {Planet, RouteAggregation, Star} from '$lib/types';

    export let stars: Star[] = [];
    export let planets: Planet[] = [];
    export let routes: RouteAggregation[] = [];
    export let ships: any[] = [];

    let container: HTMLDivElement;
    let width = 0;
    let height = 0;
    let svg: d3.Selection<SVGSVGElement, unknown, null, undefined>;
    let g: d3.Selection<SVGGElement, unknown, null, undefined>;

    // FPS tracking
    let fps = 0;
    let frameCount = 0;
    let lastTime = 0;
    let fpsRafId: number | null = null;

    // Star colors based on spectral type
    const spectralColors: Record<string, string> = {
        'O': '#6699ff',  // Blue
        'B': '#aaccff',  // Blue-white
        'A': '#ffffff',  // White
        'F': '#ffffaa',  // Yellow-white
        'G': '#ffff66',  // Yellow
        'K': '#ffaa33',  // Orange
        'M': '#ff6633',  // Red
        'X': '#00ffff',  // Cyan (White Dwarf, Neutron Star)
    };

    function getStarColor(star: Star): string {
        if (star.type === 'BlackHole') return '#333333';
        return spectralColors[star.spectr] || '#ffffff';
    }

    function buildPlanetStarMap(planets: Planet[]): Map<string, string> {
        const map = new Map<string, string>();
        for (const planet of planets) {
            map.set(planet.name, planet.starName);
        }
        return map;
    }

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

    function getActiveStars(routes: RouteAggregation[]): Set<number> {
        const activeIds = new Set<number>();
        for (const route of routes) {
            activeIds.add(route.fromStar.id);
            activeIds.add(route.toStar.id);
        }
        return activeIds;
    }

    let renderQueued = false;

    function scheduleRender() {
        if (!browser || !svg || !g) return;
        if (renderQueued) return;
        renderQueued = true;
        requestAnimationFrame(() => {
            renderQueued = false;
            render();
        });
    }

    function ensureGroups() {
        if (g.select('g.routes').empty()) g.append('g').attr('class', 'routes');
        if (g.select('g.stars').empty()) g.append('g').attr('class', 'stars');
        if (g.select('g.ships').empty()) g.append('g').attr('class', 'ships');
        if (g.select('text.empty-state').empty()) {
            g.append('text')
                .attr('class', 'empty-state')
                .attr('text-anchor', 'middle')
                .attr('fill', '#6b7280')
                .attr('font-size', '1.2rem');
        }
    }


    function render() {
        if (!svg || !g || width === 0 || height === 0) return;

        ensureGroups();

        const emptyState = g.select<SVGTextElement>('text.empty-state');

        const activeStarIds = getActiveStars(routes);
        const activeStars = stars.filter(s => activeStarIds.has(s.id));

        if (activeStars.length === 0) {
            // Show message when no routes
            g.select('g.routes').attr('display', 'none');
            g.select('g.stars').attr('display', 'none');
            g.select('g.ships').attr('display', 'none');

            emptyState
                .attr('display', null)
                .attr('x', width / 2)
                .attr('y', height / 2)
                .text('Select an item to view transport routes');

            return;
        }

        emptyState.attr('display', 'none');
        g.select('g.routes').attr('display', null);
        g.select('g.stars').attr('display', null);
        g.select('g.ships').attr('display', null);

        // Calculate bounds from active stars
        const padding = 80;
        const xExtent = d3.extent(activeStars, s => s.position.x) as [number, number];
        const zExtent = d3.extent(activeStars, s => s.position.z) as [number, number];

        // Add some padding to extents
        const xPad = (xExtent[1] - xExtent[0]) * 0.1 || 5;
        const zPad = (zExtent[1] - zExtent[0]) * 0.1 || 5;

        const xScale = d3.scaleLinear()
            .domain([xExtent[0] - xPad, xExtent[1] + xPad])
            .range([padding, width - padding]);

        const yScale = d3.scaleLinear()
            .domain([zExtent[0] - zPad, zExtent[1] + zPad])
            .range([height - padding, padding]);

        // ---- Routes: data join (no full clear) ----
        type RouteDatum = RouteAggregation & { _key: string };
        const routeData: RouteDatum[] = routes
            .filter((r) => r.forwardShips > 0 || r.backwardShips > 0)
            .flatMap((r) => {
                const baseKey = `${r.fromStar.id}-${r.toStar.id}`;
                const out: RouteDatum[] = [];
                if (r.forwardShips > 0) out.push(Object.assign({}, r, {_key: `${baseKey}-f`}));
                if (r.backwardShips > 0) out.push(Object.assign({}, r, {_key: `${baseKey}-b`}));
                return out;
            });

        const routeSel = g
            .select('g.routes')
            .selectAll<SVGLineElement, RouteDatum>('line.route')
            .data(routeData, (d) => d._key);

        routeSel.exit().remove();

        const routeEnter = routeSel
            .enter()
            .append('line')
            .attr('class', (d) => `route ${d._key.endsWith('-f') ? 'forward' : 'backward'}`)
            .attr('stroke', '#00d4ff')
            .attr('stroke-width', 2)
            .attr('stroke-dasharray', '8 12')
            .attr('stroke-opacity', 0.8)
            .attr('filter', 'url(#glow)');

        routeEnter
            .merge(routeSel)
            .attr('x1', (d) => xScale(d.fromStar.position.x))
            .attr('y1', (d) => yScale(d.fromStar.position.z))
            .attr('x2', (d) => xScale(d.toStar.position.x))
            .attr('y2', (d) => yScale(d.toStar.position.z));

        // ---- Stars: data join (group per star) ----
        const starSel = g
            .select('g.stars')
            .selectAll<SVGGElement, Star>('g.star')
            .data(activeStars, (d) => d.id);

        starSel.exit().remove();

        const starEnter = starSel.enter().append('g').attr('class', 'star');

        starEnter
            .append('circle')
            .attr('class', 'star-glow')
            .attr('r', 12)
            .attr('opacity', 0.3)
            .attr('filter', 'url(#starGlow)');

        starEnter.append('circle').attr('class', 'star-core').attr('r', 6).style('cursor', 'pointer');

        starEnter
            .append('text')
            .attr('class', 'star-label')
            .attr('text-anchor', 'middle')
            .attr('fill', '#ffffff')
            .attr('font-size', '0.75rem')
            .attr('opacity', 0.9);

        const starMerge = starEnter.merge(starSel);

        starMerge.attr('transform', (d) => `translate(${xScale(d.position.x)},${yScale(d.position.z)})`);

        starMerge.select<SVGCircleElement>('circle.star-glow').attr('fill', (d) => getStarColor(d));
        starMerge.select<SVGCircleElement>('circle.star-core').attr('fill', (d) => getStarColor(d));
        starMerge.select<SVGTextElement>('text.star-label').attr('y', -16).text((d) => d.displayName || d.name);

        // ---- Ships: join and update positions ----
        const planetStarMap = buildPlanetStarMap(planets);
        const starNameMap = buildStarNameMap(activeStars);

        type ShipDatum = { _key: string; x: number; y: number };
        const shipData: ShipDatum[] = [];

        for (const ship of ships) {
            if (ship.itemCount === 0) continue;
            if (ship.t <= 0 || ship.t >= 1) continue;

            const originStarName = planetStarMap.get(ship.originPlanet);
            const destStarName = planetStarMap.get(ship.destPlanet);
            if (!originStarName || !destStarName) continue;
            if (originStarName === destStarName) continue;

            const fromStar = starNameMap.get(originStarName);
            const toStar = starNameMap.get(destStarName);
            if (!fromStar || !toStar) continue;

            const x1 = xScale(fromStar.position.x);
            const y1 = yScale(fromStar.position.z);
            const x2 = xScale(toStar.position.x);
            const y2 = yScale(toStar.position.z);

            shipData.push({
                _key: `${ship.id ?? `${ship.originPlanet}-${ship.destPlanet}`}@${originStarName}->${destStarName}`,
                x: x1 + (x2 - x1) * ship.t,
                y: y1 + (y2 - y1) * ship.t
            });
        }

        const shipSel = g
            .select('g.ships')
            .selectAll<SVGGElement, ShipDatum>('g.ship')
            .data(shipData, (d) => d._key);

        shipSel.exit().remove();

        const shipEnter = shipSel.enter().append('g').attr('class', 'ship');
        shipEnter
            .append('circle')
            .attr('class', 'ship-glow')
            .attr('r', 8)
            .attr('fill', '#ffffff')
            .attr('opacity', 0.3)
            .attr('filter', 'url(#shipGlow)');

        shipEnter.append('circle').attr('class', 'ship-core').attr('r', 3).attr('fill', '#ffffff').attr('opacity', 0.9);

        shipEnter.merge(shipSel).attr('transform', (d) => `translate(${d.x},${d.y})`);
    }

    function setupSvg() {
        if (!browser || !container) return;

        d3.select(container).select('svg').remove();

        svg = d3.select(container).append('svg').attr('width', width).attr('height', height).style('background', '#0d0d1a');

        const defs = svg.append('defs');

        const glowFilter = defs
            .append('filter')
            .attr('id', 'glow')
            .attr('x', '-50%')
            .attr('y', '-50%')
            .attr('width', '200%')
            .attr('height', '200%');

        glowFilter.append('feGaussianBlur').attr('stdDeviation', '3').attr('result', 'coloredBlur');
        const glowMerge = glowFilter.append('feMerge');
        glowMerge.append('feMergeNode').attr('in', 'coloredBlur');
        glowMerge.append('feMergeNode').attr('in', 'SourceGraphic');

        const starGlowFilter = defs
            .append('filter')
            .attr('id', 'starGlow')
            .attr('x', '-100%')
            .attr('y', '-100%')
            .attr('width', '300%')
            .attr('height', '300%');

        starGlowFilter.append('feGaussianBlur').attr('stdDeviation', '4').attr('result', 'blur');

        const shipGlowFilter = defs
            .append('filter')
            .attr('id', 'shipGlow')
            .attr('x', '-150%')
            .attr('y', '-150%')
            .attr('width', '400%')
            .attr('height', '400%');

        shipGlowFilter.append('feGaussianBlur').attr('stdDeviation', '5').attr('result', 'blur');
        const shipMerge = shipGlowFilter.append('feMerge');
        shipMerge.append('feMergeNode').attr('in', 'blur');
        shipMerge.append('feMergeNode').attr('in', 'SourceGraphic');

        g = svg.append('g');

        scheduleRender();
    }

    function updateFPS(timestamp: number) {
        if (!browser) return;

        if (lastTime === 0) lastTime = timestamp;
        frameCount++;

        const elapsed = timestamp - lastTime;
        if (elapsed >= 1000) {
            fps = Math.round((frameCount * 1000) / elapsed);
            frameCount = 0;
            lastTime = timestamp;
        }

        fpsRafId = requestAnimationFrame(updateFPS);
    }

    function handleResize() {
        if (!browser || !container) return;
        width = container.clientWidth;
        height = container.clientHeight;

        if (svg) {
            svg.attr('width', width).attr('height', height);
            scheduleRender();
        }
    }

    onMount(() => {
        if (!browser) return;
        handleResize();
        setupSvg();
        window.addEventListener('resize', handleResize, { passive: true });
        fpsRafId = requestAnimationFrame(updateFPS);
    });

    onDestroy(() => {
        if (!browser) return;
        window.removeEventListener('resize', handleResize);
        if (fpsRafId != null) cancelAnimationFrame(fpsRafId);
    });

    // Re-render when inputs change (debounced to RAF)
    $: if (browser && svg) {
        // Reference the reactive deps explicitly
        routes; stars; planets; ships; width; height;
        scheduleRender();
    }
</script>

<div class="galaxy-map" bind:this={container} bind:clientWidth={width} bind:clientHeight={height}>
    <div class="fps-counter">
        {fps} FPS
    </div>
</div>

<style>
    /* Replace SMIL with CSS animation */
    :global(.galaxy-map .route) {
        animation: routeDash 1.5s linear infinite, routePulse 2s ease-in-out infinite;
    }

    :global(.galaxy-map .route.forward) {
        animation-direction: normal, normal;
    }

    :global(.galaxy-map .route.backward) {
        animation-direction: reverse, normal;
    }

    @keyframes routeDash {
        from {
            stroke-dashoffset: 0;
        }
        to {
            stroke-dashoffset: -20;
        }
    }

    @keyframes routePulse {
        0%,
        100% {
            stroke-opacity: 0.6;
        }
        50% {
            stroke-opacity: 1;
        }
    }

    @media (prefers-reduced-motion: reduce) {
        :global(.galaxy-map .route) {
            animation: none;
        }
    }

    .galaxy-map {
        width: 100%;
        height: 100%;
        position: relative;
        overflow: hidden;
    }

    :global(.galaxy-map svg) {
        display: block;
    }

    .fps-counter {
        position: absolute;
        top: 1rem;
        right: 1rem;
        padding: 0.5rem 1rem;
        background: rgba(13, 13, 26, 0.9);
        border: 1px solid rgba(0, 212, 255, 0.3);
        border-radius: 6px;
        color: var(--accent-cyan, #00d4ff);
        font-size: 0.875rem;
        font-weight: 600;
        font-family: monospace;
        pointer-events: none;
        z-index: 10;
    }
</style>
