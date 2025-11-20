/**
 * Quill Toolbar Builder - Drag-and-Drop Interactive UI
 * Manages toolbar configuration with SortableJS
 */

(function () {
    'use strict';

    // Module state
    let state = {
        groups: [],
        buttonRegistry: {},
        nextGroupId: 1,
        prefix: '' // Will be detected from existing form inputs
    };

    /**
     * Main initialization function
     */
    function initialize() {
        const container = document.querySelector('.quill-toolbar-builder');
        if (!container) return;

        // Check if SortableJS is loaded
        if (typeof Sortable === 'undefined') {
            console.error('SortableJS library not loaded! Drag and drop functionality will not work.');
            console.error('Please ensure SortableJS is included before quill-toolbar-builder.js');
            return;
        }

        try {
            initializeState();
            initializePalette();
            initializeToolbarGroups();
            initializeColorPalette();
            initializePresetButtons();
            initializeButtonSearch();

            // Update button usage indicators for existing buttons
            updateButtonUsageIndicators();
        } catch (error) {
            console.error('Error during initialization:', error);
        }
    }

    // Initialize when DOM is ready (handle race condition)
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        // DOM already loaded, initialize immediately
        initialize();
    }

    /**
     * Initialize state from DOM (existing groups and buttons)
     */
    function initializeState() {
        const groupElements = document.querySelectorAll('.toolbar-group');
        state.groups = [];

        // Detect prefix from first hidden input
        const firstHiddenInput = document.querySelector('.toolbar-group input[type="hidden"][name*=".Groups["]');
        if (firstHiddenInput) {
            const name = firstHiddenInput.name;
            const match = name.match(/^(.+)\.Groups\[/);
            if (match) {
                state.prefix = match[1] + '.';
            }
        }

        groupElements.forEach((groupEl, index) => {
            const groupId = groupEl.dataset.groupId || `group-${Date.now()}-${index}`;
            const buttons = [];

            groupEl.querySelectorAll('.button-chip').forEach(buttonChip => {
                const buttonType = buttonChip.dataset.buttonType;
                if (buttonType) {
                    buttons.push({ type: buttonType });
                }
            });

            state.groups.push({
                id: groupId,
                name: groupEl.querySelector('.group-name-input')?.value || '',
                order: index,
                buttons: buttons
            });
        });

        // Calculate next group ID
        const maxId = state.groups.reduce((max, g) => {
            const match = g.id.match(/group-(\d+)/);
            return match ? Math.max(max, parseInt(match[1])) : max;
        }, 0);
        state.nextGroupId = maxId + 1;
    }

    /**
     * Initialize button palette with drag-and-drop (clone mode)
     */
    function initializePalette() {
        const categories = document.querySelectorAll('#button-palette .accordion-body');

        categories.forEach(categoryBody => {
            try {
                new Sortable(categoryBody, {
                    group: {
                        name: 'buttons',
                        pull: 'clone',
                        put: false
                    },
                    animation: 150,
                    sort: false,
                    onStart: function (evt) {
                        evt.item.classList.add('sortable-drag');
                    },
                    onEnd: function (evt) {
                        evt.item.classList.remove('sortable-drag');
                    }
                });
            } catch (error) {
                console.error('Failed to initialize palette sortable:', error);
            }
        });
    }

    /**
     * Initialize toolbar groups area with drag-and-drop
     */
    function initializeToolbarGroups() {
        const toolbarGroups = document.getElementById('toolbar-groups');

        // Make groups themselves sortable
        try {
            new Sortable(toolbarGroups, {
                animation: 150,
                handle: '.group-drag-handle',
                ghostClass: 'sortable-ghost',
                chosenClass: 'sortable-chosen',
                dragClass: 'sortable-drag',
                onEnd: function () {
                    updateGroupOrder();
                    syncStateToDOM();
                }
            });
        } catch (error) {
            console.error('Failed to initialize toolbar groups sortable:', error);
        }

        // Initialize existing groups
        document.querySelectorAll('.toolbar-group').forEach(groupEl => {
            initializeGroupButtons(groupEl);
            attachGroupEventListeners(groupEl);
            initializeExistingButtonChips(groupEl); // Attach event listeners to server-rendered button chips
        });

        // Add Group button
        const addGroupBtn = document.getElementById('add-group-btn');
        if (addGroupBtn) {
            addGroupBtn.addEventListener('click', addNewGroup);
        }
    }

    /**
     * Initialize buttons area within a group
     */
    function initializeGroupButtons(groupEl) {
        const buttonsContainer = groupEl.querySelector('.group-buttons');
        if (!buttonsContainer) return;

        try {
            new Sortable(buttonsContainer, {
                group: 'buttons',
                animation: 150,
                ghostClass: 'sortable-ghost',
                chosenClass: 'sortable-chosen',
                dragClass: 'sortable-drag',
                onAdd: function (evt) {
                    handleButtonAdd(evt);
                },
                onRemove: function () {
                    updateButtonUsageIndicators();
                    syncStateToDOM();
                },
                onUpdate: function () {
                    // Called when item is reordered within the same list
                    syncStateToDOM();
                },
                onEnd: function () {
                    syncStateToDOM();
                }
            });
        } catch (error) {
            console.error('Failed to initialize group buttons sortable:', error);
        }
    }

    /**
     * Detect if item came from palette or another group and handle accordingly
     */
    function handleButtonAdd(evt) {
        // Determine if this is a clone from palette or a move between groups
        const isFromPalette = evt.from.classList.contains('accordion-body');

        if (isFromPalette) {
            handleButtonClone(evt);
        } else {
            handleButtonMove(evt);
        }
    }

    /**
     * Handle button being cloned from palette to a group
     */
    function handleButtonClone(evt) {
        const paletteItem = evt.item;
        const buttonType = paletteItem.dataset.buttonType;
        const groupEl = evt.to.closest('.toolbar-group');

        // Remove palette item clone (we'll create a proper button chip)
        paletteItem.remove();

        // Create button chip with null check
        const buttonChip = createButtonChip(buttonType);
        if (!buttonChip) {
            console.error(`Failed to create button chip for type: ${buttonType}`);
            return;
        }

        evt.to.appendChild(buttonChip);

        // Remove empty state if present
        evt.to.querySelector('.drop-zone-placeholder')?.remove();

        // Update button count badge
        updateGroupButtonCount(groupEl);

        // Update "in use" indicators
        updateButtonUsageIndicators();

        // Sync to hidden inputs
        syncStateToDOM();
    }

    /**
     * Handle button being moved between groups
     */
    function handleButtonMove(evt) {
        const groupEl = evt.to.closest('.toolbar-group');

        // Item is already a button chip, no need to recreate
        // Just update the UI state

        // Remove empty state if present
        evt.to.querySelector('.drop-zone-placeholder')?.remove();

        // Update button count badges for both source and destination
        updateGroupButtonCount(groupEl);
        const sourceGroup = evt.from.closest('.toolbar-group');
        if (sourceGroup) {
            updateGroupButtonCount(sourceGroup);
        }

        // Update "in use" indicators
        updateButtonUsageIndicators();

        // Sync to hidden inputs
        syncStateToDOM();
    }

    /**
     * Attach remove button event listener to a button chip
     */
    function attachRemoveButtonListener(chip) {
        const removeBtn = chip.querySelector('.remove-button-btn');
        if (!removeBtn) return;

        // Remove existing listener if any (to prevent duplicates)
        const newRemoveBtn = removeBtn.cloneNode(true);
        removeBtn.parentNode.replaceChild(newRemoveBtn, removeBtn);

        newRemoveBtn.addEventListener('click', function (e) {
            e.preventDefault();

            // Get references BEFORE removing the chip from DOM
            const groupEl = chip.closest('.toolbar-group');
            if (!groupEl) return;

            const buttonsContainer = groupEl.querySelector('.group-buttons');

            // Remove the chip
            chip.remove();

            // Show empty state if no buttons left
            if (buttonsContainer.querySelectorAll('.button-chip').length === 0) {
                const placeholder = document.createElement('div');
                placeholder.className = 'drop-zone-placeholder text-center text-muted py-3 w-100';
                placeholder.innerHTML = '<i class="fa-solid fa-hand-pointer me-2"></i>Drag buttons here';
                buttonsContainer.appendChild(placeholder);
            }

            updateGroupButtonCount(groupEl);
            updateButtonUsageIndicators();
            syncStateToDOM();
        });
    }

    /**
     * Initialize event listeners for existing button chips (server-rendered)
     */
    function initializeExistingButtonChips(groupEl) {
        const buttonChips = groupEl.querySelectorAll('.button-chip');
        buttonChips.forEach(chip => {
            attachRemoveButtonListener(chip);
        });
    }

    /**
     * Create a button chip element
     */
    function createButtonChip(buttonType) {
        // Get button metadata from palette
        const paletteItem = document.querySelector(`.button-palette-item[data-button-type="${buttonType}"]`);
        if (!paletteItem) {
            return null;
        }

        const icon = paletteItem.querySelector('.button-icon')?.textContent || '';
        const label = paletteItem.querySelector('.button-label')?.textContent || buttonType;

        const chip = document.createElement('div');
        chip.className = 'button-chip badge bg-light text-dark border d-inline-flex align-items-center';
        chip.dataset.buttonType = buttonType;

        chip.innerHTML = `
            <span class="button-icon me-1" style="font-weight: bold;">${icon}</span>
            <span class="button-label">${label}</span>
            <button type="button" class="btn-close btn-close-sm ms-2 remove-button-btn"
                    style="font-size: 0.6rem; padding: 0.25rem;"></button>
        `;

        // Attach remove button handler
        attachRemoveButtonListener(chip);

        return chip;
    }

    /**
     * Add new group
     */
    function addNewGroup() {
        const toolbarGroups = document.getElementById('toolbar-groups');
        const groupCount = document.querySelectorAll('.toolbar-group').length;
        const groupId = `group-${Date.now()}-${state.nextGroupId++}`;

        // Remove empty state if present
        toolbarGroups.querySelector('.empty-state')?.remove();

        const groupEl = document.createElement('div');
        groupEl.className = 'toolbar-group card mb-3';
        groupEl.dataset.groupId = groupId;

        groupEl.innerHTML = `
            <input type="hidden" name="${state.prefix}Groups[${groupCount}].Id" value="${groupId}" />
            <input type="hidden" name="${state.prefix}Groups[${groupCount}].Order" value="${groupCount}" />
            <div class="card-header d-flex align-items-center">
                <span class="group-drag-handle me-2" style="cursor: grab;">
                    <i class="fa-solid fa-grip-vertical text-muted"></i>
                </span>
                <input type="text" class="form-control form-control-sm group-name-input"
                       name="${state.prefix}Groups[${groupCount}].Name"
                       placeholder="Group name (optional)" style="max-width: 200px;" />
                <span class="badge bg-secondary ms-2 group-button-count">0 buttons</span>
                <button type="button" class="btn btn-sm btn-link text-danger ms-auto delete-group-btn">
                    <i class="fa-solid fa-trash"></i>
                </button>
            </div>
            <div class="card-body">
                <div class="group-buttons d-flex flex-wrap gap-2">
                    <div class="drop-zone-placeholder text-center text-muted py-3 w-100">
                        <i class="fa-solid fa-hand-pointer me-2"></i>Drag buttons here
                    </div>
                </div>
            </div>
        `;

        toolbarGroups.appendChild(groupEl);

        // Initialize sortable for this group's buttons
        initializeGroupButtons(groupEl);
        attachGroupEventListeners(groupEl);

        // Update state
        state.groups.push({
            id: groupId,
            name: '',
            order: groupCount,
            buttons: []
        });

        syncStateToDOM();
    }

    /**
     * Attach event listeners to a group
     */
    function attachGroupEventListeners(groupEl) {
        // Delete group button
        const deleteBtn = groupEl.querySelector('.delete-group-btn');
        if (deleteBtn) {
            deleteBtn.addEventListener('click', function () {
                groupEl.remove();

                // Show empty state if no groups left
                const toolbarGroups = document.getElementById('toolbar-groups');
                if (toolbarGroups.querySelectorAll('.toolbar-group').length === 0) {
                    toolbarGroups.innerHTML = `
                        <div class="empty-state text-center text-muted py-5">
                            <i class="fa-solid fa-layer-group fa-3x mb-3 opacity-25"></i>
                            <h5>No groups yet</h5>
                            <p>Click 'Add Group' to start building your toolbar</p>
                        </div>
                    `;
                }

                updateButtonUsageIndicators();
                syncStateToDOM();
            });
        }

        // Group name input - no listener needed, it's a regular form input with proper name attribute
    }

    /**
     * Update group order after drag
     */
    function updateGroupOrder() {
        document.querySelectorAll('.toolbar-group').forEach((groupEl, index) => {
            const groupId = groupEl.dataset.groupId;
            const group = state.groups.find(g => g.id === groupId);
            if (group) {
                group.order = index;
            }
        });
    }

    /**
     * Update button count badge for a group
     */
    function updateGroupButtonCount(groupEl) {
        const count = groupEl.querySelectorAll('.button-chip').length;
        const badge = groupEl.querySelector('.group-button-count');
        if (badge) {
            badge.textContent = `${count} button${count !== 1 ? 's' : ''}`;
        } else {
            // Fallback for old badge structure
            const oldBadge = groupEl.querySelector('.badge.bg-secondary');
            if (oldBadge) {
                oldBadge.textContent = `${count} button${count !== 1 ? 's' : ''}`;
            }
        }
    }

    /**
     * Update "in use" indicators in palette
     */
    function updateButtonUsageIndicators() {
        // Get all button types currently in use
        const usedButtonTypes = new Set();
        document.querySelectorAll('.button-chip').forEach(chip => {
            usedButtonTypes.add(chip.dataset.buttonType);
        });

        // Update palette items
        document.querySelectorAll('.button-palette-item').forEach(item => {
            const buttonType = item.dataset.buttonType;
            const isUsed = usedButtonTypes.has(buttonType);

            if (isUsed) {
                item.classList.add('in-use');
                const indicator = item.querySelector('.in-use-indicator');
                if (indicator) {
                    indicator.style.display = 'inline-block';
                }
            } else {
                item.classList.remove('in-use');
                const indicator = item.querySelector('.in-use-indicator');
                if (indicator) {
                    indicator.style.display = 'none';
                }
            }
        });
    }

    /**
     * Sync current UI state to hidden form inputs for ASP.NET model binding
     */
    function syncStateToDOM() {
        const toolbarGroups = document.getElementById('toolbar-groups');
        const groups = toolbarGroups.querySelectorAll('.toolbar-group');

        groups.forEach((groupEl, groupIndex) => {
            const groupId = groupEl.dataset.groupId;
            const groupNameInput = groupEl.querySelector('.group-name-input');
            const buttons = groupEl.querySelectorAll('.button-chip');

            // Update group hidden inputs (with prefix)
            updateOrCreateHiddenInput(groupEl, `${state.prefix}Groups[${groupIndex}].Id`, groupId);
            updateOrCreateHiddenInput(groupEl, `${state.prefix}Groups[${groupIndex}].Order`, groupIndex);

            // Update group name input's name attribute to match index (with prefix)
            if (groupNameInput) {
                groupNameInput.name = `${state.prefix}Groups[${groupIndex}].Name`;
            }

            // Update button hidden inputs (with prefix)
            buttons.forEach((buttonChip, buttonIndex) => {
                const buttonType = buttonChip.dataset.buttonType;
                const buttonValue = buttonChip.dataset.buttonValue || '';

                // Remove ALL existing hidden inputs from this button chip to avoid duplicates
                buttonChip.querySelectorAll('input[type="hidden"]').forEach(input => input.remove());

                // Create fresh hidden inputs with correct indices
                const typeInput = createHiddenInput(`${state.prefix}Groups[${groupIndex}].Buttons[${buttonIndex}].Type`, buttonType);
                const valueInput = createHiddenInput(`${state.prefix}Groups[${groupIndex}].Buttons[${buttonIndex}].Value`, buttonValue);
                const orderInput = createHiddenInput(`${state.prefix}Groups[${groupIndex}].Buttons[${buttonIndex}].Order`, buttonIndex);

                buttonChip.appendChild(typeInput);
                buttonChip.appendChild(valueInput);
                buttonChip.appendChild(orderInput);
            });

            // Update button count
            updateGroupButtonCount(groupEl);
        });
    }

    /**
     * Update existing hidden input or create new one if it doesn't exist
     */
    function updateOrCreateHiddenInput(parentEl, name, value) {
        let input = parentEl.querySelector(`input[type="hidden"][name="${name}"]`);

        if (input) {
            // Update existing input (use != null to allow 0 and empty string)
            input.value = value != null ? String(value) : '';
        } else {
            // Create new input
            input = createHiddenInput(name, value);
            parentEl.appendChild(input);
        }

        return input;
    }

    /**
     * Create hidden input element
     */
    function createHiddenInput(name, value) {
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = name;
        input.value = value != null ? String(value) : '';
        return input;
    }

    /**
     * Initialize color palette (existing functionality from Phase 4)
     */
    function initializeColorPalette() {
        const colorList = document.getElementById('color-list');
        const addColorBtn = document.getElementById('add-color-btn');
        const newColorInput = document.getElementById('new-color-input');

        if (addColorBtn) {
            addColorBtn.addEventListener('click', function () {
                const color = newColorInput.value;
                addColorToList(color);
            });
        }

        // Remove color handler (event delegation)
        colorList?.addEventListener('click', function (e) {
            if (e.target.classList.contains('remove-color-btn')) {
                e.target.closest('.color-item').remove();
                reindexColors();
            }
        });

        function addColorToList(color) {
            const currentIndex = colorList.querySelectorAll('.color-item').length;
            const colorItem = document.createElement('div');
            colorItem.className = 'color-item d-inline-flex align-items-center bg-white border rounded-pill px-2 py-1';
            colorItem.setAttribute('data-color', color);

            const hiddenInput = document.createElement('input');
            hiddenInput.type = 'hidden';
            hiddenInput.name = `${state.prefix}CustomColors[${currentIndex}]`;
            hiddenInput.value = color;

            const preview = document.createElement('div');
            preview.className = 'color-preview me-2 rounded-circle';
            preview.style.cssText = `width: 24px; height: 24px; background-color: ${color}; border: 2px solid #fff;`;

            const colorText = document.createElement('span');
            colorText.className = 'me-2 font-monospace small';
            colorText.textContent = color;

            const removeBtn = document.createElement('button');
            removeBtn.type = 'button';
            removeBtn.className = 'btn-close btn-close-sm remove-color-btn';
            removeBtn.style.fontSize = '0.6rem';

            colorItem.appendChild(hiddenInput);
            colorItem.appendChild(preview);
            colorItem.appendChild(colorText);
            colorItem.appendChild(removeBtn);
            colorList.appendChild(colorItem);

            document.getElementById('empty-color-state')?.remove();
        }

        function reindexColors() {
            const colorItems = colorList.querySelectorAll('.color-item');
            colorItems.forEach((item, index) => {
                const input = item.querySelector('input[type="hidden"]');
                input.name = `${state.prefix}CustomColors[${index}]`;
            });
        }
    }

    /**
     * Initialize preset buttons (Minimal, Standard, Full)
     */
    function initializePresetButtons() {
        document.querySelectorAll('.load-preset').forEach(btn => {
            btn.addEventListener('click', function () {
                const preset = this.dataset.preset;
                loadPreset(preset);
            });
        });
    }

    /**
     * Load a toolbar preset
     */
    function loadPreset(presetName) {
        // Clear existing groups
        const toolbarGroups = document.getElementById('toolbar-groups');
        toolbarGroups.innerHTML = '';

        // Reset state
        state.groups = [];
        state.nextGroupId = 1;

        // Define presets
        const presets = {
            minimal: [
                { name: 'Basic', buttons: ['bold', 'italic', 'link'] }
            ],
            standard: [
                { name: 'Formatting', buttons: ['bold', 'italic', 'underline', 'strike'] },
                { name: 'Blocks', buttons: ['blockquote', 'code-block'] },
                { name: 'Lists', buttons: ['list'] },
                { name: 'Media', buttons: ['link', 'image', 'video'] },
                { name: 'Advanced', buttons: ['clean'] }
            ],
            full: [
                { name: 'Text Style', buttons: ['bold', 'italic', 'underline', 'strike', 'code'] },
                { name: 'Color', buttons: ['color', 'background'] },
                { name: 'Structure', buttons: ['header', 'blockquote', 'code-block'] },
                { name: 'Lists & Align', buttons: ['list', 'align'] },
                { name: 'Advanced', buttons: ['script', 'indent', 'direction'] },
                { name: 'Media', buttons: ['link', 'image', 'video', 'formula'] },
                { name: 'Utilities', buttons: ['clean'] }
            ]
        };

        const preset = presets[presetName];
        if (!preset) {
            return;
        }

        // Create groups from preset
        preset.forEach((groupDef, index) => {
            const groupId = `group-${Date.now()}-${state.nextGroupId++}`;

            const groupEl = document.createElement('div');
            groupEl.className = 'toolbar-group card mb-3';
            groupEl.dataset.groupId = groupId;

            groupEl.innerHTML = `
                <input type="hidden" name="${state.prefix}Groups[${index}].Id" value="${groupId}" />
                <input type="hidden" name="${state.prefix}Groups[${index}].Order" value="${index}" />
                <div class="card-header d-flex align-items-center">
                    <span class="group-drag-handle me-2" style="cursor: grab;">
                        <i class="fa-solid fa-grip-vertical text-muted"></i>
                    </span>
                    <input type="text" class="form-control form-control-sm group-name-input"
                           name="${state.prefix}Groups[${index}].Name"
                           value="${groupDef.name}" placeholder="Group name (optional)" style="max-width: 200px;" />
                    <span class="badge bg-secondary ms-2 group-button-count">${groupDef.buttons.length} buttons</span>
                    <button type="button" class="btn btn-sm btn-link text-danger ms-auto delete-group-btn">
                        <i class="fa-solid fa-trash"></i>
                    </button>
                </div>
                <div class="card-body">
                    <div class="group-buttons d-flex flex-wrap gap-2"></div>
                </div>
            `;

            toolbarGroups.appendChild(groupEl);

            // Add buttons to group
            const buttonsContainer = groupEl.querySelector('.group-buttons');
            groupDef.buttons.forEach(buttonType => {
                const buttonChip = createButtonChip(buttonType, index);
                if (buttonChip) {
                    buttonsContainer.appendChild(buttonChip);
                }
            });

            // Initialize sortable and event listeners
            initializeGroupButtons(groupEl);
            attachGroupEventListeners(groupEl);

            // Update state
            state.groups.push({
                id: groupId,
                name: groupDef.name,
                order: index,
                buttons: groupDef.buttons.map(type => ({ type }))
            });
        });

        // Update UI
        updateButtonUsageIndicators();
        syncStateToDOM();
    }

    /**
     * Initialize button search functionality
     */
    function initializeButtonSearch() {
        const searchInput = document.getElementById('button-search');
        if (!searchInput) return;

        searchInput.addEventListener('input', function () {
            const query = this.value.toLowerCase().trim();
            const paletteItems = document.querySelectorAll('.button-palette-item');
            const accordion = document.getElementById('button-palette');
            const emptyState = document.getElementById('search-empty-state');
            let hasVisibleResults = false;

            if (query === '') {
                // Show all items and restore accordion state
                paletteItems.forEach(item => item.style.display = '');
                accordion.querySelectorAll('.accordion-collapse').forEach((collapse, index) => {
                    if (index === 0) {
                        collapse.classList.add('show');
                    } else {
                        collapse.classList.remove('show');
                    }
                });
                if (emptyState) emptyState.style.display = 'none';
                return;
            }

            // Filter items by search query
            paletteItems.forEach(item => {
                const label = item.querySelector('.button-label')?.textContent.toLowerCase() || '';
                const category = item.dataset.buttonCategory?.toLowerCase() || '';
                const matches = label.includes(query) || category.includes(query);

                item.style.display = matches ? '' : 'none';
                if (matches) hasVisibleResults = true;
            });

            // Show/hide categories based on visible items
            accordion.querySelectorAll('.accordion-item').forEach(accordionItem => {
                const body = accordionItem.querySelector('.accordion-body');
                const visibleItems = body.querySelectorAll('.button-palette-item:not([style*="display: none"])');

                if (visibleItems.length > 0) {
                    accordionItem.style.display = '';
                    accordionItem.querySelector('.accordion-collapse')?.classList.add('show');
                } else {
                    accordionItem.style.display = 'none';
                }
            });

            // Show/hide empty state
            if (emptyState) {
                emptyState.style.display = hasVisibleResults ? 'none' : 'block';
            }
        });
    }

})();
