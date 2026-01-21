using System.Linq;
using System.Threading.Tasks;
using eShopFlix.Web.Helpers;
using eShopFlix.Web.HttpClients;
using eShopFlix.Web.Models;
using eShopFlix.Web.Models.Stock;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace eShopFlix.Web.Controllers
{
    public class CartController : BaseController
    {
        private readonly CartServiceClient _cartServiceClient;
        private readonly StockServiceClient _stockServiceClient;
        private readonly ILogger<CartController> _logger;

        public CartController(
            CartServiceClient cartServiceClient, 
            StockServiceClient stockServiceClient,
            ILogger<CartController> logger)
        {
            _cartServiceClient = cartServiceClient;
            _stockServiceClient = stockServiceClient;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            if (CurrentUser != null)
            {
                CartModel cartModel = await _cartServiceClient.GetUserCartAsync(CurrentUser.UserId);
                if (cartModel != null)
                {
                    // Preload auxiliary data in parallel so the page renders faster
                    var totalsTask = _cartServiceClient.GetTotalsAsync(cartModel.Id);
                    var couponsTask = _cartServiceClient.GetCouponsAsync(cartModel.Id);
                    var shipmentsTask = _cartServiceClient.GetShipmentsAsync(cartModel.Id);
                    var savedTask = _cartServiceClient.GetSavedForLaterAsync(cartModel.Id);
                    var reservationsTask = _stockServiceClient.GetCartReservationsAsync(cartModel.Id);

                    await Task.WhenAll(totalsTask, couponsTask, shipmentsTask, savedTask, reservationsTask);

                    ViewBag.Totals = await totalsTask;
                    ViewBag.Coupons = (await couponsTask) ?? new List<CouponModel>();
                    ViewBag.Shipments = (await shipmentsTask) ?? new List<ShipmentModel>();
                    ViewBag.Saved = (await savedTask) ?? new List<SavedForLaterItemModel>();
                    ViewBag.Reservations = await reservationsTask;
                }
                return View(cartModel);
            }
            else
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/" });
            }
        }

        [Route("Cart/AddToCart/{ItemId}/{UnitPrice}/{Quantity}")]
        public async Task<IActionResult> AddToCart(int ItemId, decimal UnitPrice, int Quantity)
        {
            if (CurrentUser == null || CurrentUser.UserId <= 0)
            {
                _logger.LogWarning("AddToCart failed: User not authenticated");
                return Json(new { status = "failed", count = 0, error = "User not authenticated" });
            }

            _logger.LogInformation("AddToCart: User {UserId}, ItemId {ItemId}, Price {Price}, Qty {Qty}",
                CurrentUser.UserId, ItemId, UnitPrice, Quantity);

            try
            {
                // Stock check is optional - if it fails or times out, we still add to cart
                // Stock will be validated at checkout
                CheckAvailabilityResultModel? availability = null;
                
                try
                {
                    // Use a separate timeout for stock check - don't let it block cart add
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    availability = await _stockServiceClient.CheckAvailabilityAsync(ItemId, Quantity, ct: cts.Token);
                    _logger.LogDebug("Stock check result for ItemId {ItemId}: Available={Available}", 
                        ItemId, availability?.IsAvailable);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("Stock check timed out for ItemId {ItemId}, proceeding without check", ItemId);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Stock check failed for ItemId {ItemId}, proceeding without check", ItemId);
                }

                // Only block if we got a definitive "not available" response
                if (availability is { IsAvailable: false })
                {
                    _logger.LogInformation("AddToCart blocked: Insufficient stock for ItemId {ItemId}", ItemId);
                    return Json(new 
                    { 
                        status = "failed", 
                        count = 0, 
                        error = $"Insufficient stock. Only {availability.AvailableQuantity} available.",
                        availableQuantity = availability.AvailableQuantity
                    });
                }

                // Add to cart - this is the primary operation
                CartItemModel cartItemModel = new CartItemModel
                {
                    ItemId = ItemId,
                    Quantity = Quantity,
                    UnitPrice = UnitPrice
                };

                CartModel? cartModel = await _cartServiceClient.AddToCartAsync(cartItemModel, CurrentUser.UserId);
                
                if (cartModel == null)
                {
                    _logger.LogWarning("AddToCart failed: CartService returned null for User {UserId}, ItemId {ItemId}", 
                        CurrentUser.UserId, ItemId);
                    return Json(new { status = "failed", count = 0, error = "Cart service returned null. Check CartService logs." });
                }

                _logger.LogInformation("AddToCart success: CartId {CartId}, ItemCount {Count}", 
                    cartModel.Id, cartModel.CartItems?.Count ?? 0);

                // Reserve stock in background - don't block the response
                var cartId = cartModel.Id;
                var userId = CurrentUser.UserId;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _stockServiceClient.ReserveStockAsync(ItemId, Quantity, cartId, userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Stock reservation failed for CartId {CartId}, ItemId {ItemId}", cartId, ItemId);
                    }
                });

                var count = CalculateQuantity(cartModel);
                return Json(new { status = "success", count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddToCart exception for User {UserId}, ItemId {ItemId}", CurrentUser.UserId, ItemId);
                return Json(new { status = "failed", count = 0, error = $"Exception: {ex.Message}" });
            }
        }

        [Route("Cart/UpdateQuantity/{Id}/{Quantity}/{CartId}")]
        public async Task<IActionResult> UpdateQuantity(int Id, int Quantity, long CartId)
        {
            // Check stock availability before updating
            var availability = await _stockServiceClient.CheckAvailabilityAsync(Id, Quantity);
            if (availability != null && !availability.IsAvailable)
            {
                return Json(new 
                { 
                    error = $"Only {availability.AvailableQuantity} items available",
                    maxQuantity = availability.AvailableQuantity 
                });
            }

            int count = await _cartServiceClient.UpdateQuantity(CartId, Id, Quantity);
            return Json(count);
        }

        [Route("Cart/DeleteItem/{Id}/{CartId}")]
        public async Task<IActionResult> DeleteItem(int Id, long CartId)
        {
            int count = await _cartServiceClient.DeleteCartItemAsync(CartId, Id);
            return Json(count);
        }

        public async Task<IActionResult> GetCartCount()
        {
            if (CurrentUser != null && CurrentUser.UserId > 0)
            {
                var count = await _cartServiceClient.GetCartItemCount(CurrentUser.UserId);
                return Json(count);
            }
            return Json(0);
        }

        public IActionResult Checkout()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Checkout(AddressModel model)
        {
            if (ModelState.IsValid)
            {
                TempData.Set("Address", model);
                return RedirectToAction("Index", "Payment");
            }
            return View();
        }

        // ===== New UI endpoints for partial sections =====
        [HttpGet]
        public async Task<IActionResult> OrderSummary(long cartId)
        {
            var totals = await _cartServiceClient.GetTotalsAsync(cartId);
            return PartialView("~/Views/Cart/Partials/_OrderSummaryPartial.cshtml", totals);
        }

        [HttpGet]
        public async Task<IActionResult> Coupons(long cartId)
        {
            var coupons = await _cartServiceClient.GetCouponsAsync(cartId) ?? new List<CouponModel>();
            ViewBag.CartId = cartId;
            return PartialView("~/Views/Cart/Partials/_CouponsPartial.cshtml", coupons);
        }

        [HttpGet]
        public async Task<IActionResult> Shipping(long cartId)
        {
            var shipments = await _cartServiceClient.GetShipmentsAsync(cartId) ?? new List<ShipmentModel>();
            ViewBag.CartId = cartId;
            return PartialView("~/Views/Cart/Partials/_ShippingPartial.cshtml", shipments);
        }

        [HttpGet]
        public async Task<IActionResult> Saved(long cartId)
        {
            var saved = await _cartServiceClient.GetSavedForLaterAsync(cartId) ?? new List<SavedForLaterItemModel>();
            ViewBag.CartId = cartId;
            return PartialView("~/Views/Cart/Partials/_SavedForLaterPartial.cshtml", saved);
        }

        // ===== Mutations =====
        [HttpPost]
        public async Task<IActionResult> ApplyCoupon(long cartId, string code, decimal amount)
        {
            var ok = await _cartServiceClient.ApplyCouponAsync(cartId, code, amount);
            return Json(new { success = ok });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveCoupon(long cartId, string code)
        {
            var ok = await _cartServiceClient.RemoveCouponAsync(cartId, code);
            return Json(new { success = ok });
        }

        [HttpPost]
        public async Task<IActionResult> SelectShipping(long cartId, ShipmentModel model)
        {
            var ok = await _cartServiceClient.SelectShippingAsync(cartId, model);
            return Json(new { success = ok });
        }

        [HttpPost]
        public async Task<IActionResult> Clear([FromQuery] long cartId)
        {
            var ok = await _cartServiceClient.ClearAsync(cartId);
            return Json(new { success = ok });
        }

        [HttpPost]
        public async Task<IActionResult> SaveForLater([FromQuery] long cartId, [FromQuery] int itemId)
        {
            var ok = await _cartServiceClient.SaveForLaterAsync(cartId, itemId);
            return Json(new { success = ok });
        }

        [HttpPost]
        public async Task<IActionResult> MoveSavedToCart([FromQuery] int savedItemId)
        {
            var ok = await _cartServiceClient.MoveSavedToCartAsync(savedItemId);
            return Json(new { success = ok });
        }

        [HttpGet]
        public IActionResult AddItem() => View();

        [HttpPost]
        public async Task<IActionResult> AddItem(CartItemModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var cart = await _cartServiceClient.AddToCartAsync(model, CurrentUser.UserId);
            if (cart is null) return View(model);
            return RedirectToAction(nameof(Index));
        }

        private static int CalculateQuantity(CartModel? cart)
            => cart?.CartItems?.Sum(i => i.Quantity) ?? 0;
    }
}