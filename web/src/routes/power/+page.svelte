<script lang="ts">
    import type { PageData } from './$types';
    import type { GeneratorType } from '$lib/types';
    import CircularGauge from '$lib/components/ui/CircularGauge.svelte';
    import MetricCard from '$lib/components/ui/MetricCard.svelte';
    import generatorIcons from '$lib/data/generator-icons.json';
    import { image } from 'd3';

    export let data: PageData;

    // Star filter state
    let selectedStar = 'all';

    $: activePlanets = data.planets.filter(p => p.networkCount > 0)
        .sort((a, b) => b.generationCapacityW - a.generationCapacityW);

    // Get unique stars with planet counts
    $: starOptions = (() => {
        const starMap = new Map<string, number>();
        activePlanets.forEach(p => {
            starMap.set(p.starName, (starMap.get(p.starName) || 0) + 1);
        });
        return Array.from(starMap.entries())
            .map(([name, count]) => ({ name, count }))
            .sort((a, b) => b.count - a.count);
    })();

    // Filter planets by selected star
    $: filteredPlanets = selectedStar === 'all'
        ? activePlanets
        : activePlanets.filter(p => p.starName === selectedStar);

    // Generator icon lookup
    function getGeneratorIcon(type: GeneratorType): string {
        const iconData = generatorIcons[type as keyof typeof generatorIcons];
        return iconData?.icon || '';
    }

    function getGeneratorName(type: GeneratorType): string {
        const iconData = generatorIcons[type as keyof typeof generatorIcons];
        return iconData?.name || type;
    }

    // Calculate cluster totals
    $: clusterTotals = activePlanets.reduce((acc, p) => ({
        generation: acc.generation + p.generationCapacityW,
        demand: acc.demand + p.consumptionDemandW,
        consumption: acc.consumption + p.actualConsumptionW,
        networks: acc.networks + p.networkCount
    }), { generation: 0, demand: 0, consumption: 0, networks: 0 });

    $: clusterSatisfaction = clusterTotals.demand > 0
        ? (clusterTotals.generation / clusterTotals.demand) * 100
        : 100;

    function formatWatts(watts: number): { value: string; unit: string } {
        if (watts >= 1e12) return { value: (watts / 1e12).toFixed(2), unit: 'TW' };
        if (watts >= 1e9) return { value: (watts / 1e9).toFixed(2), unit: 'GW' };
        if (watts >= 1e6) return { value: (watts / 1e6).toFixed(2), unit: 'MW' };
        if (watts >= 1e3) return { value: (watts / 1e3).toFixed(2), unit: 'kW' };
        return { value: watts.toFixed(0), unit: 'W' };
    }

    function formatWattsString(watts: number): string {
        const f = formatWatts(watts);
        return `${f.value} ${f.unit}`;
    }

    function getSatisfactionColor(percent: number): 'green' | 'orange' | 'red' {
        if (percent >= 100) return 'green';
        if (percent >= 80) return 'orange';
        return 'red';
    }
</script>

