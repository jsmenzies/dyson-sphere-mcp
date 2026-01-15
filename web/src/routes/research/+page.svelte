<script lang="ts">
    import type { PageData } from './$types';
    import techIcons from '$lib/data/tech-icons.json';
    import { onMount, onDestroy } from 'svelte';

    export let data: PageData;

    // Auto-refresh state
    let autoRefresh = true;
    let refreshInterval: ReturnType<typeof setInterval> | null = null;

    // Local reactive data that can be updated by refresh
    let research = data.research;
    let queue = data.queue;
    let upgrades = data.upgrades;
    let byPlanet = data.byPlanet;

    async function refreshData() {
        try {
            const [researchRes, queueRes, upgradesRes, byPlanetRes] = await Promise.all([
                fetch('/api/research'),
                fetch('/api/research/tech-queue'),
                fetch('/api/research/upgrades'),
                fetch('/api/research/by-planet')
            ]);

            if (researchRes.ok) research = await researchRes.json();
            if (queueRes.ok) queue = (await queueRes.json()).queue;
            if (upgradesRes.ok) upgrades = await upgradesRes.json();
            if (byPlanetRes.ok) byPlanet = await byPlanetRes.json();
        } catch (e) {
            console.error("Refresh error:", e);
        }
    }

    function startRefresh() {
        if (refreshInterval) clearInterval(refreshInterval);
        refreshInterval = setInterval(refreshData, 5000);
    }

    function stopRefresh() {
        if (refreshInterval) {
            clearInterval(refreshInterval);
            refreshInterval = null;
        }
    }

    $: if (autoRefresh) {
        startRefresh();
    } else {
        stopRefresh();
    }

    onMount(() => {
        if (autoRefresh) startRefresh();
    });

    onDestroy(() => {
        stopRefresh();
    });

    function formatHash(hash: number): { value: string; suffix: string } {
        if (hash >= 1e9) return { value: (hash / 1e9).toFixed(2), suffix: 'G' };
        if (hash >= 1e6) return { value: (hash / 1e6).toFixed(2), suffix: 'M' };
        if (hash >= 1e3) return { value: (hash / 1e3).toFixed(2), suffix: 'k' };
        return { value: hash.toFixed(0), suffix: '' };
    }

    function formatHashString(hash: number): string {
        const parts = formatHash(hash);
        return `${parts.value} ${parts.suffix}`.trim();
    }

    function getPerformanceColors(percent: number): { main: string; dim: string } {
        if (percent >= 90) return { main: 'var(--accent-cyan)', dim: 'var(--accent-cyan-dim)' };
        if (percent >= 60) return { main: 'var(--accent-green)', dim: 'rgba(34, 197, 94, 0.3)' };
        if (percent >= 30) return { main: 'var(--accent-orange)', dim: 'var(--accent-orange-dim)' };
        return { main: 'var(--accent-red)', dim: 'rgba(239, 68, 68, 0.3)' };
    }

    $: planetBreakdown = byPlanet?.planets
        ? byPlanet.planets
            .filter((p: any) => p.hashPerSecond > 0)
            .map((p: any) => {
                const techSpeed = upgrades?.research?.techSpeed || 1;
                // Formula: labCount * techSpeed * 60 ticks/sec
                const theoreticalMax = p.labCount * techSpeed * 60.0;
                return {
                    ...p,
                    theoreticalMaxHashPerSecond: theoreticalMax
                };
            })
            .sort((a: any, b: any) => b.hashPerSecond - a.hashPerSecond)
        : [];

    function getTechIcon(techName: string): string | null {
        // Direct match
        const entry = techIcons[techName as keyof typeof techIcons];
        if (entry?.icon) return entry.icon;

        // Try to match infinite research by falling back to highest available level
        const levelMatch = techName.match(/^(.+) \(Lv(\d+)\)$/);
        if (levelMatch) {
            const baseName = levelMatch[1];
            let level = parseInt(levelMatch[2]);

            // Try decrementing levels until we find an icon
            while (level > 0) {
                const fallbackName = `${baseName} (Lv${level})`;
                const fallbackEntry = techIcons[fallbackName as keyof typeof techIcons];
                if (fallbackEntry?.icon) return fallbackEntry.icon;
                level--;
            }
        }

        // For techs without level suffix, find the highest level icon available
        const allKeys = Object.keys(techIcons);
        const matchingKeys = allKeys
            .filter(key => key.startsWith(techName + ' (Lv'))
            .sort((a, b) => {
                const levelA = parseInt(a.match(/\(Lv(\d+)\)$/)?.[1] || '0');
                const levelB = parseInt(b.match(/\(Lv(\d+)\)$/)?.[1] || '0');
                return levelB - levelA;
            });

        if (matchingKeys.length > 0) {
            const matchEntry = techIcons[matchingKeys[0] as keyof typeof techIcons];
            if (matchEntry?.icon) return matchEntry.icon;
        }

        return null;
    }
