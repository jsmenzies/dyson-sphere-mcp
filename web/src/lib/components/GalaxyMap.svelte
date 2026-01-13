<script lang="ts">
    import { onMount, createEventDispatcher } from 'svelte';
    import * as d3 from 'd3';
    import type { Star } from '$lib/types';

    export let stars: Star[] = [];
    
    const dispatch = createEventDispatcher();

    let svg: SVGSVGElement;
    let containerWidth = 800;
    let containerHeight = 600;
    
    // Zoom state
    let transform = d3.zoomIdentity;

    $: if (svg && stars.length > 0) {
        draw();
    }

    function draw() {
        const svgEl = d3.select(svg);
        svgEl.selectAll("*").remove();

        const g = svgEl.append("g");

        // 1. Scales
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
            .scaleExtent([0.5, 10])
            .on("zoom", (event) => {
                transform = event.transform;
                g.attr("transform", transform.toString());
            });

        svgEl.call(zoom);

        // 3. Color Scale
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

        // 4. Draw Stars
        g.selectAll("circle")
            .data(stars)
            .enter()
            .append("circle")
            .attr("cx", d => xScale(d.position.x))
            .attr("cy", d => yScale(d.position.z))
            .attr("r", d => Math.max(3, Math.log(d.luminosity || 1) * 3))
            .attr("fill", d => colorScale(d.type))
            .attr("stroke", "#333")
            .attr("stroke-width", 1)
            .attr("class", "cursor-pointer hover:stroke-white hover:stroke-2 transition-all")
            .on("click", (event, d) => {
                event.stopPropagation(); // Prevent zoom click
                dispatch('select', d);
            })
            .append("title")
            .text(d => `${d.name} (${d.type})`);
            
        // 5. Draw Labels (Optional, simplified)
        g.selectAll("text")
            .data(stars)
            .enter()
            .append("text")
            .attr("x", d => xScale(d.position.x))
            .attr("y", d => yScale(d.position.z) + 12)
            .attr("text-anchor", "middle")
            .attr("fill", "#aaa")
            .attr("font-size", "10px")
            .attr("pointer-events", "none") // Let clicks pass through
            .text(d => d.name);
    }
</script>

<div 
    class="w-full h-full bg-black overflow-hidden" 
    bind:clientWidth={containerWidth} 
    bind:clientHeight={containerHeight}
>
    <svg bind:this={svg} width={containerWidth} height={containerHeight} class="w-full h-full"></svg>
</div>