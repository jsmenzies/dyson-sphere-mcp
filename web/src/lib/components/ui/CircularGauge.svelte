<script lang="ts">
  export let value: number = 0;
  export let max: number = 100;
  export let label: string = '';
  export let size: 'sm' | 'md' | 'lg' = 'md';
  export let color: 'cyan' | 'orange' | 'green' | 'red' | string = 'cyan';
  export let showValue: boolean = true;
  export let unit: string = '%';
  export let thickness: number = 8;

  $: percentage = Math.min(100, Math.max(0, (value / max) * 100));
  $: circumference = 2 * Math.PI * 45;
  $: dashOffset = circumference - (percentage / 100) * circumference;

  const sizes = {
    sm: { container: 80, font: 'text-lg', labelFont: 'text-xs' },
    md: { container: 140, font: 'text-3xl', labelFont: 'text-sm' },
    lg: { container: 200, font: 'text-4xl', labelFont: 'text-base' }
  };

  const colors = {
    cyan: { stroke: '#00d4ff', glow: 'rgba(0, 212, 255, 0.4)' },
    orange: { stroke: '#ff9500', glow: 'rgba(255, 149, 0, 0.4)' },
    green: { stroke: '#22c55e', glow: 'rgba(34, 197, 94, 0.4)' },
    red: { stroke: '#ef4444', glow: 'rgba(239, 68, 68, 0.4)' }
  };

  $: currentSize = sizes[size];
  $: currentColor = (() => {
    if (color in colors) {
      return colors[color as keyof typeof colors];
    }
    // Custom color string (e.g., "rgb(100, 200, 100)")
    return { stroke: color, glow: color.replace('rgb', 'rgba').replace(')', ', 0.4)') };
  })();
</script>

<div
  class="circular-gauge"
  style="width: {currentSize.container}px; height: {currentSize.container}px;"
>
  <svg viewBox="0 0 100 100" class="gauge-svg">
    <!-- Background ring -->
    <circle
      cx="50"
      cy="50"
      r="45"
      fill="none"
      stroke="#252538"
      stroke-width={thickness}
    />

    <!-- Progress ring -->
    <circle
      cx="50"
      cy="50"
      r="45"
      fill="none"
      stroke={currentColor.stroke}
      stroke-width={thickness}
      stroke-linecap="round"
      stroke-dasharray={circumference}
      stroke-dashoffset={dashOffset}
      transform="rotate(-90 50 50)"
      class="progress-ring"
      style="filter: drop-shadow(0 0 6px {currentColor.glow});"
    />
  </svg>

  <div class="gauge-content">
    {#if showValue}
      <div class="gauge-value {currentSize.font}" style="color: {currentColor.stroke};">
        {value.toFixed(1)}<span class="gauge-unit">{unit}</span>
      </div>
    {/if}
    {#if label}
      <div class="gauge-label {currentSize.labelFont}">
        {label}
      </div>
    {/if}
  </div>
</div>

<style>
  .circular-gauge {
    position: relative;
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .gauge-svg {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
  }

  .progress-ring {
    transition: stroke-dashoffset 0.5s ease;
  }

  .gauge-content {
    position: relative;
    z-index: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    text-align: center;
  }

  .gauge-value {
    font-weight: 700;
    font-variant-numeric: tabular-nums;
    line-height: 1;
  }

  .gauge-unit {
    font-size: 0.5em;
    font-weight: 400;
    opacity: 0.8;
    margin-left: 2px;
  }

  .gauge-label {
    color: #a0a0b0;
    margin-top: 4px;
    text-transform: capitalize;
  }

  /* Tailwind-like text sizes */
  .text-lg { font-size: 1.125rem; }
  .text-3xl { font-size: 1.875rem; }
  .text-4xl { font-size: 2.25rem; }
  .text-xs { font-size: 0.75rem; }
  .text-sm { font-size: 0.875rem; }
  .text-base { font-size: 1rem; }
</style>
