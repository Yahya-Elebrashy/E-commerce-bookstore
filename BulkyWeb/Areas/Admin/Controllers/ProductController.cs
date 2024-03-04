using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using BulkyBookWeb.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        public readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHubContext<ProductHub> _productHub;
        public ProductController(IUnitOfWork unitOfWork , IWebHostEnvironment webHostEnvironment, IHubContext<ProductHub> ProductHub)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _productHub = ProductHub;
        }

        public IActionResult Index()
        {
            var objProductList = _unitOfWork.ProductRepository.GetAll(includeProperties: "Category").ToList();
            return View(objProductList);
        }
        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new ProductVM()
            {
                CategoryList = _unitOfWork.CategoryRepository.GetAll().Select(u =>
                new SelectListItem() { Text = u.Name, Value = u.Id.ToString() }
                ),
                Product = new Product()
            };
            if (id == null || id ==0)
                //Add
                return View(productVM);
            else
            {
                //Update
                productVM.Product = _unitOfWork.ProductRepository.Get(p => p.Id == id, includeProperties: "ProductImages");
                return View(productVM);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Upsert(ProductVM productVM , List<IFormFile> files)
        {
            if (productVM.Product.CategoryId == 0)
            {
                ModelState.AddModelError("Product.CategoryId", "The CategoryId is required");
            }
            if (ModelState.IsValid)
            {
                if (productVM.Product.Id == 0)
                    _unitOfWork.ProductRepository.Add(productVM.Product);
                else
                    _unitOfWork.ProductRepository.Update(productVM.Product);

                _unitOfWork.Save();

                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (files is not null)
                {
                    foreach (IFormFile file in files)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string productPath = @"images\product\product-" + productVM.Product.Id;
                        string finalPath = Path.Combine(wwwRootPath, productPath);

                        if (!Directory.Exists(finalPath))
                        {
                            Directory.CreateDirectory(finalPath);
                        }

                        using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                        {
                            file.CopyTo(fileStream);

                        }

                        ProductImage productImage = new ProductImage()
                        {
                            ImageUrl = @"\" + productPath + @"\" + fileName,
                            ProductId = productVM.Product.Id
                        };

                        if (productVM.Product.ProductImages == null)
                            productVM.Product.ProductImages = new List<ProductImage>();

                        productVM.Product.ProductImages.Add(productImage);
                    }

                    _unitOfWork.ProductRepository.Update(productVM.Product);
                    _unitOfWork.Save();
                }
                await _productHub.Clients.All.SendAsync("MakeProduct");
                return RedirectToAction("Index");
            }
            else
            {
                productVM.CategoryList = _unitOfWork.CategoryRepository.GetAll().Select(u =>
                new SelectListItem() { Text = u.Name, Value = u.Id.ToString() }
                );
                return View(productVM);
            }
        }

        public IActionResult DeleteImage(int imageId)
        {
            var imageToBeDeleted = _unitOfWork.ProductImageRepository.Get(pi =>pi.Id == imageId);
            var productId = imageToBeDeleted.ProductId;
            if (imageToBeDeleted != null)
            {
                if (!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl))
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageToBeDeleted.ImageUrl.TrimStart('\\'));
                    if(System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }
                _unitOfWork.ProductImageRepository.Remove(imageToBeDeleted);
                _unitOfWork.Save();
            }
            return RedirectToAction(nameof(Upsert), new { id = productId});
        }


        #region API Calls
        [HttpGet]
        public IActionResult GetAll()
        {
            var objProductList = _unitOfWork.ProductRepository.GetAll(includeProperties: "Category").ToList();
            return Json(new { Data = objProductList});
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            if (id is null || id <= 0) return Json(new {Success = false , message = "Error While deleting"});            
            var productToBeDeleted = _unitOfWork.ProductRepository.Get(u => u.Id == id);
            if (productToBeDeleted is null) Json(new { Success = false, message = "Error While deleting" });
            //delete the old images
            string productPath = @"images\product\product-" + id;
            string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);

            if(Directory.Exists(finalPath))
            {
                string[] filePaths = Directory.GetFiles(finalPath);
                foreach (string filePath in filePaths)
                {
                    System.IO.File.Delete(filePath);
                }
                Directory.Delete(finalPath);
            }
            _unitOfWork.ProductRepository.Remove(productToBeDeleted);
            _unitOfWork.Save();
            return Json(new { Success = true, message = "Delete Successful" });
        }
        #endregion
    }
}
