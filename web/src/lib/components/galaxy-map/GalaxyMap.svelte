<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import { browser } from '$app/environment';
    import * as d3 from 'd3';
    import type { Star, Planet, RouteAggregation } from '$lib/types';

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
    let fpsInterval: number;

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

    // Get stars that are part of routes
    function getActiveStars(routes: RouteAggregation[]): Set<number> {
        const activeIds = new Set<number>();
        for (const route of routes) {
            activeIds.add(route.fromStar.id);
            activeIds.add(route.toStar.id);
        }
        return activeIds;
    }

    function render() {
        if (!svg || !g || width === 0 || height === 0) return;

        // Clear previous content
        g.selectAll('*').remove();

        const activeStarIds = getActiveStars(routes);
        const activeStars = stars.filter(s => activeStarIds.has(s.id));

        if (activeStars.length === 0) {
            // Show message when no routes
            g.append('text')
                .attr('x', width / 2)
                .attr('y', height / 2)
                .attr('text-anchor', 'middle')
                .attr('fill', '#6b7280')
                .attr('font-size', '1.2rem')
                .text('Select an item to view transport routes');
            return;
        }

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

        // Draw routes first (behind stars)
        const routeGroup = g.append('g').attr('class', 'routes');

        for (const route of routes) {
            const x1 = xScale(route.fromStar.position.x);
            const y1 = yScale(route.fromStar.position.z);
            const x2 = xScale(route.toStar.position.x);
            const y2 = yScale(route.toStar.position.z);

            // Calculate line length for animation
            const dx = x2 - x1;
            const dy = y2 - y1;
            const length = Math.sqrt(dx * dx + dy * dy);

            // Forward direction ships (fromStar to toStar)
            if (route.forwardShips > 0) {
                const forwardPath = routeGroup.append('line')
                    .attr('x1', x1)
                    .attr('y1', y1)
                    .attr('x2', x2)
                    .attr('y2', y2)
                    .attr('stroke', '#00d4ff')
                    .attr('stroke-width', 2)
                    .attr('stroke-dasharray', '8 12')
                    .attr('stroke-dashoffset', 0)
                    .attr('stroke-opacity', 0.8)
                    .attr('filter', 'url(#glow)');

                // Animate dash offset to create traveling dots effect
                forwardPath.append('animate')
                    .attr('attributeName', 'stroke-dashoffset')
                    .attr('from', 0)
                    .attr('to', -20)
                    .attr('dur', '1.5s')
                    .attr('repeatCount', 'indefinite');

                // Pulsing opacity
                forwardPath.append('animate')
                    .attr('attributeName', 'stroke-opacity')
                    .attr('values', '0.6;1;0.6')
                    .attr('dur', '2s')
                    .attr('repeatCount', 'indefinite');
            }

            // Backward direction ships (toStar to fromStar)
            if (route.backwardShips > 0) {
                const backwardPath = routeGroup.append('line')
                    .attr('x1', x1)
                    .attr('y1', y1)
                    .attr('x2', x2)
                    .attr('y2', y2)
                    .attr('stroke', '#00d4ff')
                    .attr('stroke-width', 2)
                    .attr('stroke-dasharray', '8 12')
                    .attr('stroke-dashoffset', 0)
                    .attr('stroke-opacity', 0.8)
                    .attr('filter', 'url(#glow)');

                // Animate dash offset in reverse direction
                backwardPath.append('animate')
                    .attr('attributeName', 'stroke-dashoffset')
                    .attr('from', 0)
                    .attr('to', 20)
                    .attr('dur', '1.5s')
                    .attr('repeatCount', 'indefinite');

                // Pulsing opacity (offset from forward for visual distinction)
                backwardPath.append('animate')
                    .attr('attributeName', 'stroke-opacity')
                    .attr('values', '0.8;0.5;0.8')
                    .attr('dur', '2s')
                    .attr('repeatCount', 'indefinite');
            }
        }

        // Draw stars
        const starGroup = g.append('g').attr('class', 'stars');

        for (const star of activeStars) {
            const cx = xScale(star.position.x);
            const cy = yScale(star.position.z);
            const color = getStarColor(star);

            // Star glow
            starGroup.append('circle')
                .attr('cx', cx)
                .attr('cy', cy)
                .attr('r', 12)
                .attr('fill', color)
                .attr('opacity', 0.3)
                .attr('filter', 'url(#starGlow)');

            // Star core
            starGroup.append('circle')
                .attr('cx', cx)
                .attr('cy', cy)
                .attr('r', 6)
                .attr('fill', color)
                .style('cursor', 'pointer');

            // Star label
            starGroup.append('text')
                .attr('x', cx)
                .attr('y', cy - 16)
                .attr('text-anchor', 'middle')
                .attr('fill', '#ffffff')
                .attr('font-size', '0.75rem')
                .attr('opacity', 0.9)
                .text(star.displayName || star.name);
        }

        // Draw individual ships in transit
        if (ships.length > 0) {
            const planetStarMap = buildPlanetStarMap(planets);
            const starNameMap = buildStarNameMap(activeStars);
            const shipGroup = g.append('g').attr('class', 'ships');

            for (const ship of ships) {
                // Only show ships that are carrying items
                if (ship.itemCount === 0) continue;

                // Only show ships that are in transit (0 < t < 1)
                if (ship.t <= 0 || ship.t >= 1) continue;

                const originStarName = planetStarMap.get(ship.originPlanet);
                const destStarName = planetStarMap.get(ship.destPlanet);

                if (!originStarName || !destStarName) continue;
                if (originStarName === destStarName) continue;

                const fromStar = starNameMap.get(originStarName);
                const toStar = starNameMap.get(destStarName);

                if (!fromStar || !toStar) continue;

                // Calculate ship position along the route
                const x1 = xScale(fromStar.position.x);
                const y1 = yScale(fromStar.position.z);
                const x2 = xScale(toStar.position.x);
                const y2 = yScale(toStar.position.z);

                const shipX = x1 + (x2 - x1) * ship.t;
                const shipY = y1 + (y2 - y1) * ship.t;

                // Ship glow
                shipGroup.append('circle')
                    .attr('cx', shipX)
                    .attr('cy', shipY)
                    .attr('r', 8)
                    .attr('fill', '#ffffff')
                    .attr('opacity', 0.3)
                    .attr('filter', 'url(#shipGlow)');

                // Ship core
                shipGroup.append('circle')
                    .attr('cx', shipX)
                    .attr('cy', shipY)
                    .attr('r', 3)
                    .attr('fill', '#ffffff')
                    .attr('opacity', 0.9);
            }
        }
    }

    function setupSvg() {
        if (!browser || !container) return;

        // Clear any existing SVG
        d3.select(container).select('svg').remove();

        svg = d3.select(container)
            .append('svg')
            .attr('width', width)
            .attr('height', height)
            .style('background', '#0d0d1a');

        // Add filters for glow effects
        const defs = svg.append('defs');

        // Route glow filter
        const glowFilter = defs.append('filter')
            .attr('id', 'glow')
            .attr('x', '-50%')
            .attr('y', '-50%')
            .attr('width', '200%')
            .attr('height', '200%');

        glowFilter.append('feGaussianBlur')
            .attr('stdDeviation', '3')
            .attr('result', 'coloredBlur');

        const glowMerge = glowFilter.append('feMerge');
        glowMerge.append('feMergeNode').attr('in', 'coloredBlur');
        glowMerge.append('feMergeNode').attr('in', 'SourceGraphic');

        // Star glow filter
        const starGlowFilter = defs.append('filter')
            .attr('id', 'starGlow')
            .attr('x', '-100%')
            .attr('y', '-100%')
            .attr('width', '300%')
            .attr('height', '300%');

        starGlowFilter.append('feGaussianBlur')
            .attr('stdDeviation', '4')
            .attr('result', 'blur');

        // Ship glow filter
        const shipGlowFilter = defs.append('filter')
            .attr('id', 'shipGlow')
            .attr('x', '-150%')
            .attr('y', '-150%')
            .attr('width', '400%')
            .attr('height', '400%');

        shipGlowFilter.append('feGaussianBlur')
            .attr('stdDeviation', '5')
            .attr('result', 'blur');

        const shipMerge = shipGlowFilter.append('feMerge');
        shipMerge.append('feMergeNode').attr('in', 'blur');
        shipMerge.append('feMergeNode').attr('in', 'SourceGraphic');

        // Create main group
        g = svg.append('g');

        render();
    }

    function updateFPS(timestamp: number) {
        if (!browser) return;

        if (lastTime === 0) {
            lastTime = timestamp;
        }

        frameCount++;

        const elapsed = timestamp - lastTime;
        if (elapsed >= 1000) {
            fps = Math.round((frameCount * 1000) / elapsed);
            frameCount = 0;
            lastTime = timestamp;
        }

        requestAnimationFrame(updateFPS);
    }

    function handleResize() {
        if (!browser || !container) return;
        width = container.clientWidth;
        height = container.clientHeight;

        if (svg) {
            svg.attr('width', width).attr('height', height);
            render();
        }
    }

    onMount(() => {
        if (browser) {
            handleResize();
            setupSvg();
            window.addEventListener('resize', handleResize);
            requestAnimationFrame(updateFPS);
        }
    });

    onDestroy(() => {
        if (browser) {
            window.removeEventListener('resize', handleResize);
        }
    });

    // Re-render when routes change
    $: if (browser && routes && svg) {
        render();
    }
</script>

<div class="galaxy-map" bind:this={container} bind:clientWidth={width} bind:clientHeight={height}>
    <div class="fps-counter">
        {fps} FPS
    </div>
</div>

<style>
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
