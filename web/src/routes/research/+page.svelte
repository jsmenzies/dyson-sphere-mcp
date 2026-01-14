<script lang="ts">
    import type { PageData } from './$types';

    export let data: PageData;
    
    function formatHash(hash: number) {
        if (hash >= 1e9) return (hash / 1e9).toFixed(2) + ' G';
        if (hash >= 1e6) return (hash / 1e6).toFixed(2) + ' M';
        if (hash >= 1e3) return (hash / 1e3).toFixed(2) + ' k';
        return hash.toFixed(0);
    }
</script>

<div class="p-6 bg-gray-900 min-h-full text-gray-100">
    <div class="mb-6">
        <h2 class="text-2xl font-bold text-cyan-400">Research & Technology</h2>
        <p class="text-sm text-gray-400">Cluster-wide scientific progress</p>
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Current Research -->
        <div class="lg:col-span-2 space-y-6">
            <div class="bg-gray-800 rounded-lg border border-gray-700 p-6 shadow-lg">
                <h3 class="text-lg font-semibold mb-4 text-white flex justify-between">
                    Current Research
                    {#if data.research?.totalHashPerSecond}
                        <span class="text-cyan-400 font-mono text-sm">{formatHash(data.research.totalHashPerSecond)} H/s</span>
                    {/if}
                </h3>
                
                {#if data.research?.currentTech}
                    <div class="space-y-4">
                        <div class="flex justify-between items-end">
                            <div>
                                <div class="text-xl font-bold text-white">{data.research.currentTech.name}</div>
                                <div class="text-xs text-gray-500">ID: {data.research.currentTech.id}</div>
                            </div>
                            <div class="text-right">
                                <div class="text-sm font-mono text-cyan-400">{data.research.currentTech.progressPercent.toFixed(2)}%</div>
                                <div class="text-[10px] text-gray-500">
                                    {formatHash(data.research.currentTech.hashUploaded)} / {formatHash(data.research.currentTech.hashNeeded)} Hashes
                                </div>
                            </div>
                        </div>
                        
                        <div class="w-full h-4 bg-gray-900 rounded-full overflow-hidden border border-gray-700">
                            <div 
                                class="h-full bg-cyan-500 shadow-[0_0_10px_rgba(6,182,212,0.5)] transition-all duration-1000" 
                                style="width: {data.research.currentTech.progressPercent}%"
                            ></div>
                        </div>

                        <div class="flex justify-between text-xs text-gray-400">
                            <span>Labs Active: {data.research.totalLabCount}</span>
                            <span>Est. Time: {((data.research.currentTech.hashNeeded - data.research.currentTech.hashUploaded) / (data.research.totalHashPerSecond || 1) / 60).toFixed(1)}m remaining</span>
                        </div>
                    </div>
                {:else}
                    <div class="py-8 text-center text-gray-500 italic">No active research.</div>
                {/if}
            </div>

            <!-- Queue -->
            <div class="bg-gray-800 rounded-lg border border-gray-700 overflow-hidden shadow-lg">
                <div class="px-6 py-4 bg-gray-700/30 border-b border-gray-700">
                    <h3 class="font-semibold text-white">Technology Queue</h3>
                </div>
                <div class="divide-y divide-gray-700">
                    {#each data.queue.slice(1) as item}
                        <div class="px-6 py-4 flex justify-between items-center hover:bg-gray-700/20">
                            <div class="flex items-center space-x-4">
                                <span class="text-xs font-mono text-gray-500">#{item.position}</span>
                                <span class="text-sm font-medium text-gray-200">{item.name}</span>
                            </div>
                            <div class="text-xs font-mono text-gray-400">
                                {formatHash(item.hashNeeded)} Hashes
                            </div>
                        </div>
                    {/each}
                    {#if data.queue.length <= 1}
                        <div class="px-6 py-8 text-center text-gray-500 text-sm italic">Queue is empty.</div>
                    {/if}
                </div>
            </div>
        </div>

        <!-- Upgrades & Stats -->
        <div class="space-y-6">
            <div class="bg-gray-800 rounded-lg border border-gray-700 p-6 shadow-lg">
                <h3 class="text-lg font-semibold mb-4 text-white">Mecha Status</h3>
                {#if data.upgrades?.mecha}
                    <div class="space-y-3 text-sm">
                        <div class="flex justify-between">
                            <span class="text-gray-500">Core Energy:</span>
                            <span class="text-gray-300">{(data.upgrades.mecha.coreEnergyCap / 1e6).toFixed(0)} MJ</span>
                        </div>
                        <div class="flex justify-between">
                            <span class="text-gray-500">Walk Speed:</span>
                            <span class="text-gray-300">{data.upgrades.mecha.walkSpeed} m/s</span>
                        </div>
                        <div class="flex justify-between">
                            <span class="text-gray-500">Warp Speed:</span>
                            <span class="text-gray-300">{(data.upgrades.mecha.maxWarpSpeed / 40000).toFixed(2)} LY/s</span>
                        </div>
                    </div>
                {/if}
            </div>

            <div class="bg-gray-800 rounded-lg border border-gray-700 p-6 shadow-lg">
                <h3 class="text-lg font-semibold mb-4 text-white">Logistics Level</h3>
                {#if data.upgrades?.logisticsCapacity}
                    <div class="space-y-3 text-sm">
                        <div class="flex justify-between">
                            <span class="text-gray-500">Drone Capacity:</span>
                            <span class="text-gray-300">{data.upgrades.logisticsCapacity.stationDroneCount}</span>
                        </div>
                        <div class="flex justify-between">
                            <span class="text-gray-500">Ship Capacity:</span>
                            <span class="text-gray-300">{data.upgrades.logisticsCapacity.stationShipCount}</span>
                        </div>
                        <div class="flex justify-between">
                            <span class="text-gray-500">Tech Speed:</span>
                            <span class="text-gray-300">+{((data.upgrades.research.techSpeed - 1) * 100).toFixed(0)}%</span>
                        </div>
                    </div>
                {/if}
            </div>
        </div>
    </div>
</div>
