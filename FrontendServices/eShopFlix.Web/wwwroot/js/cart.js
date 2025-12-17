const currencyFormat = (amount, currencySymbol) => {
    const value = Number(amount) || 0;
    const symbol = currencySymbol || '₹';
    return `${symbol}${value.toFixed(2)}`;
};

const setCartBadge = (value) => {
    const badge = document.getElementById('cartCounter');
    if (!badge) return;
    const safeValue = Number.isFinite(value) && value >= 0 ? Math.trunc(value) : 0;
    badge.textContent = safeValue.toString();
    badge.dataset.initialCount = safeValue;
};

const getCurrentBadgeCount = () => {
    const badge = document.getElementById('cartCounter');
    if (!badge) return 0;
    const val = parseInt(badge.dataset.initialCount, 10);
    return isNaN(val) ? 0 : val;
};

const refreshCartCounter = async (forceRefresh = false) => {
    const badge = document.getElementById('cartCounter');
    // Skip refresh if we have a valid server-rendered count and not forcing
    if (!forceRefresh && badge && badge.dataset.serverRendered === 'true') {
        badge.dataset.serverRendered = 'false'; // Allow future refreshes
        return;
    }
    
    const previousCount = getCurrentBadgeCount();
    try {
        const response = await fetch('/Cart/GetCartCount', { credentials: 'same-origin', cache: 'no-store' });
        if (!response.ok) {
            throw new Error(`Status ${response.status}`);
        }
        const count = await response.json();
        const newCount = Number(count);
        // Only update if we got a valid response
        if (Number.isFinite(newCount)) {
            setCartBadge(newCount);
        }
    } catch (err) {
        console.error('GetCartCount failed, keeping previous count', err);
        // Keep previous count on error - don't reset to 0
    }
};

const refreshCartSections = async (cartId, sections) => {
    if (!cartId) return;
    const config = Object.assign({ summary: true, coupons: false, shipping: false, saved: false }, sections);
    const targets = {
        summary: { element: document.getElementById('orderSummaryContainer'), url: `/Cart/OrderSummary?cartId=${cartId}` },
        coupons: { element: document.getElementById('couponsContainer'), url: `/Cart/Coupons?cartId=${cartId}` },
        shipping: { element: document.getElementById('shippingContainer'), url: `/Cart/Shipping?cartId=${cartId}` },
        saved: { element: document.getElementById('savedContainer'), url: `/Cart/Saved?cartId=${cartId}` }
    };

    const jobs = Object.entries(targets).map(async ([key, target]) => {
        if (!config[key] || !target.element) return;
        try {
            const html = await fetch(target.url, { cache: 'no-store' }).then(r => r.text());
            target.element.innerHTML = html;
        } catch (e) {
            console.warn(`Refresh ${key} failed`, e);
        }
    });

    await Promise.all(jobs);
};

const refreshCartSummaryAndCounter = (cartId, sections) => {
    if (!cartId) return Promise.resolve();
    return Promise.all([
        refreshCartSections(cartId, sections),
        refreshCartCounter(true) // Force refresh after mutations
    ]);
};

const updateHeaderCount = () => {
    const inputs = document.querySelectorAll('.quantity-input');
    const total = Array.from(inputs).reduce((sum, input) => {
        const qty = parseInt(input.value, 10);
        return sum + (isNaN(qty) ? 0 : qty);
    }, 0);
    const header = document.getElementById('cartItemCount');
    if (header) header.textContent = total;
};

const updateLineTotal = (itemId, quantity) => {
    const span = document.querySelector(`.line-total[data-item-id="${itemId}"]`);
    if (!span) return;
    const unitPrice = parseFloat(span.dataset.unitPrice);
    const currency = span.dataset.currency || '₹';
    const lineTotal = (isNaN(unitPrice) ? 0 : unitPrice * quantity);
    span.textContent = `Line Total : ${currencyFormat(lineTotal, currency)}`;
};

const wireQuantityButtons = () => {
    const root = document.getElementById('cartRoot');
    if (!root) return;

    root.addEventListener('click', async (evt) => {
        const btn = evt.target.closest('.btn-qty');
        if (!btn) return;

        evt.preventDefault();
        const delta = btn.dataset.action === 'increase' ? 1 : -1;
        const cartId = parseInt(btn.dataset.cartId, 10);
        const itemId = parseInt(btn.dataset.itemId, 10);
        const input = btn.closest('.input-group')?.querySelector('.quantity-input');
        if (!input || isNaN(cartId) || isNaN(itemId)) return;

        const current = parseInt(input.value, 10) || 0;
        const next = current + delta;
        if (next < 1) return;

        document.dispatchEvent(new Event('cart-updating'));
        try {
            const response = await fetch(`/Cart/UpdateQuantity/${itemId}/${delta}/${cartId}`, { method: 'GET' });
            const text = await response.text();
            if (parseInt(text, 10) > 0) {
                input.value = next;
                updateLineTotal(itemId, next);
                updateHeaderCount();
                    await refreshCartSummaryAndCounter(cartId, { summary: true });
            } else {
                console.warn('Quantity update returned 0');
            }
        } catch (err) {
            console.error('UpdateQuantity failed', err);
        } finally {
            document.dispatchEvent(new Event('cart-updated'));
        }
    });
};

