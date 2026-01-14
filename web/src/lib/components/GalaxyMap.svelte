<script lang="ts">
    import { onMount, createEventDispatcher } from 'svelte';
    import * as d3 from 'd3';
    import type { Star, ShippingRoute } from '$lib/types';

    export let stars: Star[] = [];
    export let routes: ShippingRoute[] = [];
    export let activeStars: string[] = [];
    
    const dispatch = createEventDispatcher();

    let svg: SVGSVGElement;
    let containerWidth = 800;
    let containerHeight = 600;
    let showRoutes = true;
    let showOnlyActive = false;
    let selectedStar: Star | null = null;
    
    // Zoom state
    let transform = d3.zoomIdentity;

    $: visibleStars = showOnlyActive 
        ? stars.filter(s => activeStars.includes(s.name)) 
        : stars;

    // React to changes in data or view settings
    $: if (svg && (stars.length > 0 || routes.length > 0)) {
        // We watch showRoutes/showOnlyActive/selectedStar implicitly via draw() call or explicit reactive block
        draw();
    }

    // Explicit redraw trigger for toggles
    $: showRoutes, showOnlyActive, selectedStar, draw();

    function draw() {
        if (!svg) return;
        
        const svgEl = d3.select(svg);
        svgEl.selectAll("*").remove();

        const g = svgEl.append("g");

        // 1. Scales - Always use the full 'stars' extent to keep map stable when filtering
        const xExtent = d3.extent(stars, d => d.position.x) as [number, number];
        const zExtent = d3.extent(stars, d => d.position.z) as [number, number];
        const maxDist = Math.max(
            Math.abs(xExtent[0]||60), Math.abs(xExtent[1]||60), 
            Math.abs(zExtent[0]||60), Math.abs(zExtent[1]||60)
        ) * 1.1;

        const xScale = d3.scaleLinear()
            .domain([-maxDist, maxDist])
            .range([0, containerWidth]);

        const yScale = d3.scaleLinear()
            .domain([-maxDist, maxDist])
            .range([containerHeight, 0]); // Invert Z

        // 2. Zoom Behavior
        const zoom = d3.zoom<SVGSVGElement, unknown>()
            .scaleExtent([0.5, 20])
            .on("zoom", (event) => {
                transform = event.transform;
                g.attr("transform", transform.toString());
            });

        // Apply existing transform if re-drawing
        svgEl.call(zoom).call(zoom.transform, transform);

        // 3. Draw Routes
        // Routes are visible if:
        //  a) Global 'showRoutes' is ON
        //  b) Global 'showRoutes' is OFF, but this route connects to the 'selectedStar'
        if (routes.length > 0) {
            const starMap = new Map(stars.map(s => [s.name, s]));
            const activeStarSet = new Set(activeStars);
            
            const connections = new Map<string, {origin: Star, dest: Star, count: number}>();
            
            routes.forEach(r => {
                const originStar = starMap.get(r.originStarName);
                
                let destStar: Star | undefined;
                // Try to match destination planet name to a star
                for (const star of stars) {
                    if (r.destPlanetName.startsWith(star.name)) {
                        destStar = star;
                        break;
                    }
                }

                if (originStar && destStar && originStar.id !== destStar.id) {
                    // Filter routes if "Show Only Active" is active
                    if (showOnlyActive) {
                        // If restricting to active stars, routes between non-visible stars shouldn't be drawn
                        if (!activeStarSet.has(originStar.name) || !activeStarSet.has(destStar.name)) return;
                    }

                    const key = [originStar.id, destStar.id].sort().join('-');
                    if (!connections.has(key)) {
                        connections.set(key, { origin: originStar, dest: destStar, count: 0 });
                    }
                    connections.get(key)!.count++;
                }
            });

            // Filter connections based on visibility logic
            const visibleConnections = Array.from(connections.values()).filter(conn => {
                if (showRoutes) return true;
                if (selectedStar) {
                    return conn.origin.id === selectedStar.id || conn.dest.id === selectedStar.id;
                }
                return false;
            });

            g.selectAll(".route")
                .data(visibleConnections)
                .enter()
                .append("line")
                .attr("x1", d => xScale(d.origin.position.x))
                .attr("y1", d => yScale(d.origin.position.z))
                .attr("x2", d => xScale(d.dest.position.x))
                .attr("y2", d => yScale(d.dest.position.z))
                .attr("stroke", d => (selectedStar && (d.origin.id === selectedStar.id || d.dest.id === selectedStar.id)) 
                    ? "rgba(6, 182, 212, 0.8)"  // Highlighted route
                    : "rgba(6, 182, 212, 0.3)"  // Dim route
                )
                .attr("stroke-width", d => Math.sqrt(d.count) * (selectedStar ? 3 : 2))
                .attr("stroke-dasharray", "5,5")
                .attr("class", "route transition-all duration-300");
        }

        // 4. Color Scale
        const colorScale = (type: string) => {
            if (type.startsWith('O')) return '#9db4ff';
            if (type.startsWith('B')) return '#aabfff';
            if (type.startsWith('A')) return '#cad8ff';
            if (type.startsWith('F')) return '#fbf8ff';
            if (type.startsWith('G')) return '#fff4e8';
            if (type.startsWith('K')) return '#ffddb4';
            if (type.startsWith('M')) return '#ffbd6f';
            if (type.includes('Neutron')) return '#00ffff';
            if (type.includes('Black')) return '#333333';
            if (type.includes('Giant')) return '#ff0000';
            return '#ffffff';
        };

        // 5. Draw Stars
        g.selectAll("circle")
            .data(visibleStars)
            .enter()
            .append("circle")
            .attr("cx", d => xScale(d.position.x))
            .attr("cy", d => yScale(d.position.z))
            .attr("r", d => {
                const baseR = Math.max(3, Math.log(d.luminosity || 1) * 3);
                return selectedStar?.id === d.id ? baseR * 1.5 : baseR;
            })
            .attr("fill", d => colorScale(d.type))
            .attr("stroke", d => selectedStar?.id === d.id ? "#06b6d4" : "#333")
            .attr("stroke-width", d => selectedStar?.id === d.id ? 3 : 1)
            .attr("class", "cursor-pointer hover:stroke-white hover:stroke-2 transition-all")
            .on("click", (event, d) => {
                event.stopPropagation(); // Prevent zoom click
                selectedStar = d;
                dispatch('select', d);
                draw(); // Redraw to update routes/highlights
            })
            .append("title")
            .text(d => `${d.name} (${d.type})`);
            
        // 6. Draw Labels
        g.selectAll("text")
            .data(visibleStars)
            .enter()
            .append("text")
            .attr("x", d => xScale(d.position.x))
            .attr("y", d => yScale(d.position.z) + 12)
            .attr("text-anchor", "middle")
            .attr("fill", d => selectedStar?.id === d.id ? "#fff" : "#aaa")
            .attr("font-weight", d => selectedStar?.id === d.id ? "bold" : "normal")
            .attr("font-size", "10px")
            .attr("pointer-events", "none")
            .text(d => d.name);
    }
    
    // Clear selection when clicking empty space
    function handleContainerClick() {
        selectedStar = null;
        draw();
    }
