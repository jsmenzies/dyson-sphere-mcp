# DSP Manager Frontend - Product Requirements Document

## Overview

Build an enhanced web frontend for Dyson Sphere Program that visualizes game data in an easy-to-digest manner, with
focus on **Item Flow Tracking**, **Bottleneck Detection**, and **Cross-linking Navigation**.

### Design Principles

- **Icons + Colors + Charts**: Mixed visual approach using game icons for items, status colors (red/yellow/green), and
  trend charts
- **Inline Warnings**: Embed alerts within cards/rows rather than separate panel
- **Cross-linking**: Click any item/station to see all related entities
- **Incremental Delivery**: One feature at a time, verified with Chrome DevTools

### Current State

- 5 pages built: Galaxy Map, Logistics, Production, Power, Research
- D3.js galaxy visualization with shipping routes
- 18 API endpoints available with rich logistics/production data

---

## Design System (Match Game UI)

Based on game screenshots, implement a cohesive design system matching the in-game Statistics Panel.

### Color Palette

```css
:root {
    /* Backgrounds */
    --bg-primary: #0d0d1a; /* Dark panel background */
    --bg-secondary: #1a1a2e; /* Slightly lighter */
    --bg-card: rgba(13, 13, 26, 0.95); /* Semi-transparent panels */

    /* Accents */
    --accent-cyan: #00d4ff; /* Primary accent, production, supply */
    --accent-orange: #ff9500; /* Consumption, demand, warnings */
    --accent-yellow: #ffd700; /* Reference rates, stars */
    --accent-green: #22c55e; /* Positive, healthy */
    --accent-red: #ef4444; /* Critical, alerts */

    /* Text */
    --text-primary: #ffffff;
    --text-secondary: #a0a0b0;
    --text-muted: #6b7280;

    /* Borders */
    --border-subtle: rgba(255, 255, 255, 0.1);
}
```

### Typography

- **Large metrics**: 24-32px, bold, colored by type (cyan/orange)
- **Units**: 12-14px, muted gray, inline after number (e.g., "29.3 **GW**")
- **Item names**: 14-16px, white
- **Labels**: 12px, uppercase, muted
- **Number formatting**: Use "k", "M" suffixes (e.g., 14.7k, 1.05M)

### File: `web/src/lib/styles/game-theme.css`

New CSS file with game-matched variables and utility classes.

### File: `web/src/lib/components/ui/`

New component directory:

- `MetricCard.svelte` - Large metric display with icon
- `CircularGauge.svelte` - Ring gauge with percentage

### Phase 0: Power Page ✓ Complete

Power page fully implemented with:

- Game-themed CSS variables and design system
- CircularGauge, MetricCard components
- Real-time cluster overview with generation/consumption metrics
- Generation breakdown chart (icon + bar + value per generator type)
- Planet cards with ring gauges and generator type icons
- Star system filter dropdown
- Hover tooltips showing per-planet generation breakdown

### Verification Checklist (Chrome DevTools MCP)

1. Navigate to http://localhost:5173/research
2. Take screenshot - verify breakdown section appears below progress bar
3. Take snapshot - verify breakdown rows have correct structure
4. Verify bar widths are proportional to hash rates
5. Verify hash rates display correctly formatted (e.g., "135 kH/s")
6. Test with mock data showing varied hash rates to verify proportional bars
7. Verify section is hidden when no planets have research

### Future Enhancements (Out of Scope)

- Clickable planet names linking to planet detail page
- Hover tooltip showing lab count (working/idle)
- Color coding based on utilization (workingLabs/labCount)

---

### Phase 0: Game Data Research (Prerequisite)

#### Objective

Analyze the Dyson Sphere Program `Assembly-CSharp.dll` to determine if historical/aggregate shipping data is available
beyond the current real-time ship tracking.

#### Research Questions

1. Does `StationComponent` or `GalacticTransport` track cumulative transport statistics?
2. Are there any statistics classes that aggregate route traffic over time?
3. Can we access total items transported per route (not just current ships)?
4. Is route frequency/usage tracked anywhere in the game?

#### Method

Use the DecompilerServer MCP tools to:

1. Load `Assembly-CSharp.dll` from game directory
2. Search for types: `StationComponent`, `GalacticTransport`, `TrafficStatistics`, `*Statistics*`
3. Examine fields for cumulative counters (totalShipped, routeCount, etc.)
4. Check if production statistics (`ProductionStatistics`) has transport data

#### Expected Outcomes

- **If found:** Add API endpoints to expose aggregate route data
- **If not found:** Implement client-side accumulation over time, or proceed with real-time data only

#### Deliverable

Document findings and update PRD with data availability.

---

### Out of Scope (Future Phases)

- Planet-level view (zooming into star to see orbiting planets)
- Dyson sphere visualization
- Real-time WebSocket updates (currently using polling)
- Item category color coding for routes
- Historical playback of shipping activity
- Search/filter by item name
