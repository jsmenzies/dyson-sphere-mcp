<script>
  import "../app.css";
  import "$lib/styles/game-theme.css";
  import { page } from '$app/stores';

  $: currentPath = $page.url.pathname;

  const navItems = [
    { href: '/', label: 'Galaxy Map' },
    { href: '/logistics', label: 'Logistics' },
    { href: '/production', label: 'Production' },
    { href: '/power', label: 'Power' },
    { href: '/research', label: 'Research' }
  ];

  function isActive(href, path) {
    if (href === '/') return path === '/';
    return path.startsWith(href);
  }
</script>

<div class="app-container">
  <header class="app-header">
    <div class="header-content">
      <h1 class="app-title">DSP Manager</h1>
      <nav class="main-nav">
        {#each navItems as item}
          <a
            href={item.href}
            class="nav-link"
            class:active={isActive(item.href, currentPath)}
          >
            {item.label}
          </a>
        {/each}
      </nav>
    </div>
  </header>
  <main class="app-main">
    <slot />
  </main>
</div>

<style>
  .app-container {
    min-height: 100vh;
    display: flex;
    flex-direction: column;
    background-color: var(--bg-primary);
  }

  .app-header {
    background-color: var(--bg-secondary);
    border-bottom: 1px solid var(--border-subtle);
    padding: 0 1.5rem;
    height: 56px;
    display: flex;
    align-items: center;
  }

  .header-content {
    display: flex;
    align-items: center;
    gap: 3rem;
    width: 100%;
  }

  .app-title {
    font-size: 1.25rem;
    font-weight: 700;
    color: var(--accent-cyan);
    margin: 0;
    letter-spacing: -0.02em;
  }

  .main-nav {
    display: flex;
    gap: 0.5rem;
  }

  .nav-link {
    position: relative;
    padding: 0.5rem 1rem;
    font-size: 0.875rem;
    font-weight: 500;
    color: var(--text-secondary);
    text-decoration: none;
    border-radius: var(--radius-sm);
    transition: color var(--transition-fast), background-color var(--transition-fast);
  }

  .nav-link:hover {
    color: var(--text-primary);
    background-color: rgba(255, 255, 255, 0.05);
  }

  .nav-link.active {
    color: var(--accent-cyan);
    background-color: rgba(0, 212, 255, 0.1);
  }

  .nav-link.active::after {
    content: '';
    position: absolute;
    bottom: -1px;
    left: 0;
    right: 0;
    height: 2px;
    background-color: var(--accent-cyan);
    border-radius: 1px 1px 0 0;
  }

  .app-main {
    flex: 1;
    position: relative;
    overflow: hidden;
  }
</style>
