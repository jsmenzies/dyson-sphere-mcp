# DSP Manager Frontend - Product Requirements Document

## Overview

Build an enhanced web frontend for Dyson Sphere Program that visualizes game data in an easy-to-digest manner, with focus on **Item Flow Tracking**, **Bottleneck Detection**, and **Cross-linking Navigation**.

### Design Principles
- **Icons + Colors + Charts**: Mixed visual approach using game icons for items, status colors (red/yellow/green), and trend charts
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
  --bg-primary: #0d0d1a;        /* Dark panel background */
  --bg-secondary: #1a1a2e;      /* Slightly lighter */
  --bg-card: rgba(13, 13, 26, 0.95); /* Semi-transparent panels */

  /* Accents */
  --accent-cyan: #00d4ff;       /* Primary accent, production, supply */
  --accent-orange: #ff9500;     /* Consumption, demand, warnings */
  --accent-yellow: #ffd700;     /* Reference rates, stars */
  --accent-green: #22c55e;      /* Positive, healthy */
  --accent-red: #ef4444;        /* Critical, alerts */

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

### Components to Build

#### 1. Vertical Sidebar Navigation
```
┌──────────────┐
│ ▌ Production │ ← cyan bar indicator when active
│   Power      │
│   Research   │
│   Logistics  │
└──────────────┘
```

#### 2. Metric Cards
```
┌─────────────────────────────┐
│ ⚡ Real-time Power Data     │
│                             │
│ Generation      Consumption │
│ 29.3 GW         17.4 GW     │
│ (cyan)          (orange)    │
└─────────────────────────────┘
```

#### 3. Circular Gauge
- Ring with fill percentage
- Large percentage text in center
- Label below (e.g., "Sufficiency")

#### 4. Item Row (Production Table Style)
```
┌────┬──────────┬───────────┬───────────┬──────────┬──────────┐
│ 🔷 │ Iron Ore │ 14.7k /m  │ 15.0k /m  │ 1.05M /m │ ▁▂▃▅▇   │
│ ⭐⭐ │          │ Production│Consumption│ Reference│ Sparkline│
└────┴──────────┴───────────┴───────────┴──────────┴──────────┘
```

#### 5. Sparkline Chart
- Small bar chart (orange/cyan bars)
- ~100px wide, inline in tables
- Shows trend over time

#### 6. Progress Bar (Storage)
- Rounded ends
- Gradient fill based on status
- Value label on right

### File: `web/src/lib/styles/game-theme.css`
New CSS file with game-matched variables and utility classes.

### File: `web/src/lib/components/ui/`
New component directory:
- `MetricCard.svelte` - Large metric display with icon
- `CircularGauge.svelte` - Ring gauge with percentage
- `Sparkline.svelte` - Mini bar chart
- `ItemRow.svelte` - Production table row style
- `SidebarNav.svelte` - Vertical navigation with active indicator
- `ProgressBar.svelte` - Game-style progress bar

---

## Phase 1: Item Icon System

**Goal**: Add game item icons throughout the UI to improve scannability.

### 1.1 Wiki Icon Scraper
Create a Python script to scrape all item icon URLs from the wiki.

**File**: `api/tools/scrape_icons.py`
```python
# Scrapes https://dyson-sphere-program.fandom.com/wiki/Items
# Extracts item names and icon URLs
# Outputs: web/src/lib/data/item-icons.json
```

**Output**: `web/src/lib/data/item-icons.json`
```json
{
  "1001": {"name": "Iron Ore", "icon": "https://static.wikia.nocookie.net/..."},
  "1002": {"name": "Copper Ore", "icon": "https://static.wikia.nocookie.net/..."}
}
```

**Note**: Run once, commit the JSON file. Re-run if game adds new items.

**Scraping Strategy**:
1. Start from https://dyson-sphere-program.fandom.com/wiki/Items
2. Extract all item links from the page
3. For each item page, extract the infobox image URL
4. Map to game item IDs using item name matching (API provides ~150 items with names)
5. Handle special characters (spaces → underscores, etc.)

### 1.2 ItemIcon Component
Reusable component displaying item icon with fallback.

**File**: `web/src/lib/components/ItemIcon.svelte`
- Props: `itemId`, `itemName`, `size` (sm/md/lg)
- Displays wiki icon or colored letter fallback
- Tooltip showing full item name

### 1.3 Apply to Existing Pages
- **Logistics**: Add icons next to item names in storage slots
- **Production**: Add icons in the ITEM column

