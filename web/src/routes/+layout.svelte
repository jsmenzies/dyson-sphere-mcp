<script>
  import "../app.css";
</script>

<div class="min-h-screen flex flex-col">
  <header class="bg-gray-800 border-b border-gray-700 p-4 flex justify-between items-center">
    <div class="flex items-center space-x-8">
      <h1 class="text-xl font-bold text-cyan-400">DSP Manager</h1>
      <nav class="flex space-x-4">
        <a href="/" class="text-gray-300 hover:text-white px-3 py-2 rounded-md text-sm font-medium">Galaxy Map</a>
        <a href="/logistics" class="text-gray-300 hover:text-white px-3 py-2 rounded-md text-sm font-medium">Logistics</a>
        <a href="/production" class="text-gray-300 hover:text-white px-3 py-2 rounded-md text-sm font-medium">Production</a>
        <a href="/power" class="text-gray-300 hover:text-white px-3 py-2 rounded-md text-sm font-medium">Power</a>
        <a href="/research" class="text-gray-300 hover:text-white px-3 py-2 rounded-md text-sm font-medium">Research</a>
      </nav>
    </div>
  </header>
  <main class="flex-1 relative overflow-hidden">
    <slot />
  </main>
</div>
