<script lang="ts">
    import type { PageData } from './$types';
    import GalaxyMap from '$lib/components/GalaxyMap.svelte';
    import type { Star } from '$lib/types';
    
    export let data: PageData;
    let selectedStar: Star | null = null;

    function handleSelect(event: CustomEvent<Star>) {
        selectedStar = event.detail;
    }
</script>

<div class="flex h-[calc(100vh-64px)]">
    <!-- Sidebar -->
    <div class="w-72 bg-gray-800 border-r border-gray-700 flex flex-col">
        <div class="p-4 border-b border-gray-700">
            <h2 class="text-xl font-bold text-cyan-400">Systems ({data.stars.length})</h2>
        </div>
        
        <!-- List -->
        <div class="flex-1 overflow-y-auto p-2">
            <ul class="space-y-1">
                {#each data.stars as star}
                    <button 
                        class="w-full text-left px-3 py-2 rounded text-sm truncate 
                               {selectedStar?.id === star.id ? 'bg-cyan-900 text-white' : 'text-gray-300 hover:bg-gray-700'}"
                        on:click={() => selectedStar = star}
                    >
                        {star.name} <span class="text-xs text-gray-500 ml-1">({star.type})</span>
                    </button>
                {/each}
            </ul>
        </div>

        <!-- Details Panel (Bottom of Sidebar or separate?) Let's put it in a right sidebar or overlay -->
    </div>

    <!-- Main Map Area -->
    <div class="flex-1 bg-gray-900 relative">
        {#if data.stars.length > 0}
            <GalaxyMap stars={data.stars} on:select={handleSelect} />
        {:else}
            <div class="flex items-center justify-center h-full text-gray-500">
                Loading or No Data...
            </div>
        {/if}

        <!-- Floating Details Panel -->
        {#if selectedStar}
            <div class="absolute top-4 right-4 w-80 bg-gray-800/90 backdrop-blur border border-gray-600 rounded-lg shadow-xl p-4">
                <div class="flex justify-between items-start mb-2">
                    <h3 class="text-xl font-bold text-white">{selectedStar.name}</h3>
                    <button class="text-gray-400 hover:text-white" on:click={() => selectedStar = null}>✕</button>
                </div>
                <div class="space-y-2 text-sm text-gray-300">
                    <div class="grid grid-cols-2 gap-2">
                        <span class="text-gray-500">Type:</span> <span>{selectedStar.type}</span>
                        <span class="text-gray-500">Luminosity:</span> <span>{selectedStar.luminosity.toFixed(3)}</span>
                        <span class="text-gray-500">Mass:</span> <span>{selectedStar.mass.toFixed(3)}</span>
                        <span class="text-gray-500">Planets:</span> <span>{selectedStar.planetCount}</span>
                        <span class="text-gray-500">Position:</span> <span>{selectedStar.position.x.toFixed(1)}, {selectedStar.position.y.toFixed(1)}, {selectedStar.position.z.toFixed(1)}</span>
                    </div>
                </div>
            </div>
        {/if}
    </div>
</div>