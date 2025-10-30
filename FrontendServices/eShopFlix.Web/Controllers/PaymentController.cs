using eShopFlix.Web.Helpers;
using eShopFlix.Web.HttpClients;
using eShopFlix.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace eShopFlix.Web.Controllers
{
    public class PaymentController : BaseController
    {
        private readonly CartServiceClient _cartServiceClient;
        private readonly PaymentServiceClient _paymentServiceClient;
        private readonly IConfiguration _configuration;
        public PaymentController(CartServiceClient cartServiceClient, PaymentServiceClient paymentServiceClient, IConfiguration configuration)
        {
            _cartServiceClient = cartServiceClient;
            _paymentServiceClient = paymentServiceClient;
            _configuration = configuration;
        }
        public async Task<IActionResult> Index()
        {
            if (CurrentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var cartModel = await _cartServiceClient.GetUserCartAsync(CurrentUser.UserId);
            if (cartModel != null)
            {
                var payment = new PaymentModel
                {
                    Cart = cartModel,
                    Currency = "INR",
                    Description = string.Join(",", cartModel.CartItems.Select(x => x.Name)),
                    GrandTotal = cartModel.GrandTotal,
                    RazorpayKey = _configuration["RazorPay:Key"]
                };
                var razorpayOrder = new RazorPayOrderModel
                {
                    Amount = Convert.ToInt32(payment.GrandTotal * 100),
                    Currency = payment.Currency,
                    Receipt = Guid.NewGuid().ToString()
                };
                payment.OrderId = await _paymentServiceClient.CreateOrderAsync(razorpayOrder) ?? string.Empty;
                payment.Receipt = razorpayOrder.Receipt; // ensure we round-trip the receipt to the Status action
                return View(payment);

            }
            return RedirectToAction("Index", "Cart");
        }

        public async Task<IActionResult> Status(IFormCollection form)
        {
            if (!string.IsNullOrEmpty(form["rzp_paymentid"]))
            {
                string paymentId = form["rzp_paymentid"]!;
                string orderId = form["rzp_orderid"]!;
                string signature = form["rzp_signature"]!;
                string transactionId = form["Receipt"]!;
                string currency = form["Currency"]!;

                var payment = new PaymentConfirmModel
                {
                    PaymentId = paymentId,
                    OrderId = orderId,
                    Signature = signature
                };
                string status = await _paymentServiceClient.VerifyPaymentAsync(payment);
                if (status == "captured" || status == "completed")
                {
                    var cart = await _cartServiceClient.GetUserCartAsync(CurrentUser!.UserId);
                    if (cart == null)
                    {
                        ViewBag.Message = "Cart not found after payment. Please contact support with your payment id.";
                        return View();
                    }
                    var model = new TransactionModel
                    {
                        CartId = cart.Id,
                        Total = cart.Total,
                        Tax = cart.Tax,
                        GrandTotal = cart.GrandTotal,
                        CreatedDate = DateTime.Now,
                        Status = status,
                        TransactionId = transactionId,
                        Currency = currency,
                        Email = CurrentUser.Email,
                        Id = paymentId,
                        UserId = CurrentUser.UserId
                    };

                    bool result = await _paymentServiceClient.SavePaymentDetailsAsync(model);
                    if (result)
                    {
                        await _cartServiceClient.MakeCartInActiveAsync(cart.Id);
                        TempData.Set("Receipt", model);
                        return RedirectToAction("Receipt");
                    }
                }
            }
            ViewBag.Message = "Due to some technical issues we are not able to receive order details. We will contact you soon..";
            return View();
        }

        public IActionResult Receipt()
        {
            TransactionModel model = TempData.Get<TransactionModel>("Receipt");
            return View(model);
        }
    }
}