**Verification Checklist** (using Chrome DevTools MCP):
1. Navigate to http://localhost:5173/logistics
2. Take screenshot - verify icons appear next to item names
3. Take snapshot - verify `img` elements with item icon URLs
4. Navigate to http://localhost:5173/production
5. Take screenshot - verify icons in ITEM column
6. Test fallback - check item with missing icon shows colored letter

---

## Phase 2: Inline Bottleneck Warnings

**Goal**: Show status indicators embedded in cards to highlight issues at a glance.

### 2.1 Status Color System
Define consistent status colors:
```typescript
export const STATUS_COLORS = {
  critical: '#ef4444',  // red - immediate attention
  warning: '#f59e0b',   // amber - potential issue
  good: '#22c55e',      // green - healthy
  neutral: '#6b7280',   // gray - no data
};
```

### 2.2 Ship/Drone Utilization Warning
**Location**: Logistics page station cards

**Logic**:
- `critical`: Ships = working/total (all busy)
- `warning`: Ships > 80% utilized
- Add pulsing border or badge when critical

**Display**:
```
SHIPS: 10/10 ⚠️ ALL BUSY
```

### 2.3 Item Deficit Warning
**Location**: Production page

**Logic**:
- `critical`: Net < -1000/min
- `warning`: Net < 0
- Row highlight + deficit badge

### 2.4 Station Starvation Warning
**Location**: Logistics page storage slots

**Logic**:
- `critical`: Demand mode + count = 0
- `warning`: Demand mode + count < 10% of max
- Red outline on storage bar

### 2.5 Power Shortage Warning
**Location**: Power page cards (already exists, enhance)

**Logic**:
- `critical`: Satisfaction < 80%
- `warning`: Satisfaction < 100%
- Already has pulsing animation - keep it

**Verification**: Create test scenarios in mock data, screenshot warnings

---

## Phase 3: Cross-linking Navigation

**Goal**: Click any entity to see related data across the app.

### 3.1 Clickable Items
When clicking an item anywhere:
- Show modal/panel with:
  - Item icon + name
  - Production rate (global)
  - Consumption rate (global)
  - Net balance
  - **Stations supplying** this item (links to Logistics)
  - **Stations demanding** this item (links to Logistics)
  - **Ships transporting** this item (live count)

**API**: Uses `find_item_transport(itemId)`, `list_ils_per_planet`

### 3.2 Clickable Stations
When clicking a station:
- Show panel with:
  - Station ID, planet, system
  - Fleet status (drones/ships/warpers)
  - Storage items (with icons)
  - **Incoming routes** (from which stations)
  - **Outgoing routes** (to which stations)
  - Link to planet view

**API**: Uses `get_shipping_routes_for_ils(stationId)`

### 3.3 Clickable Systems (Galaxy Map)
Already exists - enhance with:
- Show planets in system
- Total ILS stations
- Active shipping routes count
- Power status summary

### 3.4 URL State
- Support deep-linking: `/logistics?station=572`
- Support item filtering: `/production?item=1208`
- Enable browser back/forward navigation

**Verification**: Click item on Production, verify modal shows correct data

---

## Phase 4: Animated Item Flow (Primary Feature)

**Goal**: Visualize item movement across the galaxy with animated ships.

### 4.1 Enhanced Galaxy Map
**File**: `web/src/lib/components/GalaxyMap.svelte`

Features:
- Toggle: "Show Live Ships"
- Animated dots traveling along route lines
- Dot color = item category color
- Dot size = shipment size
- Hover dot = show item name + quantity + destination

### 4.2 Ship Animation System
```typescript
interface AnimatedShip {
  fromStar: {x, z};
  toStar: {x, z};
  progress: number;  // 0-1
  itemId: number;
  itemCount: number;
  remainingSeconds: number;
}
```

Use D3 transitions or requestAnimationFrame for smooth animation.

### 4.3 Item Flow Filter
- Dropdown to filter by item type
- When filtered:
  - Dim routes not carrying selected item
  - Highlight routes carrying selected item
  - Show only ships with selected item

### 4.4 Flow Direction Indicators
- Arrow heads on route lines showing direction
- Line thickness = traffic volume
- Dashed = low traffic, solid = high traffic

**Verification**: Select "Iron Ore" filter, observe only iron ore ships animate

---

## Phase 5: Item Flow Detail View

**Goal**: Dedicated page showing where items come from and go.

### 5.1 New Route: `/flow`
**Files**:
- `web/src/routes/flow/+page.svelte`
- `web/src/routes/flow/+page.ts`

### 5.2 Item Selector
- Search/dropdown to select an item
- Show item icon + production/consumption summary