</script>

<div class="research-page">
    <!-- Page Header -->
    <div class="page-header">
        <div class="header-left">
            <h1 class="page-title">
                <span class="title-icon">🔬</span>
                Research & Technology
            </h1>
            <p class="page-subtitle">Cluster-wide scientific progress</p>
        </div>
        <div class="header-controls">
            <label class="refresh-toggle">
                <input type="checkbox" bind:checked={autoRefresh} />
                <span class="toggle-label">Auto-refresh</span>
                <span class="toggle-indicator" class:active={autoRefresh}></span>
            </label>
        </div>
    </div>

    <div class="content-grid">
        <!-- Current Research & Queue Column -->
        <div class="main-column">
            <!-- Current Research Card -->
            <div class="overview-card">
                <div class="card-header">
                    <span class="header-icon">⚗️</span>
                    <h2>Current Research</h2>
                    {#if research?.totalHashPerSecond}
                        <div class="hash-rate">
                            <span class="value">{formatHash(research.totalHashPerSecond).value}</span>
                            <span class="suffix">{formatHash(research.totalHashPerSecond).suffix}</span>
                            <span class="unit">Hash/s</span>
                        </div>
                    {/if}
                </div>

                {#if research?.currentTech}
                    <div class="current-research">
                        <div class="research-info">
                            <div class="tech-icon-wrapper">
                                {#if getTechIcon(research.currentTech.name)}
                                    <img
                                        src={getTechIcon(research.currentTech.name)}
                                        alt={research.currentTech.name}
                                        class="tech-icon"
                                    />
                                {:else}
                                    <div class="tech-icon-placeholder">?</div>
                                {/if}
                            </div>
                            <div class="tech-details">
                                <div class="tech-name">{research.currentTech.name}</div>
                                <div class="tech-id">ID: {research.currentTech.id}</div>
                            </div>
                            <div class="progress-info">
                                <div class="progress-percent">{research.currentTech.progressPercent.toFixed(2)}%</div>
                                <div class="progress-hashes">
                                    {formatHashString(research.currentTech.hashUploaded)} / {formatHashString(research.currentTech.hashNeeded)} Hashes
                                </div>
                            </div>
                        </div>

                        <div class="progress-bar-container">
                            <div class="progress-bar">
                                <div
                                    class="progress-bar-fill"
                                    style="width: {research.currentTech.progressPercent}%"
                                >
                                    <div class="progress-bar-pulse"></div>
                                </div>
                            </div>
                        </div>

                        <!-- Research by Planet Breakdown -->
                        {#if planetBreakdown.length > 0}
                            <div class="research-breakdown">
                                <div class="breakdown-label">Research by Planet</div>
                                <div class="breakdown-list">
                                    {#each planetBreakdown as planet}
                                        <div class="breakdown-row">
                                            <div class="breakdown-name" title="{planet.planetName} ({planet.starName})">
                                                {planet.planetName}
                                            </div>
                                            <div class="breakdown-bar-container">
                                                <div
                                                    class="breakdown-bar-fill"
                                                    style="
                                                        width: {Math.min(100, (planet.hashPerSecond / (planet.theoreticalMaxHashPerSecond || 1)) * 100)}%;
                                                        background: linear-gradient(90deg, {getPerformanceColors((planet.hashPerSecond / (planet.theoreticalMaxHashPerSecond || 1)) * 100).dim} 0%, {getPerformanceColors((planet.hashPerSecond / (planet.theoreticalMaxHashPerSecond || 1)) * 100).main} 100%);
                                                    "
                                                >
                                                    <div class="progress-bar-pulse"></div>
                                                </div>
                                            </div>
                                            <div class="breakdown-value">
                                                <span class="value">{formatHash(planet.hashPerSecond).value}</span>
                                                <span class="suffix">{formatHash(planet.hashPerSecond).suffix}</span>
                                                <span class="unit">Hash/s</span>
                                            </div>
                                        </div>
                                    {/each}
                                </div>
                            </div>
                        {/if}

                        <div class="research-stats">
                            <span class="stat">Labs Active: <strong>{research.totalLabCount}</strong></span>
                            <span class="stat">Est. Time: <strong>{((research.currentTech.hashNeeded - research.currentTech.hashUploaded) / (research.totalHashPerSecond || 1) / 60).toFixed(1)}m</strong> remaining</span>
                        </div>
                    </div>
                {:else}
                    <div class="empty-state">
                        <div class="empty-text">No active research</div>
                    </div>
                {/if}
            </div>

            <!-- Technology Queue Card -->
            <div class="overview-card queue-card">
                <div class="card-header">
                    <span class="header-icon">📋</span>
                    <h2>Technology Queue</h2>
                    <span class="queue-count">{queue.length > 2 ? queue.length - 2 : 0} queued</span>
                </div>

                <div class="queue-list">
                    {#each queue.slice(2) as item}
                        <div class="queue-item">
                            <div class="queue-position">#{item.position}</div>
                            <div class="queue-icon-wrapper">
                                {#if getTechIcon(item.name)}
                                    <img
                                        src={getTechIcon(item.name)}
                                        alt={item.name}
                                        class="queue-icon"
                                    />
                                {:else}
                                    <div class="queue-icon-placeholder">?</div>
                                {/if}
                            </div>
                            <div class="queue-name">{item.name}</div>
                            <div class="queue-hashes">{formatHashString(item.hashNeeded)} Hashes</div>
                        </div>
                    {/each}
                    {#if queue.length <= 2}
                        <div class="empty-state small">
                            <div class="empty-text">Queue is empty</div>
                        </div>
                    {/if}
                </div>
            </div>
        </div>

        <!-- Sidebar Column -->
        <div class="sidebar-column">
            <!-- Mecha Status Card -->
            <div class="overview-card">
                <div class="card-header">
                    <span class="header-icon">🤖</span>
                    <h2>Mecha Status</h2>
                </div>

                {#if upgrades?.mecha}
                    <div class="stats-list">
                        <div class="stat-row">
                            <span class="stat-label">Core Energy</span>
                            <span class="stat-value">{(upgrades.mecha.coreEnergyCap / 1e6).toFixed(0)} MJ</span>
                        </div>
                        <div class="stat-row">
                            <span class="stat-label">Walk Speed</span>
                            <span class="stat-value">{upgrades.mecha.walkSpeed} m/s</span>
                        </div>
                        <div class="stat-row">
                            <span class="stat-label">Warp Speed</span>
                            <span class="stat-value">{(upgrades.mecha.maxWarpSpeed / 40000).toFixed(2)} LY/s</span>
                        </div>
                    </div>
                {:else}
                    <div class="empty-state small">
                        <div class="empty-text">No data available</div>
                    </div>
                {/if}
            </div>

            <!-- Logistics Level Card -->
            <div class="overview-card">
                <div class="card-header">
                    <span class="header-icon">🚀</span>
                    <h2>Logistics Level</h2>
                </div>

                {#if upgrades?.logisticsCapacity}
                    <div class="stats-list">
                        <div class="stat-row">
                            <span class="stat-label">Drone Capacity</span>
                            <span class="stat-value">{upgrades.logisticsCapacity.stationDroneCount}</span>
                        </div>
                        <div class="stat-row">
                            <span class="stat-label">Ship Capacity</span>
                            <span class="stat-value">{upgrades.logisticsCapacity.stationShipCount}</span>
                        </div>
                        <div class="stat-row">
                            <span class="stat-label">Tech Speed</span>
                            <span class="stat-value cyan">+{((upgrades.research.techSpeed - 1) * 100).toFixed(0)}%</span>
                        </div>
                    </div>
                {:else}
                    <div class="empty-state small">
                        <div class="empty-text">No data available</div>
                    </div>
                {/if}
            </div>
        </div>
    </div>
</div>

<style>
    .research-page {
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

    /* Refresh Toggle */
    .refresh-toggle {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        cursor: pointer;
        background: var(--bg-secondary);
        border: 1px solid var(--border-subtle);
        border-radius: var(--radius-sm);
        padding: 0.5rem 0.75rem;
        font-size: 0.75rem;
        color: var(--text-secondary);
    }

    .refresh-toggle input {
        display: none;
    }

    .toggle-indicator {
        width: 8px;
        height: 8px;
        border-radius: 50%;
        background: var(--text-muted);
        transition: background 0.2s ease;
    }

    .toggle-indicator.active {
        background: var(--accent-green);
        box-shadow: 0 0 6px var(--accent-green);
    }

    /* Content Grid */
    .content-grid {
        display: grid;
        grid-template-columns: 2fr 1fr;
        gap: 1.5rem;
    }

    .main-column {
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
    }

    .sidebar-column {
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
    }

    /* Cards */
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
        flex: 1;
    }

    .header-icon {
        font-size: 1rem;
    }

    .hash-rate {
        display: flex;
        align-items: baseline;
        gap: 0.25rem;
        font-family: var(--font-mono);
        color: var(--accent-cyan);
    }

    .hash-rate .value {
        font-size: 1.25rem;
        font-weight: 700;
    }

    .hash-rate .suffix {
        font-size: 1rem;
        font-weight: 600;
    }

    .hash-rate .unit {
        font-size: 0.75rem;
        font-weight: 400;
        color: var(--text-muted);
        margin-left: 0.125rem;
    }

    .queue-count {
        font-size: 0.75rem;
        color: var(--text-muted);
    }

    /* Current Research */
    .current-research {
        display: flex;
        flex-direction: column;
        gap: 1rem;
    }

    .research-info {
        display: flex;
        align-items: center;
        gap: 1rem;
    }

    .tech-icon-wrapper {
        flex-shrink: 0;
    }

    .tech-icon {
        width: 56px;
        height: 56px;
        border-radius: var(--radius-sm);
        border: 1px solid var(--border-subtle);
        object-fit: contain;
        background: var(--bg-secondary);
    }

    .tech-icon-placeholder {
        width: 56px;
        height: 56px;
        border-radius: var(--radius-sm);
        border: 1px solid var(--border-subtle);
        background: var(--bg-secondary);
        display: flex;
        align-items: center;
        justify-content: center;
        color: var(--text-muted);
        font-size: 1.25rem;
    }

    .tech-details {
        flex: 1;
    }

    .tech-name {
        font-size: 1.25rem;
        font-weight: 700;
        color: var(--text-primary);
    }

    .tech-id {
        font-size: 0.625rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: var(--text-muted);
        margin-top: 0.125rem;
    }

    .progress-info {
        text-align: right;
    }

    .progress-percent {
        font-size: 1.25rem;
        font-weight: 700;
        font-family: var(--font-mono);
        color: var(--accent-cyan);
    }

    .progress-hashes {
        font-size: 0.625rem;
        color: var(--text-muted);
        margin-top: 0.125rem;
    }

    /* Progress Bar */
    .progress-bar-container {
        position: relative;
    }

    .progress-bar {
        height: 20px;
        background: var(--bg-secondary);
        border-radius: var(--radius-sm);
        border: 1px solid var(--border-subtle);
        overflow: hidden;
        position: relative;
    }

    .progress-bar-fill {
        position: absolute;
        top: 0;
        left: 0;
        height: 100%;
        background: linear-gradient(90deg, var(--accent-cyan-dim) 0%, var(--accent-cyan) 100%);
        transition: width 0.5s ease;
        overflow: hidden;
        border-radius: var(--radius-sm);
    }

    .progress-bar-pulse {
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: linear-gradient(
            90deg,
            transparent 0%,
            rgba(255, 255, 255, 0.3) 50%,
            transparent 100%
        );
        animation: pulse-sweep 2s ease-in-out infinite;
    }

    @keyframes pulse-sweep {
        0% {
            transform: translateX(-100%);
        }
        100% {
            transform: translateX(100%);
        }
    }

    .research-stats {
        display: flex;
        justify-content: space-between;
        font-size: 0.75rem;
        color: var(--text-muted);
    }

    .research-stats strong {
        color: var(--text-secondary);
    }

    /* Research Breakdown */
    .research-breakdown {
        margin: 0.5rem 0 0.5rem 0;
        padding-top: 1rem;
        border-top: 1px dashed var(--border-subtle);
    }

    .breakdown-label {
        font-size: 0.625rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: var(--text-muted);
        margin-bottom: 0.5rem;
    }

    .breakdown-list {
        display: flex;
        flex-direction: column;
        gap: 0.375rem;
    }

    .breakdown-row {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        font-size: 0.75rem;
    }

    .breakdown-name {
        width: 120px;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
        color: var(--text-secondary);
    }

    .breakdown-bar-container {
        flex: 1;
        height: 12px;
        background: var(--bg-secondary);
        border-radius: var(--radius-sm);
        overflow: hidden;
        border: 1px solid var(--border-subtle);
    }

    .breakdown-bar-fill {
        position: relative;
        height: 100%;
        border-radius: var(--radius-sm);
        transition: width 0.3s ease, background 0.3s ease;
        overflow: hidden;
    }

    .breakdown-value {
        width: 100px;
        text-align: right;
        font-family: var(--font-mono);
        color: var(--accent-cyan);
        display: flex;
        align-items: baseline;
        justify-content: flex-end;
        gap: 0.125rem;
    }

    .breakdown-value .suffix {
        font-size: 0.75rem;
        font-weight: 600;
    }

    .breakdown-value .unit {
        font-size: 0.625rem;
        font-weight: 400;
        color: var(--text-muted);
        margin-left: 0.125rem;
    }

    /* Queue */
    .queue-card {
        padding-bottom: 0;
    }

    .queue-card .card-header {
        margin-bottom: 0;
    }

    .queue-list {
        max-height: 400px;
        overflow-y: auto;
    }

    .queue-item {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        padding: 0.875rem 0;
        border-bottom: 1px solid var(--border-subtle);
        transition: background 0.15s ease;
    }

    .queue-item:hover {
        background: rgba(255, 255, 255, 0.02);
    }

    .queue-item:last-child {
        border-bottom: none;
    }

    .queue-position {
        font-size: 0.625rem;
        font-family: var(--font-mono);
        color: var(--text-muted);
        width: 1.5rem;
        text-align: center;
    }

    .queue-icon-wrapper {
        flex-shrink: 0;
    }

    .queue-icon {
        width: 32px;
        height: 32px;
        border-radius: var(--radius-sm);
        border: 1px solid var(--border-subtle);
        object-fit: contain;
        background: var(--bg-secondary);
    }

    .queue-icon-placeholder {
        width: 32px;
        height: 32px;
        border-radius: var(--radius-sm);
        border: 1px solid var(--border-subtle);
        background: var(--bg-secondary);
        display: flex;
        align-items: center;
        justify-content: center;
        color: var(--text-muted);
        font-size: 0.75rem;
    }

    .queue-name {
        flex: 1;
        font-size: 0.875rem;
        font-weight: 500;
        color: var(--text-primary);
    }

    .queue-hashes {
        font-size: 0.75rem;
        font-family: var(--font-mono);
        color: var(--text-muted);
    }

    /* Stats List */
    .stats-list {
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
    }

    .stat-row {
        display: flex;
        justify-content: space-between;
        align-items: center;
    }

    .stat-label {
        font-size: 0.75rem;
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

    .stat-value.cyan {
        color: var(--accent-cyan);
    }

    /* Empty State */
    .empty-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: 3rem;
    }

    .empty-state.small {
        padding: 1.5rem;
    }

    .empty-text {
        color: var(--text-muted);
        font-style: italic;
        font-size: 0.875rem;
    }

    /* Responsive */
    @media (max-width: 768px) {
        .content-grid {
            grid-template-columns: 1fr;
        }

        .research-info {
            flex-wrap: wrap;
        }

        .progress-info {
            width: 100%;
            text-align: left;
            margin-top: 0.5rem;
        }
    }
</style>
