using eShopFlix.Web.Helpers;
using eShopFlix.Web.HttpClients;
using eShopFlix.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace eShopFlix.Web.Controllers
{
    public class CartController : BaseController
    {
        CartServiceClient _cartServiceClient;
        public CartController(CartServiceClient cartServiceClient)
        {
            _cartServiceClient = cartServiceClient;
        }

        public async Task<IActionResult> Index()
        {
            if (CurrentUser != null)
            {
                CartModel cartModel = await _cartServiceClient.GetUserCartAsync(CurrentUser.UserId);
                if (cartModel != null)
                {
                    // Preload totals/coupons/shipments/saved for initial render
                    ViewBag.Totals = await _cartServiceClient.GetTotalsAsync(cartModel.Id);
                    ViewBag.Coupons = await _cartServiceClient.GetCouponsAsync(cartModel.Id) ?? new List<CouponModel>();
                    ViewBag.Shipments = await _cartServiceClient.GetShipmentsAsync(cartModel.Id) ?? new List<ShipmentModel>();
                    ViewBag.Saved = await _cartServiceClient.GetSavedForLaterAsync(cartModel.Id) ?? new List<SavedForLaterItemModel>();
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
            CartItemModel cartItemModel = new CartItemModel
            {
                ItemId = ItemId,
                Quantity = Quantity,
                UnitPrice = UnitPrice
            };

            CartModel cartModel = await _cartServiceClient.AddToCartAsync(cartItemModel, CurrentUser.UserId);
            if (cartModel != null)
            {
                return Json(new { status = "success", count = cartModel.CartItems.Count });
            }
            return Json(new { status = "failed", count = 0 });
        }

        [Route("Cart/UpdateQuantity/{Id}/{Quantity}/{CartId}")]
        public async Task<IActionResult> UpdateQuantity(int Id, int Quantity, long CartId)
        {
            int count = await _cartServiceClient.UpdateQuantity(CartId, Id, Quantity);
            return Json(count);
        }

        [Route("Cart/DeleteItem/{Id}/{CartId}")]
        public async Task<IActionResult> DeleteItem(int Id, long CartId)
        {
            int count =  await _cartServiceClient.DeleteCartItemAsync(CartId, Id);
            return Json(count);
        }

        public async Task<IActionResult> GetCartCount()
        {
            if (CurrentUser != null)
            {
                var count =  await _cartServiceClient.GetCartItemCount(CurrentUser.UserId);
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
        public async Task<IActionResult> Clear(long cartId)
        {
            var ok = await _cartServiceClient.ClearAsync(cartId);
            return Json(new { success = ok });
        }

        [HttpPost]
        public async Task<IActionResult> SaveForLater(long cartId, int itemId)
        {
            var ok = await _cartServiceClient.SaveForLaterAsync(cartId, itemId);
            return Json(new { success = ok });
        }

        [HttpPost]
        public async Task<IActionResult> MoveSavedToCart(int savedItemId)
        {
            var ok = await _cartServiceClient.MoveSavedToCartAsync(savedItemId);
            return Json(new { success = ok });
        }
    }
}