using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using BulkyBookWeb.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
	[Area("Customer")]
	[Authorize]
	public class CartController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<OrderHub> _orderHub;
        public CartController(IUnitOfWork unitOfWork, IHubContext<OrderHub> OrderHub)
		{
			_unitOfWork = unitOfWork;
			_orderHub = OrderHub;
		}

		public IActionResult Index()
		{

			var claimsIdentty = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentty.FindFirst(ClaimTypes.NameIdentifier).Value;
			ShoppingCartVM shoppingCartVM = new()
			{
				ShoppingCartList = _unitOfWork.ShoppingCartRepository.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
				OrderHeader = new OrderHeader()
			};
			foreach (var cart in shoppingCartVM.ShoppingCartList)
			{
                cart.Product.ProductImages = _unitOfWork.ProductImageRepository.GetAll(u => u.ProductId == cart.ProductId).ToList();
                cart.Price = GetPriceBasedOnQuantity(cart);
				shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}

			return View(shoppingCartVM);
		}
		public IActionResult Plus(int cartId)
		{
			ShoppingCart cart = _unitOfWork.ShoppingCartRepository.Get(c => c.Id == cartId);

			cart.Count += 1;

			_unitOfWork.ShoppingCartRepository.Update(cart);
			_unitOfWork.Save();

			return RedirectToAction(nameof(Index));
		}

		public IActionResult Minus(int cartId)
		{
			ShoppingCart cart = _unitOfWork.ShoppingCartRepository.Get(c => c.Id == cartId);

			if (cart.Count == 1)
			{
				_unitOfWork.ShoppingCartRepository.Remove(cart);
                var count = _unitOfWork.ShoppingCartRepository.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).Count() - 1;
                HttpContext.Session.SetInt32(SD.SessionCart, count);
            }
			else
			{
				cart.Count -= 1;

				_unitOfWork.ShoppingCartRepository.Update(cart);
			}
			_unitOfWork.Save();

			return RedirectToAction(nameof(Index));
		}

		public IActionResult Remove(int cartId)
		{
			ShoppingCart cart = _unitOfWork.ShoppingCartRepository.Get(c => c.Id == cartId);

			_unitOfWork.ShoppingCartRepository.Remove(cart);
			_unitOfWork.Save();

            var count = _unitOfWork.ShoppingCartRepository.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).Count();
            HttpContext.Session.SetInt32(SD.SessionCart, count);
            return RedirectToAction(nameof(Index));
		}
		public IActionResult Summary()
		{
			var claimsIdentty = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentty.FindFirst(ClaimTypes.NameIdentifier).Value;
            var otherClaims = claimsIdentty.Claims.Where(c => c.Type != ClaimTypes.NameIdentifier);

            foreach (var claim in otherClaims)
            {
                // Access the claim properties if needed
                var claimType = claim.Type;
                var claimValue = claim.Value;

                // Do something with the claim...
            }

            var shoppingCartVM = new ShoppingCartVM()
			{
				OrderHeader = new OrderHeader(),
				ShoppingCartList = _unitOfWork.ShoppingCartRepository.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product")
			};

			shoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUserRepository.Get(c => c.Id == userId);

			shoppingCartVM.OrderHeader.Name = shoppingCartVM.OrderHeader.ApplicationUser.Name;
			shoppingCartVM.OrderHeader.PhoneNumber = shoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
			shoppingCartVM.OrderHeader.StreetAddress = shoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
			shoppingCartVM.OrderHeader.City = shoppingCartVM.OrderHeader.ApplicationUser.City;
			shoppingCartVM.OrderHeader.State = shoppingCartVM.OrderHeader.ApplicationUser.State;
			shoppingCartVM.OrderHeader.PostalCode = shoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

			foreach (var cart in shoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}

			return View(shoppingCartVM);
		}


		[HttpPost]
		[ActionName("Summary")]
		[ValidateAntiForgeryToken]
		public IActionResult SummaryPost(ShoppingCartVM shoppingCartVM)
		{
			var claimsIdentty = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentty.FindFirst(ClaimTypes.NameIdentifier).Value;

			shoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUserRepository.Get(c => c.Id == userId);

			shoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCartRepository.GetAll(c => c.ApplicationUserId == userId, includeProperties: "Product");



			shoppingCartVM.OrderHeader.ApplicationUserId = userId;
			shoppingCartVM.OrderHeader.OrderDate = DateTime.Now;

			foreach (var cart in shoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				shoppingCartVM.OrderHeader.OrderTotal += cart.Count * cart.Price;
			}

			if (shoppingCartVM.OrderHeader.ApplicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
				shoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
			}
			else
			{
				shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
				shoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
			}
			_unitOfWork.OrderHeaderRepository.Add(shoppingCartVM.OrderHeader);
			_unitOfWork.Save();

			foreach (var item in shoppingCartVM.ShoppingCartList)
			{
				item.Price = GetPriceBasedOnQuantity(item);
				shoppingCartVM.OrderHeader.OrderTotal += item.Count * item.Price;
				OrderDetail orderDetails = new()
				{
					ProductId = item.ProductId,
					OrderId = shoppingCartVM.OrderHeader.Id,
					Price = item.Price,
					Count = item.Count
				};

				_unitOfWork.OrderDetailRepository.Add(orderDetails);
				_unitOfWork.Save();
			}
			if(shoppingCartVM.OrderHeader.ApplicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				var domain = "http://localhost:5093/";
				var options = new SessionCreateOptions
				{
					SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={shoppingCartVM.OrderHeader.Id}",
					LineItems = new List<SessionLineItemOptions>(),
					Mode = "payment",
				};

				foreach (var item in shoppingCartVM.ShoppingCartList)
				{
					var sessionLineItem = new SessionLineItemOptions
					{
						PriceData = new SessionLineItemPriceDataOptions
						{
							UnitAmount = (long)(item.Price * 100),
							Currency = "usd",
							ProductData = new SessionLineItemPriceDataProductDataOptions
							{
								Name = item.Product.Title
							}
						},
						Quantity = item.Count
					};
					options.LineItems.Add(sessionLineItem);
				}

				var service = new SessionService();
				Session session = service.Create(options);
				_unitOfWork.OrderHeaderRepository.UpdateStripePaymentID(shoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
				_unitOfWork.Save();
				Response.Headers.Add("Location", session.Url);
				return new StatusCodeResult(303);
			}
			else
			{
				return RedirectToAction("OrderConfirmation", new { id = shoppingCartVM.OrderHeader.Id });
			}
}

		public async Task<IActionResult> OrderConfirmation(int id)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeaderRepository.Get(u => u.Id == id);

			if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
			{
				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);
				//check the stripe status
				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeaderRepository.UpdateStripePaymentID(id, orderHeader.SessionId, session.PaymentIntentId);
					_unitOfWork.OrderHeaderRepository.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
					_unitOfWork.Save();
				}
			}

			List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCartRepository.GetAll(u => u.ApplicationUserId ==
			orderHeader.ApplicationUserId).ToList();

			_unitOfWork.ShoppingCartRepository.RemoveRange(shoppingCarts);
			_unitOfWork.Save();
			await _orderHub.Clients.All.SendAsync("MakeOrder");
			HttpContext.Session.Clear();
            return View(id);
		}


		private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
		{
			if (shoppingCart.Count <= 50)
			{
				return shoppingCart.Product.Price;
			}
			else
			{
				if (shoppingCart.Count <= 100)
				{
					return shoppingCart.Product.Price50;
				}
				else
				{
					return shoppingCart.Product.Price100;
				}
			}
		}
	}
}
