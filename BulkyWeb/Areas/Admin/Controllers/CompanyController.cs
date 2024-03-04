using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        public readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CompanyController(IUnitOfWork unitOfWork , IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            var objCompanyList = _unitOfWork.CompanyRepository.GetAll().ToList();
            return View(objCompanyList);
        }
        public IActionResult Upsert(int? id)
        {
            
            if (id == null || id ==0)
                //Add
                return View(new Company());
            else
            {
                //Update
                Company CompanyObj = _unitOfWork.CompanyRepository.Get(p => p.Id == id);
                return View(CompanyObj);
            }
        }
        [HttpPost]
        public IActionResult Upsert(Company CompanyObj)
        {
            if (ModelState.IsValid)
            {
                
                if (CompanyObj.Id == 0)
                    _unitOfWork.CompanyRepository.Add(CompanyObj);
                else 
                    _unitOfWork.CompanyRepository.Update(CompanyObj);

                _unitOfWork.Save();
                TempData["Success"] = "Company Created Successfuly";
                return RedirectToAction("Index");
            }
            else
            {
                return View(CompanyObj);
            }
        }

        #region API Calls
        [HttpGet]
        public IActionResult GetAll()
        {
            var objCompanyList = _unitOfWork.CompanyRepository.GetAll().ToList();
            return Json(new { Data = objCompanyList});
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            if (id is null || id <= 0) return Json(new {Success = false , message = "Error While deleting"});            
            var CompanyToBeDeleted = _unitOfWork.CompanyRepository.Get(u => u.Id == id);
            if (CompanyToBeDeleted is null) Json(new { Success = false, message = "Error While deleting" });
            _unitOfWork.CompanyRepository.Remove(CompanyToBeDeleted);
            _unitOfWork.Save();
            return Json(new { Success = true, message = "Delete Successful" });
        }
        #endregion
    }
}
