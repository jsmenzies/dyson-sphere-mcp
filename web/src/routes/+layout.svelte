<script>
  import "../app.css";
</script>

<div class="min-h-screen flex flex-col">
  <header class="bg-gray-800 border-b border-gray-700 p-4">
    <h1 class="text-xl font-bold text-cyan-400">DSP Galaxy Map</h1>
  </header>
  <main class="flex-1 relative overflow-hidden">
    <slot />
  </main>
</div>