function addToCart(ItemId, Name, UnitPrice, Quantity) {
    $.ajax({
        type: "GET",
        contentType: "application/json; charset=utf-8",
        url: '/Cart/AddToCart/' + ItemId + "/" + UnitPrice + "/" + Quantity,
        success: function (response) {
            if (response !== undefined && response.status === 'success') {
                var counter = response.count;
                setCartBadge(counter);
            }
        },
        complete: function () {
            refreshCartCounter(true); // Force refresh after add
        },
        error: function (xhr) {
            console.error('AddToCart failed', xhr.responseText);
        }
    });
}

async function deleteItem(id, cartId) {
    const itemId = parseInt(id, 10);
    const cart = parseInt(cartId, 10);
    if (!itemId || !cart) return;

    document.dispatchEvent(new Event('cart-updating'));
    try {
        const response = await fetch(`/Cart/DeleteItem/${itemId}/${cart}`, { method: 'GET', cache: 'no-store' });
        const payload = await response.text();
        if (parseInt(payload, 10) > 0) {
            const row = document.querySelector(`[data-item-row="${itemId}"]`);
            if (row) {
                row.remove();
            }
            updateHeaderCount();
            await refreshCartSummaryAndCounter(cart, { summary: true, coupons: true });
            if (!document.querySelector('.cart-line')) {
                window.location.reload();
            }
        }
    } catch (err) {
        console.error('DeleteItem failed', err);
    } finally {
        document.dispatchEvent(new Event('cart-updated'));
    }
}

async function clearCart(cartId) {
    const cart = parseInt(cartId, 10);
    if (!cart) return;

    document.dispatchEvent(new Event('cart-updating'));
    try {
        const response = await fetch(`/Cart/Clear?cartId=${cart}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            cache: 'no-store'
        });
        const result = await response.json();
        if (result.success) {
            setCartBadge(0);
            window.location.reload();
        } else {
            console.warn('Clear cart returned failure');
        }
    } catch (err) {
        console.error('ClearCart failed', err);
    } finally {
        document.dispatchEvent(new Event('cart-updated'));
    }
}

async function saveForLater(cartId, itemId) {
    const cart = parseInt(cartId, 10);
    const item = parseInt(itemId, 10);
    if (!cart || !item) return;

    document.dispatchEvent(new Event('cart-updating'));
    try {
        const response = await fetch(`/Cart/SaveForLater?cartId=${cart}&itemId=${item}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            cache: 'no-store'
        });
        const result = await response.json();
        if (result.success) {
            const row = document.querySelector(`[data-item-row="${item}"]`);
            if (row) {
                row.remove();
            }
            updateHeaderCount();
            await refreshCartSummaryAndCounter(cart, { summary: true, saved: true });
            if (!document.querySelector('.cart-line')) {
                window.location.reload();
            }
        } else {
            console.warn('Save for later returned failure');
        }
    } catch (err) {
        console.error('SaveForLater failed', err);
    } finally {
        document.dispatchEvent(new Event('cart-updated'));
    }
}

async function moveToCart(savedItemId, cartId) {
    const savedId = parseInt(savedItemId, 10);
    const cart = parseInt(cartId, 10);
    if (!savedId) return;

    document.dispatchEvent(new Event('cart-updating'));
    try {
        const response = await fetch(`/Cart/MoveSavedToCart?savedItemId=${savedId}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            cache: 'no-store'
        });
        const result = await response.json();
        if (result.success) {
            window.location.reload();
        } else {
            console.warn('Move to cart returned failure');
        }
    } catch (err) {
        console.error('MoveSavedToCart failed', err);
    } finally {
        document.dispatchEvent(new Event('cart-updated'));
    }
}

document.addEventListener('DOMContentLoaded', () => {
    const badge = document.getElementById('cartCounter');
    if (badge) {
        // Mark as server-rendered to skip immediate AJAX refresh
        if (badge.dataset.initialCount) {
            badge.dataset.serverRendered = 'true';
            setCartBadge(Number(badge.dataset.initialCount));
        }
    }
    // Don't call refreshCartCounter here - trust the server-rendered value
    // Counter will refresh naturally after any cart mutation
    wireQuantityButtons();
});

(function () {
    window.cartMutations = {
        addItem: async (itemId, unitPrice, qty, userId, cartId) => {
            await fetch(`/Cart/AddToCart/${itemId}/${unitPrice}/${qty}`, { method: 'GET' });
            await refreshCartSections(cartId, { summary: true, coupons: true, shipping: true, saved: true });
            await refreshCartCounter(true);
        },
        updateQuantity: async (cartId, itemId, delta) => {
            await fetch(`/Cart/UpdateQuantity/${itemId}/${delta}/${cartId}`, { method: 'GET' });
            await refreshCartSections(cartId, { summary: true });
            await refreshCartCounter(true);
        },
        deleteItem: async (cartId, itemId) => {
            await fetch(`/Cart/DeleteItem/${itemId}/${cartId}`, { method: 'GET' });
            await refreshCartSections(cartId, { summary: true, coupons: true });
            await refreshCartCounter(true);
        },
        applyCoupon: async (cartId, code, amount) => {
            const form = new FormData();
            form.append("cartId", cartId);
            form.append("code", code);
            form.append("amount", amount);
            await fetch(`/Cart/ApplyCoupon?cartId=${cartId}&code=${encodeURIComponent(code)}&amount=${amount}`, { method: 'POST', body: form });
            await refreshCartSections(cartId, { summary: true, coupons: true });
        },
        clearCart: async (cartId) => {
            await clearCart(cartId);
        },
        saveForLater: async (cartId, itemId) => {
            await saveForLater(cartId, itemId);
        },
        moveToCart: async (savedItemId, cartId) => {
            await moveToCart(savedItemId, cartId);
        }
    };
})();
