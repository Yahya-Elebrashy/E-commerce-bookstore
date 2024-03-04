using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger , IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> products = _unitOfWork.ProductRepository.GetAll(includeProperties: "Category,ProductImages");
            return View(products);
        }
        public IActionResult Details(int productId)
        {
            ShoppingCart cart = new()
            {
                Product = _unitOfWork.ProductRepository.Get(u => u.Id == productId, includeProperties: "Category,ProductImages"),
                ProductId = productId,
                Count = 1
            };

            return View(cart);
        }

        [HttpPost]
        [Authorize] // User must be logged in
        public IActionResult Details(ShoppingCart shoppingCart)
        {      
                var claimsIdentty = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentty.FindFirst(ClaimTypes.NameIdentifier);
                shoppingCart.ApplicationUserId = claim.Value;

                var cartFromDb = _unitOfWork.ShoppingCartRepository.Get(
                    u => u.ApplicationUserId == shoppingCart.ApplicationUserId && u.ProductId == shoppingCart.ProductId);

                if (cartFromDb != null)
                {                  
                    // Shopping Cart exists
                    cartFromDb.Count += shoppingCart.Count;

                    // Update Count in DB.
                    _unitOfWork.ShoppingCartRepository.Update(cartFromDb);
                    _unitOfWork.Save();

            }
            else
                {
                    // Adding cart to DB
                    _unitOfWork.ShoppingCartRepository.Add(shoppingCart);
                    _unitOfWork.Save();

                    var count = _unitOfWork.ShoppingCartRepository.GetAll(u => u.ApplicationUserId == claim.Value).Count();
                    HttpContext.Session.SetInt32(SD.SessionCart, count);
            }

            return RedirectToAction("Index");

        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}