<script lang="ts">
    import type { PageData } from './$types';
    import type { ProductionStat } from '$lib/types';

    export let data: PageData;
    
    let searchTerm = "";
    
    $: filteredStats = data.stats.filter(s => 
        s.itemName.toLowerCase().includes(searchTerm.toLowerCase())
    ).sort((a, b) => b.productionRate - a.productionRate);

    function getNetRate(stat: ProductionStat) {
        return stat.productionRate - stat.consumptionRate;
    }

    function getUtilization(stat: ProductionStat) {
        if (stat.theoreticalMaxProduction === 0) return 0;
        return (stat.productionRate / stat.theoreticalMaxProduction) * 100;
    }
</script>

<div class="p-6 bg-gray-900 min-h-full text-gray-100">
    <div class="mb-6 flex justify-between items-center">
        <div>
            <h2 class="text-2xl font-bold text-cyan-400">Production Statistics</h2>
            <p class="text-sm text-gray-400">
                {data.planetId === -1 ? 'Global Cluster' : 'Planet ' + data.planetId} 
                • Time Level: {data.timeLevel === 0 ? '1m' : data.timeLevel === 1 ? '10m' : '1h+'}
            </p>
        </div>
        
        <div class="relative">
            <input 
                type="text" 
                bind:value={searchTerm}
                placeholder="Search items..."
                class="bg-gray-800 border border-gray-700 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-cyan-500 w-64"
            />
        </div>
    </div>

    <div class="bg-gray-800 rounded-lg border border-gray-700 overflow-hidden shadow-lg">
        <table class="w-full text-left text-sm">
            <thead class="bg-gray-700/50 text-gray-400 uppercase text-[10px] tracking-wider">
                <tr>
                    <th class="px-6 py-3">Item</th>
                    <th class="px-6 py-3 text-right">Production</th>
                    <th class="px-6 py-3 text-right">Consumption</th>
                    <th class="px-6 py-3 text-right">Net</th>
                    <th class="px-6 py-3">Balance</th>
                    <th class="px-6 py-3 text-right">Efficiency</th>
                </tr>
            </thead>
            <tbody class="divide-y divide-gray-700">
                {#each filteredStats as stat}
                    <tr class="hover:bg-gray-700/30 transition-colors">
                        <td class="px-6 py-4 font-medium text-white">
                            {stat.itemName}
                        </td>
                        <td class="px-6 py-4 text-right font-mono">
                            {stat.productionRate.toLocaleString()} <span class="text-[10px] text-gray-500">/m</span>
                        </td>
                        <td class="px-6 py-4 text-right font-mono">
                            {stat.consumptionRate.toLocaleString()} <span class="text-[10px] text-gray-500">/m</span>
                        </td>
                        <td class="px-6 py-4 text-right font-mono font-bold {getNetRate(stat) >= 0 ? 'text-green-400' : 'text-red-400'}">
                            {(getNetRate(stat) > 0 ? '+' : '')}{getNetRate(stat).toLocaleString()}
                        </td>
                        <td class="px-6 py-4">
                            <div class="w-32 h-2 bg-gray-900 rounded-full overflow-hidden flex">
                                {#if stat.productionRate + stat.consumptionRate > 0}
                                    {@const prodPercent = (stat.productionRate / (stat.productionRate + stat.consumptionRate)) * 100}
                                    <div class="h-full bg-blue-500" style="width: {prodPercent}%"></div>
                                    <div class="h-full bg-orange-500" style="width: {100 - prodPercent}%"></div>
                                {/if}
                            </div>
                        </td>
                        <td class="px-6 py-4 text-right">
                            <span class="text-xs px-2 py-1 rounded {getUtilization(stat) > 90 ? 'bg-yellow-900/50 text-yellow-400' : 'bg-gray-700 text-gray-400'}">
                                {getUtilization(stat).toFixed(1)}%
                            </span>
                        </td>
                    </tr>
                {/each}
            </tbody>
        </table>
        
        {#if filteredStats.length === 0}
            <div class="p-12 text-center text-gray-500">
                No items matching "{searchTerm}"
            </div>
        {/if}
    </div>
</div>