</script>

<!-- svelte-ignore a11y-click-events-have-key-events -->
<!-- svelte-ignore a11y-no-noninteractive-element-to-interactive-role -->
<div 
    class="w-full h-full bg-black overflow-hidden relative" 
    bind:clientWidth={containerWidth} 
    bind:clientHeight={containerHeight}
    on:click={handleContainerClick}
    role="button"
    tabindex="0"
>
    <div class="absolute top-4 left-4 z-10 flex flex-col space-y-2">
        <button 
            class="bg-gray-800/80 backdrop-blur border border-gray-700 px-3 py-1 rounded text-xs text-white hover:bg-gray-700 transition-colors w-32 shadow-lg"
            on:click|stopPropagation={() => { showRoutes = !showRoutes; }}
        >
            {showRoutes ? 'Hide' : 'Show'} Routes
        </button>
        <button 
            class="bg-gray-800/80 backdrop-blur border border-gray-700 px-3 py-1 rounded text-xs text-white hover:bg-gray-700 transition-colors w-32 shadow-lg {showOnlyActive ? 'ring-1 ring-cyan-500 text-cyan-400' : ''}"
            on:click|stopPropagation={() => { showOnlyActive = !showOnlyActive; }}
        >
            {showOnlyActive ? 'Show All Stars' : 'Active Only'}
        </button>
    </div>
    <svg bind:this={svg} width={containerWidth} height={containerHeight} class="w-full h-full"></svg>
</div>
