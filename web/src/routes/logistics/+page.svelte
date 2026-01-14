<script lang="ts">
    import type { PageData } from './$types';
    import type { PlanetILSGroup, ILSStation, ILSStorage } from '$lib/types';

    export let data: PageData;
    
    function getLogicColor(logic: string) {
        switch (logic) {
            case 'Supply': return 'text-blue-400';
            case 'Demand': return 'text-orange-400';
            default: return 'text-gray-400';
        }
    }

    function formatEnergy(joules: number) {
        if (joules > 1e12) return (joules / 1e12).toFixed(2) + ' TJ';
        if (joules > 1e9) return (joules / 1e9).toFixed(2) + ' GJ';
        if (joules > 1e6) return (joules / 1e6).toFixed(2) + ' MJ';
        return joules.toFixed(0) + ' J';
    }
</script>

<div class="p-6 bg-gray-900 min-h-full text-gray-100">
    <div class="mb-6 flex justify-between items-center">
        <h2 class="text-2xl font-bold text-cyan-400">Logistics Network</h2>
        <div class="text-sm text-gray-400">
            Monitoring {data.planets.reduce((acc, p) => acc + p.ilsStations.length, 0)} stations
        </div>
    </div>

    {#if data.planets.length === 0}
        <div class="flex items-center justify-center h-64 text-gray-500 bg-gray-800 rounded-lg border border-gray-700">
            No logistics data available. Ensure the game is running and stations are built.
        </div>
    {:else}
        <div class="space-y-8">
            {#each data.planets as planet}
                <div class="bg-gray-800 rounded-lg border border-gray-700 overflow-hidden shadow-lg">
                    <div class="bg-gray-700/50 px-4 py-2 flex justify-between items-center border-b border-gray-600">
                        <div class="flex items-center space-x-3">
                            <span class="text-lg font-semibold text-white">{planet.planetName}</span>
                            <span class="text-xs text-gray-400 uppercase tracking-wider">{planet.starName} System</span>
                        </div>
                        <span class="text-xs bg-gray-600 px-2 py-1 rounded text-gray-300">
                            {planet.ilsStations.length} Stations
                        </span>
                    </div>
                    
                    <div class="p-4 grid grid-cols-1 lg:grid-cols-2 gap-4">
                        {#each planet.ilsStations as station}
                            <div class="bg-gray-900/50 rounded p-4 border border-gray-700">
                                <div class="flex justify-between items-start mb-4">
                                    <div>
                                        <div class="flex items-center space-x-2">
                                            <span class="font-mono text-cyan-500">#{station.gid}</span>
                                            <span class="text-sm font-medium">
                                                {station.isStellar ? 'Interstellar' : 'Planetary'} Station
                                            </span>
                                        </div>
                                        <div class="text-xs text-gray-500 mt-1">
                                            Entity ID: {station.entityId}
                                        </div>
                                    </div>
                                    <div class="text-right">
                                        <div class="text-xs text-gray-400 mb-1">Energy: {formatEnergy(station.energy)} / {formatEnergy(station.energyMax)}</div>
                                        <div class="w-32 h-1.5 bg-gray-700 rounded-full overflow-hidden">
                                            <div 
                                                class="h-full bg-cyan-500 transition-all duration-500" 
                                                style="width: {(station.energy / station.energyMax * 100).toFixed(1)}%"
                                            ></div>
                                        </div>
                                    </div>
                                </div>

                                <!-- Assets: Drones/Ships/Warpers -->
                                <div class="grid grid-cols-3 gap-2 mb-4">
                                    <div class="bg-gray-800/50 p-2 rounded text-center">
                                        <div class="text-[10px] text-gray-500 uppercase">Drones</div>
                                        <div class="text-sm font-semibold {station.workDroneCount > 0 ? 'text-yellow-400' : 'text-gray-300'}">
                                            {station.idleDroneCount} / {station.deliveryDrones}
                                        </div>
                                    </div>
                                    <div class="bg-gray-800/50 p-2 rounded text-center">
                                        <div class="text-[10px] text-gray-500 uppercase">Ships</div>
                                        <div class="text-sm font-semibold {station.workShipCount > 0 ? 'text-yellow-400' : 'text-gray-300'}">
                                            {station.idleShipCount} / {station.deliveryShips}
                                        </div>
                                    </div>
                                    <div class="bg-gray-800/50 p-2 rounded text-center">
                                        <div class="text-[10px] text-gray-500 uppercase">Warpers</div>
                                        <div class="text-sm font-semibold {station.warperCount < 10 ? 'text-red-400' : 'text-gray-300'}">
                                            {station.warperCount} / {station.warperMaxCount}
                                        </div>
                                    </div>
                                </div>

                                <!-- Storage -->
                                <div class="space-y-3">
                                    {#each station.storage as slot}
                                        <div>
                                            <div class="flex justify-between text-xs mb-1">
                                                <span class="font-medium text-gray-200">{slot.itemName}</span>
                                                <div class="space-x-2">
                                                    <span class={getLogicColor(slot.localLogic)}>{slot.localLogic} (L)</span>
                                                    <span class={getLogicColor(slot.remoteLogic)}>{slot.remoteLogic} (R)</span>
                                                </div>
                                            </div>
                                            <div class="flex items-center space-x-2">
                                                <div class="flex-1 h-2 bg-gray-800 rounded-full overflow-hidden border border-gray-700">
                                                    <div 
                                                        class="h-full bg-blue-500 transition-all" 
                                                        style="width: {Math.min(100, (slot.count / slot.max * 100))}%"
                                                    ></div>
                                                </div>
                                                <span class="text-[10px] font-mono text-gray-400 w-16 text-right">
                                                    {slot.count.toLocaleString()} / {slot.max.toLocaleString()}
                                                </span>
                                            </div>
                                        </div>
                                    {/each}
                                </div>
                            </div>
                        {/each}
                    </div>
                </div>
            {/each}
        </div>
    {/if}
</div>
