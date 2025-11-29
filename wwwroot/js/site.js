document.addEventListener("DOMContentLoaded", function () {
    // This is the main function that runs when the page is loaded.
    const salesLayout = document.querySelector('.sales-layout');
    if (salesLayout) {
        initializeSalesPage();
    }
});

function initializeSalesPage() {
    const cart = new Map();
    const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const sizeModal = new bootstrap.Modal(document.getElementById('sizeSelectionModal'));
    const productGrid = document.querySelector('.product-grid');
    const searchInput = document.getElementById('productSearch');
    const categoryFilter = document.getElementById('categoryFilter');

    // --- Event Listeners ---
    if (searchInput) searchInput.addEventListener('input', filterProducts);
    if (categoryFilter) categoryFilter.addEventListener('change', filterProducts);
    if (productGrid) productGrid.addEventListener('click', handleProductClick);
    document.getElementById('cart-items-container').addEventListener('click', handleCartInteraction);
    document.getElementById('process-sale-btn').addEventListener('click', recordSale);

    function filterProducts() {
        const searchTerm = searchInput.value.toLowerCase();
        const selectedCategory = categoryFilter.value;
        productGrid.querySelectorAll('.product-card').forEach(card => {
            const productName = card.dataset.baseName.toLowerCase();
            const productCategory = card.dataset.category;
            card.style.display = (productName.includes(searchTerm) && (selectedCategory === 'All' || productCategory === selectedCategory)) ? 'block' : 'none';
        });
    }

    function handleProductClick(e) {
        const card = e.target.closest('.product-card');
        if (!card) return;

        if (card.classList.contains('product-group')) {
            const groupName = card.dataset.baseName;
            const sizeOptions = card.querySelectorAll('.size-option');
            const modalBody = document.getElementById('sizeOptionsContainer');

            document.getElementById('sizeSelectionModalLabel').textContent = `Select a size for ${groupName}`;
            modalBody.innerHTML = '';

            sizeOptions.forEach(opt => {
                const btn = document.createElement('button');
                btn.className = 'btn btn-outline-dark w-100 mb-2';
                btn.textContent = `${opt.dataset.name} - ₱${parseFloat(opt.dataset.price).toFixed(2)}`;
                btn.onclick = () => {
                    updateCart(opt.dataset.id, opt.dataset.name, parseFloat(opt.dataset.price), 1);
                    sizeModal.hide();
                };
                modalBody.appendChild(btn);
            });
            sizeModal.show();
        } else {
            updateCart(card.dataset.productId, card.dataset.productName, parseFloat(card.dataset.price), 1);
        }
    }

    function handleCartInteraction(e) {
        const cartItem = e.target.closest('.cart-item');
        if (!cartItem) return;

        const productId = cartItem.dataset.productId;
        const product = cart.get(productId);
        if (!product) return;

        if (e.target.closest('.btn-inc')) updateCart(productId, product.name, product.price, 1);
        else if (e.target.closest('.btn-dec')) updateCart(productId, product.name, product.price, -1);
    }

    function updateCart(productId, name, price, quantityChange) {
        if (cart.has(productId)) {
            const item = cart.get(productId);
            item.quantity += quantityChange;
            if (item.quantity <= 0) cart.delete(productId);
        } else if (quantityChange > 0) {
            cart.set(productId, { name, price, quantity: quantityChange });
        }
        renderCart();
    }

    function renderCart() {
        const container = document.getElementById('cart-items-container');
        const header = document.getElementById('cart-header');
        const totalEl = document.getElementById('cart-total-val');

        container.innerHTML = "";
        let subtotal = 0;
        let totalItems = 0;

        if (cart.size === 0) {
            container.innerHTML = '<p class="text-center text-secondary mt-4">Your cart is empty.</p>';
        } else {
            for (const [id, item] of cart.entries()) {
                subtotal += item.quantity * item.price;
                totalItems += item.quantity;
                container.innerHTML += `
                    <div class="cart-item" data-product-id="${id}">
                        <div class="cart-item-details">
                            <p class="cart-item-name mb-0">${item.name}</p>
                            <p class="cart-item-price">₱${item.price.toFixed(2)} each</p>
                        </div>
                        <div class="cart-item-controls">
                            <button class="btn btn-dec">-</button>
                            <span class="quantity">${item.quantity}</span>
                            <button class="btn btn-inc">+</button>
                        </div>
                        <p class="cart-item-total">₱${(item.quantity * item.price).toFixed(2)}</p>
                    </div>`;
            }
        }

        header.textContent = `Cart (${totalItems} items)`;
        totalEl.textContent = `₱${subtotal.toFixed(2)}`;
    }

    async function recordSale() {
        if (cart.size === 0) {
            showToast("Cart is empty.", false);
            return;
        }

        const saleDetailsPayload = Array.from(cart.entries()).map(([productId, item]) => ({
            ProductID: parseInt(productId, 10),
            Quantity: item.quantity,
            UnitPrice: item.price,
            LineTotal: item.quantity * item.price
        }));

        const salePayload = { SaleDetails: saleDetailsPayload };

        try {
            const response = await fetch('/Sales/RecordSale', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': antiForgeryToken },
                body: JSON.stringify(salePayload)
            });
            const result = await response.json();
            if (response.ok && result.success) {
                showToast(result.message, true);
                cart.clear(); renderCart();
            } else {
                showToast(result.message || "An unknown error occurred.", false);
            }
        } catch (error) {
            showToast('Could not connect to the server.', false);
        }
    }

    renderCart();
}

function showToast(message, isSuccess = true) {
    const container = document.querySelector('.toast-container');
    if (!container) return;
    const id = 'toast-' + Date.now();
    const icon = isSuccess ? `<i class="fas fa-check-circle text-success me-2"></i>` : `<i class="fas fa-times-circle text-danger me-2"></i>`;
    const html = `<div id="${id}" class="toast" role="alert" aria-live="assertive" aria-atomic="true"><div class="toast-header"><strong class="me-auto">${icon}${isSuccess ? 'Success' : 'Error'}</strong><button type="button" class="btn-close" data-bs-dismiss="toast"></button></div><div class="toast-body">${message}</div></div>`;
    container.insertAdjacentHTML('beforeend', html);
    new bootstrap.Toast(document.getElementById(id)).show();
}