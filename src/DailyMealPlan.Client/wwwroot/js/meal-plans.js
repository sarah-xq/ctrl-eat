(function () {
    const list = document.querySelector('[data-snack-list]');
    const addButton = document.querySelector('[data-add-snack]');
    if (!list || !addButton) return;

    function renumber() {
        list.querySelectorAll('[data-snack-row]').forEach((row, index) => {
            row.querySelectorAll('[data-snack-field]').forEach((field) => {
                const suffix = field.dataset.snackField;
                const name = `Input.Snacks[${index}].${suffix === 'isRecipe' ? 'IsRecipe' : suffix === 'itemId' ? 'ItemId' : suffix === 'quantity' ? 'Quantity' : 'Unit'}`;
                field.name = name;
                field.id = name.replaceAll('.', '_').replaceAll('[', '_').replaceAll(']', '_');
            });
            row.querySelectorAll('input[type="hidden"]').forEach((field) => {
                if (field.dataset.snackHidden === 'isRecipe' || field.name.endsWith('.IsRecipe')) {
                    field.name = `Input.Snacks[${index}].IsRecipe`;
                }
            });
        });
    }

    function addRow() {
        const row = document.createElement('div');
        row.className = 'snack-row';
        row.dataset.snackRow = '';
        row.innerHTML = `
            <label><input type="hidden" value="false" data-snack-hidden="isRecipe"><input type="checkbox" data-snack-field="isRecipe"> Recipe</label>
            <label>Item ID <input type="number" min="1" data-snack-field="itemId"></label>
            <label>Quantity <input type="number" min="0.000001" step="any" value="1" data-snack-field="quantity"></label>
            <label>Unit <input type="text" data-snack-field="unit"></label>
            <button type="button" class="danger secondary" data-remove-snack>Remove</button>`;
        list.appendChild(row);
        renumber();
    }

    list.addEventListener('click', (event) => {
        if (event.target.closest('[data-remove-snack]')) {
            event.target.closest('[data-snack-row]').remove();
            renumber();
        }
    });
    addButton.addEventListener('click', addRow);
    renumber();
})();