<script lang="ts">
    import type { PageData } from './$types';

    export let data: PageData;
    
    $: activePlanets = data.planets.filter(p => p.networkCount > 0)
        .sort((a, b) => b.generationCapacityW - a.generationCapacityW);

    function formatWatts(watts: number) {
        if (watts >= 1e12) return (watts / 1e12).toFixed(2) + ' TW';
        if (watts >= 1e9) return (watts / 1e9).toFixed(2) + ' GW';
        if (watts >= 1e6) return (watts / 1e6).toFixed(2) + ' MW';
        if (watts >= 1e3) return (watts / 1e3).toFixed(2) + ' kW';
        return watts.toFixed(0) + ' W';
    }

    function getSatisfactionColor(percent: number) {
        if (percent >= 100) return 'text-green-400';
        if (percent >= 80) return 'text-yellow-400';
        return 'text-red-400';
    }

    function getSatisfactionBg(percent: number) {
        if (percent >= 100) return 'bg-green-500';
        if (percent >= 80) return 'bg-yellow-500';
        return 'bg-red-500';
    }
</script>

<div class="p-6 bg-gray-900 min-h-full text-gray-100">
    <div class="mb-6">
        <h2 class="text-2xl font-bold text-cyan-400">Power Grid Status</h2>
        <p class="text-sm text-gray-400">Monitoring {activePlanets.length} active planetary grids</p>
    </div>

    {#if activePlanets.length === 0}
        <div class="flex items-center justify-center h-64 text-gray-500 bg-gray-800 rounded-lg border border-gray-700">
            No active power networks detected.
        </div>
    {:else}
        <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
            {#each activePlanets as planet}
                <div class="bg-gray-800 rounded-lg border border-gray-700 overflow-hidden shadow-lg flex flex-col">
                    <div class="px-4 py-3 bg-gray-700/50 border-b border-gray-600 flex justify-between items-center">
                        <div>
                            <div class="font-bold text-white">{planet.planetName}</div>
                            <div class="text-[10px] text-gray-400 uppercase tracking-tighter">{planet.starName}</div>
                        </div>
                        <div class="text-right">
                            <div class="text-xs font-mono {getSatisfactionColor(planet.satisfactionPercent)}">
                                {planet.satisfactionPercent.toFixed(1)}% Saturation
                            </div>
                        </div>
                    </div>
                    
                    <div class="p-4 flex-1 space-y-4">
                        <!-- Satisfaction Gauge -->
                        <div class="relative pt-1">
                            <div class="overflow-hidden h-2 mb-4 text-xs flex rounded bg-gray-900 border border-gray-700">
                                <div 
                                    style="width: {Math.min(100, planet.satisfactionPercent)}%" 
                                    class="shadow-none flex flex-col text-center white-space-nowrap text-white justify-center {getSatisfactionBg(planet.satisfactionPercent)} transition-all duration-500"
                                ></div>
                            </div>
                        </div>

                        <!-- Stats Grid -->
                        <div class="grid grid-cols-2 gap-4">
                            <div class="bg-gray-900/50 p-3 rounded border border-gray-700">
                                <div class="text-[10px] text-gray-500 uppercase mb-1">Generation</div>
                                <div class="text-sm font-mono text-blue-400">{formatWatts(planet.generationCapacityW)}</div>
                            </div>
                            <div class="bg-gray-900/50 p-3 rounded border border-gray-700">
                                <div class="text-[10px] text-gray-500 uppercase mb-1">Demand</div>
                                <div class="text-sm font-mono text-orange-400">{formatWatts(planet.consumptionDemandW)}</div>
                            </div>
                        </div>

                        <div class="space-y-2">
                            <div class="flex justify-between text-xs">
                                <span class="text-gray-500">Actual Consumption:</span>
                                <span class="text-gray-300 font-mono">{formatWatts(planet.actualConsumptionW)}</span>
                            </div>
                            <div class="flex justify-between text-xs">
                                <span class="text-gray-500">Networks:</span>
                                <span class="text-gray-300 font-mono">{planet.networkCount}</span>
                            </div>
                            <div class="flex justify-between text-xs">
                                <span class="text-gray-500">Energy Stored:</span>
                                <span class="text-gray-300 font-mono">{formatWatts(planet.energyStored)}h</span>
                            </div>
                        </div>
                    </div>

                    {#if planet.satisfactionPercent < 100}
                        <div class="px-4 py-2 bg-red-900/20 border-t border-red-900/50">
                            <div class="text-[10px] text-red-400 font-bold uppercase animate-pulse">
                                Warning: Power Deficit
                            </div>
                        </div>
                    {/if}
                </div>
            {/each}
        </div>
    {/if}
</div>
