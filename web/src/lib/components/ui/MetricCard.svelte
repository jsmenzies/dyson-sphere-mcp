<script lang="ts">
  export let title: string = '';
  export let icon: string = '';
  export let metrics: Array<{
    label: string;
    value: number | string;
    unit?: string;
    color?: 'cyan' | 'orange' | 'green' | 'red' | 'white';
  }> = [];

  const colorClasses: Record<string, string> = {
    cyan: 'text-cyan',
    orange: 'text-orange',
    green: 'text-green',
    red: 'text-red',
    white: 'text-white'
  };

  function formatValue(val: number | string): string {
    if (typeof val === 'string') return val;
    if (val >= 1_000_000_000) return (val / 1_000_000_000).toFixed(2);
    if (val >= 1_000_000) return (val / 1_000_000).toFixed(2);
    if (val >= 1_000) return (val / 1_000).toFixed(2);
    return val.toFixed(2);
  }

  function getUnit(val: number | string, unit?: string): string {
    if (!unit) return '';
    if (typeof val === 'string') return unit;
    if (val >= 1_000_000_000) return 'G' + unit;
    if (val >= 1_000_000) return 'M' + unit;
    if (val >= 1_000) return 'k' + unit;
    return unit;
  }
</script>

<div class="metric-card">
  {#if title || icon}
    <div class="card-header">
      {#if icon}
        <span class="card-icon">{icon}</span>
      {/if}
      {#if title}
        <h3 class="card-title">{title}</h3>
      {/if}
    </div>
  {/if}

  <div class="metrics-grid" class:single={metrics.length === 1}>
    {#each metrics as metric}
      <div class="metric-item">
        <div class="metric-label">{metric.label}</div>
        <div class="metric-value {colorClasses[metric.color || 'white']}">
          {formatValue(metric.value)}<span class="metric-unit">{getUnit(metric.value, metric.unit)}</span>
        </div>
      </div>
    {/each}
  </div>
</div>

<style>
  .metric-card {
    background: rgba(13, 13, 26, 0.95);
    border: 1px solid rgba(255, 255, 255, 0.1);
    border-radius: 8px;
    padding: 1rem 1.25rem;
  }

  .card-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 1rem;
    padding-bottom: 0.75rem;
    border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  }

  .card-icon {
    font-size: 1.25rem;
  }

  .card-title {
    font-size: 0.875rem;
    font-weight: 500;
    color: #a0a0b0;
    margin: 0;
  }

  .metrics-grid {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 1rem;
  }

  .metrics-grid.single {
    grid-template-columns: 1fr;
  }

  .metric-item {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .metric-label {
    font-size: 0.75rem;
    font-weight: 500;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: #6b7280;
  }

  .metric-value {
    font-size: 1.5rem;
    font-weight: 700;
    font-variant-numeric: tabular-nums;
    line-height: 1.2;
  }

  .metric-unit {
    font-size: 0.75rem;
    font-weight: 400;
    color: #6b7280;
    margin-left: 0.25rem;
  }

  /* Color classes */
  .text-cyan { color: #00d4ff; }
  .text-orange { color: #ff9500; }
  .text-green { color: #22c55e; }
  .text-red { color: #ef4444; }
  .text-white { color: #ffffff; }
</style>
