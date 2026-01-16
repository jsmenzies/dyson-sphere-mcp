<script lang="ts">
    import { createEventDispatcher } from 'svelte';

    export let items: Array<{ id: number; name: string }> = [];
    export let selectedItem: { id: number; name: string } | null = null;
    export let shipCount: number = 0;

    const dispatch = createEventDispatcher<{
        select: { id: number; name: string };
    }>();

    let searchTerm = '';
    let isOpen = false;

    $: filteredItems = items.filter(item =>
        item.name.toLowerCase().includes(searchTerm.toLowerCase())
    );

    function selectItem(item: { id: number; name: string }) {
        selectedItem = item;
        searchTerm = item.name;
        isOpen = false;
        dispatch('select', item);
    }

    function handleFocus() {
        isOpen = true;
    }

    function handleBlur() {
        // Delay to allow click on dropdown item
        setTimeout(() => {
            isOpen = false;
        }, 200);
    }
</script>

<div class="item-search">
    <div class="search-container">
        <input
            type="text"
            bind:value={searchTerm}
            on:focus={handleFocus}
            on:blur={handleBlur}
            placeholder="Search items..."
            class="search-input"
        />

        {#if isOpen && filteredItems.length > 0}
            <div class="dropdown">
                {#each filteredItems as item}
                    <button
                        class="dropdown-item"
                        on:click={() => selectItem(item)}
                    >
                        {item.name}
                        <span class="item-id">#{item.id}</span>
                    </button>
                {/each}
            </div>
        {/if}
    </div>

    {#if selectedItem}
        <div class="selection-info">
            <span class="item-name">{selectedItem.name}</span>
            <span class="ship-count">{shipCount} ships in transit</span>
        </div>
    {/if}
</div>

<style>
    .item-search {
        position: absolute;
        top: 1rem;
        left: 1rem;
        z-index: 100;
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
    }

    .search-container {
        position: relative;
    }

    .search-input {
        width: 280px;
        padding: 0.75rem 1rem;
        background: rgba(13, 13, 26, 0.95);
        border: 1px solid rgba(0, 212, 255, 0.3);
        border-radius: 8px;
        color: white;
        font-size: 0.875rem;
        outline: none;
        transition: border-color 0.2s;
    }

    .search-input:focus {
        border-color: var(--accent-cyan, #00d4ff);
        box-shadow: 0 0 10px rgba(0, 212, 255, 0.2);
    }

    .search-input::placeholder {
        color: rgba(160, 160, 176, 0.6);
    }

    .dropdown {
        position: absolute;
        top: 100%;
        left: 0;
        right: 0;
        margin-top: 4px;
        background: rgba(13, 13, 26, 0.98);
        border: 1px solid rgba(0, 212, 255, 0.3);
        border-radius: 8px;
        max-height: 200px;
        overflow-y: auto;
    }

    .dropdown-item {
        width: 100%;
        padding: 0.75rem 1rem;
        background: transparent;
        border: none;
        color: white;
        font-size: 0.875rem;
        text-align: left;
        cursor: pointer;
        display: flex;
        justify-content: space-between;
        align-items: center;
        transition: background-color 0.15s;
    }

    .dropdown-item:hover {
        background: rgba(0, 212, 255, 0.1);
    }

    .item-id {
        color: rgba(160, 160, 176, 0.6);
        font-size: 0.75rem;
    }

    .selection-info {
        display: flex;
        align-items: center;
        gap: 1rem;
        padding: 0.5rem 1rem;
        background: rgba(13, 13, 26, 0.9);
        border-radius: 6px;
        border: 1px solid rgba(255, 149, 0, 0.3);
    }

    .item-name {
        color: var(--accent-orange, #ff9500);
        font-weight: 600;
    }

    .ship-count {
        color: rgba(160, 160, 176, 0.8);
        font-size: 0.8rem;
    }
</style>
