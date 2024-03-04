using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        public readonly ApplicationDbContext _db;

        public UserController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        #region API Calls
        [HttpGet]
        public IActionResult GetAll()
        {
            var objuserList = _db.ApplicationUsers.Include(u => u.Company).ToList();
            foreach (var user in objuserList)
            {
                var roleId = _db.UserRoles.FirstOrDefault(ur => ur.UserId == user.Id).RoleId;
                user.Role = _db.Roles.FirstOrDefault(r => r.Id == roleId).Name;
                if (user.Company == null)
                {
                    user.Company = new Company()
                    {
                        Name = ""
                    };
                }
            }
            return Json(new { Data = objuserList });
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody]string id)
        {
            var objuserList = _db.ApplicationUsers.FirstOrDefault(u => u.Id == id);
            if (objuserList is null) Json(new { Success = false, message = "Error While Locking/Unlocking" });
            
            if(objuserList.LockoutEnd !=null && objuserList.LockoutEnd > DateTime.Now)
            {
                // need to unlock user
                objuserList.LockoutEnd = DateTime.Now;
            }
            else
            {
                // need to Lock user
                objuserList.LockoutEnd = DateTime.Now.AddYears(1000);
            }
            _db.SaveChanges();
            return Json(new { Success = true, message = "Successful" });
        }
        #endregion
    }
}
