function addToCart(ItemId, Name, UnitPrice, Quantity) {
    $.ajax({
        type: "GET",
        contentType: "application/json; charset=utf-8",
        url: '/Cart/AddToCart/' + ItemId + "/" + UnitPrice + "/" + Quantity,
        success: function (response) {
            if (response != undefined && response.status === 'success') {
                var counter = response.count;
                $("#cartCounter").text(counter);
            }
        },
        error: function (xhr) {
            console.error('AddToCart failed', xhr.responseText);
        }
    });
}
function deleteItem(id, cartId) {
    id = parseInt(id, 10);
    cartId = parseInt(cartId, 10);
    if (id > 0 && cartId > 0) {
        $.ajax({
            type: "GET",
            url: '/Cart/DeleteItem/' + id + "/" + cartId,
            success: function (data) {
                if (parseInt(data, 10) > 0) {
                    location.reload();
                }
            },
            error: function (xhr) {
                console.error('DeleteItem failed', xhr.responseText);
            }
        });
    }
}
function updateQuantity(id, currentQuantity, quantity, cartId) {
    id = parseInt(id, 10);
    currentQuantity = parseInt(currentQuantity, 10);
    quantity = parseInt(quantity, 10);
    cartId = parseInt(cartId, 10);
    if ((currentQuantity >= 1 && quantity === 1) || (currentQuantity > 1 && quantity === -1)) {
        $.ajax({
            url: '/Cart/UpdateQuantity/' + id + "/" + quantity + "/" + cartId,
            type: 'GET',
            success: function (response) {
                if (parseInt(response, 10) > 0) {
                    location.reload();
                }
            },
            error: function (xhr) {
                console.error('UpdateQuantity failed', xhr.responseText);
            }
        });
    }
}

$(document).ready(function () {
    $.ajax({
        type: "GET",
        contentType: "application/json; charset=utf-8",
        url: '/Cart/GetCartCount',
        success: function (data) {
            $("#cartCounter").text(data);
        },
        error: function (xhr) {
            console.error('GetCartCount failed', xhr.responseText);
        },
    });
});

(function () {
    const refreshSections = async (cartId) => {
        try {
            const [summaryHtml, couponsHtml, shippingHtml, savedHtml] = await Promise.all([
                fetch(`/Cart/OrderSummary?cartId=${cartId}`).then(r => r.text()),
                fetch(`/Cart/Coupons?cartId=${cartId}`).then(r => r.text()),
                fetch(`/Cart/Shipping?cartId=${cartId}`).then(r => r.text()),
                fetch(`/Cart/Saved?cartId=${cartId}`).then(r => r.text())
            ]);

            const summary = document.getElementById("orderSummaryContainer");
            const coupons = document.getElementById("couponsContainer");
            const shipping = document.getElementById("shippingContainer");
            const saved = document.getElementById("savedContainer");

            if (summary) summary.innerHTML = summaryHtml;
            if (coupons) coupons.innerHTML = couponsHtml;
            if (shipping) shipping.innerHTML = shippingHtml;
            if (saved) saved.innerHTML = savedHtml;
        } catch (e) {
            console.warn("Refresh failed", e);
        }
    };

    // Example hooks for existing buttons (ensure these functions exist or wire them)
    window.cartMutations = {
        addItem: async (itemId, unitPrice, qty, userId, cartId) => {
            await fetch(`/Cart/AddToCart/${itemId}/${unitPrice}/${qty}`, { method: 'GET' });
            await refreshSections(cartId);
        },
        updateQuantity: async (cartId, itemId, delta) => {
            await fetch(`/Cart/UpdateQuantity/${itemId}/${delta}/${cartId}`, { method: 'GET' });
            await refreshSections(cartId);
        },
        deleteItem: async (cartId, itemId) => {
            await fetch(`/Cart/DeleteItem/${itemId}/${cartId}`, { method: 'GET' });
            await refreshSections(cartId);
        },
        applyCoupon: async (cartId, code, amount) => {
            const form = new FormData();
            form.append("cartId", cartId);
            form.append("code", code);
            form.append("amount", amount);
            await fetch(`/Cart/ApplyCoupon?cartId=${cartId}&code=${encodeURIComponent(code)}&amount=${amount}`, { method: 'POST', body: form });
            await refreshSections(cartId);
        }
    };
})();