<div class="power-page">
    <!-- Page Header -->
    <div class="page-header">
        <div class="header-left">
            <h1 class="page-title">
                <span class="title-icon">⚡</span>
                Power Grid Status
            </h1>
            <p class="page-subtitle">Real-time power data across {activePlanets.length} planetary grids</p>
        </div>
        <div class="header-filters">
            <select class="filter-select" bind:value={selectedStar}>
                <option value="all">All Stars ({activePlanets.length} planets)</option>
                {#each starOptions as star}
                    <option value={star.name}>{star.name} ({star.count})</option>
                {/each}
            </select>
        </div>
    </div>

    {#if activePlanets.length === 0}
        <div class="empty-state">
            <div class="empty-icon">⚡</div>
            <div class="empty-text">No active power networks detected</div>
        </div>
    {:else}
        <!-- Cluster Overview Section -->
        <section class="cluster-overview">
            <div class="overview-grid">
                <!-- Main Metrics Card -->
                <div class="overview-card metrics-card">
                    <div class="card-header">
                        <span class="header-icon">⚡</span>
                        <h2>Real-time Power Data</h2>
                    </div>
                    <div class="metrics-row">
                        <div class="metric">
                            <div class="metric-label">Generation Capacity</div>
                            <div class="metric-value cyan">
                                {formatWatts(clusterTotals.generation).value}<span class="unit">{formatWatts(clusterTotals.generation).unit}</span>
                            </div>
                        </div>
                        <div class="metric">
                            <div class="metric-label">Consumption Demand</div>
                            <div class="metric-value orange">
                                {formatWatts(clusterTotals.demand).value}<span class="unit">{formatWatts(clusterTotals.demand).unit}</span>
                            </div>
                        </div>
                    </div>
                    <div class="power-bar">
                        <div
                            class="power-bar-fill"
                            style="width: {Math.min(100, (clusterTotals.demand / clusterTotals.generation) * 100)}%"
                        ></div>
                    </div>
                    <div class="metrics-row secondary">
                        <div class="metric-small">
                            <span class="label">Actual Consumption:</span>
                            <span class="value">{formatWattsString(clusterTotals.consumption)}</span>
                        </div>
                        <div class="metric-small">
                            <span class="label">Networks:</span>
                            <span class="value">{clusterTotals.networks}</span>
                        </div>
                    </div>
                </div>

                <!-- Gauges Section -->
                <div class="overview-card gauges-card">
                    <div class="gauges-row">
                        <CircularGauge
                            value={Math.min(clusterSatisfaction, 999)}
                            max={100}
                            label="Sufficiency"
                            size="lg"
                            color={getSatisfactionColor(clusterSatisfaction)}
                        />
                        <div class="gauge-secondary">
                            <CircularGauge
                                value={parseFloat(formatWatts(clusterTotals.generation).value)}
                                max={parseFloat(formatWatts(clusterTotals.generation).value)}
                                label="Generation"
                                size="md"
                                color="cyan"
                                unit={formatWatts(clusterTotals.generation).unit}
                            />
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <!-- Planetary Grids Section -->
        <section class="grids-section">
            <h2 class="section-title">
                Planetary Power Grids
                {#if selectedStar !== 'all'}
                    <span class="filter-indicator">- {selectedStar}</span>
                {/if}
            </h2>
            <div class="grids-list">
                {#each filteredPlanets as planet}
                    <div class="grid-card" class:warning={planet.satisfactionPercent < 100}>
                        <div class="grid-header">
                            <div class="planet-info">
                                <div class="planet-name">{planet.planetName}</div>
                                <div class="star-name">{planet.starName}</div>
                            </div>
                        </div>

                        <div class="grid-body">
                            <div class="grid-stats">
                                <div class="stat">
                                    <span class="stat-label">Generation</span>
                                    <span class="stat-value cyan">{formatWattsString(planet.generationCapacityW)}</span>
                                </div>
                                <div class="stat">
                                    <span class="stat-label">Demand</span>
                                    <span class="stat-value orange">{formatWattsString(planet.consumptionDemandW)}</span>
                                </div>
                                <div class="stat">
                                    <span class="stat-label">Networks</span>
                                    <span class="stat-value">{planet.networkCount}</span>
                                </div>
                            </div>

                            <div class="grid-gauge-section">
                                <CircularGauge
                                    value={Math.min(planet.satisfactionPercent, 999)}
                                    max={100}
                                    size="sm"
                                    color={getSatisfactionColor(planet.satisfactionPercent)}
                                    unit="%"
                                />
                                {#if planet.generatorTypes && planet.generatorTypes.length > 0}
                                    <div class="generator-icons">
                                        {#each planet.generatorTypes.sort((a, b) => b.totalPowerW - a.totalPowerW).slice(0, 4) as genType}
                                            <img
                                                class="generator-icon"
                                                src={getGeneratorIcon(genType.type)}
                                                alt={getGeneratorName(genType.type)}
                                                title={`${getGeneratorName(genType.type)}: ${formatWattsString(genType.totalPowerW)}`}
                                            />
                                        {/each}
                                    </div>
                                {/if}
                            </div>
                        </div>

                        {#if planet.satisfactionPercent < 100}
                            <div class="warning-banner">
                                <span class="warning-icon">⚠</span>
                                Power Deficit
                            </div>
                        {/if}
                    </div>
                {/each}
            </div>
        </section>
    {/if}
</div>

<style>
    .power-page {
        padding: 1.5rem;
        min-height: 100%;
        background-color: var(--bg-primary);
        color: var(--text-primary);
    }

    /* Page Header */
    .page-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        margin-bottom: 1.5rem;
    }

    .page-title {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        font-size: 1.5rem;
        font-weight: 700;
        color: var(--accent-cyan);
        margin: 0;
    }

    .title-icon {
        font-size: 1.25rem;
    }

    .page-subtitle {
        font-size: 0.875rem;
        color: var(--text-muted);
        margin: 0.25rem 0 0 0;
    }

    .filter-select {
        background: var(--bg-secondary);
        border: 1px solid var(--border-subtle);
        border-radius: var(--radius-sm);
        padding: 0.5rem 1rem;
        color: var(--text-primary);
        font-size: 0.875rem;
    }

    /* Empty State */
    .empty-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        height: 300px;
        background: var(--bg-secondary);
        border: 1px solid var(--border-subtle);
        border-radius: var(--radius-md);
    }

    .empty-icon {
        font-size: 3rem;
        opacity: 0.3;
        margin-bottom: 1rem;
    }

    .empty-text {
        color: var(--text-muted);
    }

    /* Cluster Overview */
    .cluster-overview {
        margin-bottom: 2rem;
    }

    .overview-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 1.5rem;
    }

    .overview-card {
        background: var(--bg-card);
        border: 1px solid var(--border-subtle);
        border-radius: var(--radius-md);
        padding: 1.25rem;
    }

    .card-header {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding-bottom: 1rem;
        border-bottom: 1px solid var(--border-subtle);
        margin-bottom: 1rem;
    }

    .card-header h2 {
        font-size: 0.875rem;
        font-weight: 500;
        color: var(--text-secondary);
        margin: 0;
    }

    .header-icon {
        font-size: 1rem;
    }

    .metrics-row {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 1.5rem;
        margin-bottom: 1rem;
    }

    .metrics-row.secondary {
        margin-bottom: 0;
        gap: 1rem;
    }

    .metric-label {
        font-size: 0.75rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: var(--text-muted);
        margin-bottom: 0.25rem;
    }

    .metric-value {
        font-size: 1.75rem;
        font-weight: 700;
        font-variant-numeric: tabular-nums;
    }

    .metric-value.cyan { color: var(--accent-cyan); }
    .metric-value.orange { color: var(--accent-orange); }

    .metric-value .unit {
        font-size: 0.875rem;
        font-weight: 400;
        color: var(--text-muted);
        margin-left: 0.25rem;
    }

    .metric-small {
        display: flex;
        justify-content: space-between;
        font-size: 0.75rem;
    }

    .metric-small .label {
        color: var(--text-muted);
    }

    .metric-small .value {
        color: var(--text-secondary);
        font-variant-numeric: tabular-nums;
    }

    .power-bar {
        height: 24px;
        background: linear-gradient(90deg, var(--accent-cyan-dim) 0%, var(--accent-cyan) 100%);
        border-radius: var(--radius-sm);
        margin-bottom: 1rem;
        position: relative;
        overflow: hidden;
    }

    .power-bar-fill {
        position: absolute;
        right: 0;
        top: 0;
        height: 100%;
        background: linear-gradient(90deg, var(--accent-orange-dim), var(--accent-orange));
        transition: width 0.5s ease;
    }

    /* Gauges */
    .gauges-card {
        display: flex;
        align-items: center;
        justify-content: center;
    }

    .gauges-row {
        display: flex;
        align-items: center;
        gap: 2rem;
    }

    .gauge-secondary {
        display: flex;
        flex-direction: column;
        gap: 1rem;
    }

    /* Section Title */
    .section-title {
        font-size: 1rem;
        font-weight: 600;
        color: var(--text-secondary);
        margin: 0 0 1rem 0;
    }

    .filter-indicator {
        font-weight: 400;
        color: var(--accent-cyan);
    }

    /* Grids List */
    .grids-list {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
        gap: 1rem;
    }

    .grid-card {
        background: var(--bg-card);
        border: 1px solid var(--border-subtle);
        border-radius: var(--radius-md);
        overflow: hidden;
        transition: border-color var(--transition-fast);
    }

    .grid-card:hover {
        border-color: var(--border-accent);
    }

    .grid-card.warning {
        border-color: rgba(239, 68, 68, 0.3);
    }

    .grid-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 0.75rem 1rem;
        background: var(--bg-secondary);
        border-bottom: 1px solid var(--border-subtle);
    }

    .planet-name {
        font-weight: 600;
        color: var(--text-primary);
    }

    .star-name {
        font-size: 0.625rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: var(--text-muted);
    }

    .grid-body {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 1rem;
        gap: 1rem;
    }

    .grid-stats {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        flex: 1;
    }

    .stat {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 0.5rem;
    }

    .stat-label {
        font-size: 0.625rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: var(--text-muted);
    }

    .stat-value {
        font-size: 0.875rem;
        font-weight: 600;
        font-variant-numeric: tabular-nums;
        color: var(--text-primary);
    }

    .stat-value.cyan { color: var(--accent-cyan); }
    .stat-value.orange { color: var(--accent-orange); }

    .grid-gauge-section {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 0.5rem;
    }

    .generator-icons {
        display: flex;
        gap: 2px;
        flex-wrap: wrap;
        justify-content: center;
        max-width: 100px;
    }

    .generator-icon {
        width: 28px;
        height: 28px;
        object-fit: contain;
        cursor: help;
        filter: drop-shadow(0 1px 2px rgba(0, 0, 0, 0.5));
    }

    .warning-banner {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 0.5rem;
        padding: 0.5rem;
        background: rgba(239, 68, 68, 0.1);
        border-top: 1px solid rgba(239, 68, 68, 0.2);
        color: var(--accent-red);
        font-size: 0.75rem;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        animation: pulse-warning 2s ease-in-out infinite;
    }

    @keyframes pulse-warning {
        0%, 100% { opacity: 1; }
        50% { opacity: 0.6; }
    }

    @media (max-width: 768px) {
        .overview-grid {
            grid-template-columns: 1fr;
        }

        .gauges-row {
            flex-direction: column;
        }
    }
</style>