### 5.3 Source/Destination Table
| Source Station | Source Planet | → | Dest Station | Dest Planet | Ships Active | Items/min |
|---|---|---|---|---|---|---|

### 5.4 Mini Sankey (Optional)
Simple horizontal flow diagram:
```
[Producers] ──→ [ILS Suppliers] ──→ [ILS Demanders] ──→ [Consumers]
```

**Verification**: Navigate to /flow, select Critical Photon, verify flow data matches API

---

## Phase 6: Dashboard Overview

**Goal**: At-a-glance health summary on home page or dedicated dashboard.

### 6.1 Alert Summary Cards
Quick stats at top of Galaxy Map:
- 🔴 **X** Critical Issues
- 🟡 **Y** Warnings
- 🟢 **Z** Systems Healthy

Click to expand list of issues.

### 6.2 Key Metrics Row
- Total Production Rate
- Total Consumption Rate
- Active Ships / Total Ships
- Power Satisfaction (cluster avg)

### 6.3 Recent Activity (Optional)
- Last 5 ship departures/arrivals
- Tech research progress mini-bar

**Verification**: Screenshot dashboard, confirm metrics update

---

## Technical Notes

### Icon Caching
- Create `/web/public/icons/` directory
- Download and cache wiki icons locally
- Fallback to wiki URL if local not found

### Performance
- Lazy load station details (don't fetch all 643 at once)
- Use virtual scrolling for long lists
- Debounce filter inputs

### Files to Modify
- `web/src/lib/types.ts` - Add status types
- `web/src/lib/components/ItemIcon.svelte` - New
- `web/src/lib/components/StatusBadge.svelte` - New
- `web/src/lib/components/GalaxyMap.svelte` - Add animation
- `web/src/routes/+page.svelte` - Add dashboard metrics
- `web/src/routes/logistics/+page.svelte` - Add icons, warnings
- `web/src/routes/production/+page.svelte` - Add icons, warnings
- `web/src/routes/flow/+page.svelte` - New page

---

## Implementation Order

| Phase | Feature | Complexity | Dependencies |
|-------|---------|------------|--------------|
| **0** | **Design System** | Medium | None |
| 1 | Item Icon System | Low | Phase 0 |
| 2 | Inline Warnings | Medium | Phases 0-1 |
| 3 | Cross-linking | Medium | Phase 1 |
| 4 | Animated Flow | High | Phases 1-3 |
| 5 | Flow Detail Page | Medium | Phase 4 |
| 6 | Dashboard Overview | Low | Phases 0-2 |

### Pre-Implementation: Commit Current State
```bash
git add -A
git commit -m "feat: Phase 4 baseline - working frontend before design system"
git push
```

### Phase 0 Implementation Steps (Power Page Full Redesign)

**Design Decisions:**
- Keep top horizontal navigation (don't switch to sidebar)
- Power page as test case (matches game's circular gauges)
- Full page redesign approach (commit baseline first)

**Steps:**
1. Create `web/src/lib/styles/game-theme.css` with CSS variables
2. Build `web/src/lib/components/ui/CircularGauge.svelte`
3. Build `web/src/lib/components/ui/MetricCard.svelte`
4. Build `web/src/lib/components/ui/Sparkline.svelte`
5. Update top nav styling in `+layout.svelte` (keep horizontal, apply new colors)
6. **Full redesign**: `web/src/routes/power/+page.svelte` to match game Power panel:
   - Real-time Power Data header with lightning icon
   - Generation/Consumption metric cards (cyan/orange)
   - Large circular gauge showing Sufficiency %
   - Generator list with icons and power values
7. **Verify**: Screenshot Power page, compare with game screenshot

**Target Design (Power Page):**
```
┌─────────────────────────────────────────────────┐
│ ⚡ Real-time Power Data                         │
│                                                 │
│ Generation Capacity    Consumption Demand       │
│ 29.3 GW (cyan)         17.4 GW (orange)        │
│                                                 │
│ [═══════════════════░░░░░░░] Area Chart        │
│                                                 │
│    ┌─────────┐           ┌─────────┐           │
│    │  100%   │           │ 29.3 GW │           │
│    │Sufficiency│         │Generation│           │
│    └─────────┘           └─────────┘           │
│                                                 │
│ Generator List:                                 │
│ 🌟 Artificial Star     29.3 GW    99.9%        │
│ 🔥 Thermal Power        14.8 MW    0.1%        │
└─────────────────────────────────────────────────┘
```

**Recommended Start**: Commit baseline → CSS variables → CircularGauge → Power page redesign → Verify
