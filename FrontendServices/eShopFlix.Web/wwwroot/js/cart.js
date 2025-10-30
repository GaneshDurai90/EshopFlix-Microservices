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

// ===== New feature JS helpers =====
function refreshOrderSummary(cartId) {
    $("#orderSummaryContainer").load('/Cart/OrderSummary?cartId=' + cartId);
}
function refreshCoupons(cartId) {
    $("#couponsContainer").load('/Cart/Coupons?cartId=' + cartId);
}
function refreshShipping(cartId) {
    $("#shippingContainer").load('/Cart/Shipping?cartId=' + cartId);
}
function refreshSaved(cartId) {
    $("#savedContainer").load('/Cart/Saved?cartId=' + cartId);
}

function applyCoupon(cartId) {
    var code = $('#couponCode').val();
    var amount = parseFloat($('#couponAmount').val() || '0');
    $.post('/Cart/ApplyCoupon', { cartId: cartId, code: code, amount: amount })
        .done(function () {
            refreshCoupons(cartId);
            refreshOrderSummary(cartId);
        })
        .fail(function (xhr) { console.error('applyCoupon failed', xhr.responseText); });
}
function removeCoupon(cartId, code) {
    $.post('/Cart/RemoveCoupon', { cartId: cartId, code: code })
        .done(function () { refreshCoupons(cartId); refreshOrderSummary(cartId); })
        .fail(function (xhr) { console.error('removeCoupon failed', xhr.responseText); });
}
function selectShipping(cartId, methodCode) {
    var row = $("button[onclick*='" + methodCode + "']");
    var costText = row.find('span').last().text().replace('₹','');
    var cost = parseFloat(costText) || 0;
    var carrier = row.text().trim().split(' - ')[0];
    var methodName = row.text().trim();
    $.post('/Cart/SelectShipping', { cartId: cartId, Carrier: carrier, MethodCode: methodCode, MethodName: methodName, Cost: cost })
        .done(function () { refreshShipping(cartId); refreshOrderSummary(cartId); })
        .fail(function (xhr) { console.error('selectShipping failed', xhr.responseText); });
}
function clearCart(cartId) {
    $.post('/Cart/Clear', { cartId: cartId })
        .done(function () { location.reload(); })
        .fail(function (xhr) { console.error('clearCart failed', xhr.responseText); });
}

function saveForLater(cartId, itemId) {
    $.post('/Cart/SaveForLater', { cartId: cartId, itemId: itemId })
        .done(function () { refreshSaved(cartId); location.reload(); })
        .fail(function (xhr) { console.error('saveForLater failed', xhr.responseText); });
}
function moveSavedToCart(savedItemId) {
    $.post('/Cart/MoveSavedToCart', { savedItemId: savedItemId })
        .done(function () { location.reload(); })
        .fail(function (xhr) { console.error('moveSavedToCart failed', xhr.responseText); });
}