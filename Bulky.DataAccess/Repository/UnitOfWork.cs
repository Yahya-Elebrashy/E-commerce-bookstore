using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _db;
        public ICategoryRepository CategoryRepository { get; set; }
        public IProductRepository ProductRepository { get; set; }
        public ICompanyRepository CompanyRepository { get; set; }
        public IShoppingCartRepository ShoppingCartRepository { get; set; }
        public IApplicationUserRepository ApplicationUserRepository { get; set; }
		public IOrderHeaderRepository OrderHeaderRepository { get; set; }
		public IOrderDetailRepository OrderDetailRepository { get; set; }
        public IProductImageRepository ProductImageRepository { get; set; }

		public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;
            CategoryRepository = new CategoryRepository(_db);
            ProductRepository = new ProductRepository(_db);
            CompanyRepository = new CompanyRepository(_db);
            ShoppingCartRepository = new ShoppingCartRepository(_db);
            ApplicationUserRepository = new ApplicationUserRepository(_db);
			OrderHeaderRepository = new OrderHeaderRepository(_db);
			OrderDetailRepository = new OrderDetailRepository(_db);
            ProductImageRepository = new ProductImageRepository(_db);
        }

        public void Save()
        {
            _db.SaveChanges();
        }
    }
}
