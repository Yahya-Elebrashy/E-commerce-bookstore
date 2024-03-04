using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IUnitOfWork
    {
        public ICategoryRepository CategoryRepository { get; set; }
        public IProductRepository ProductRepository { get; set; }
        public ICompanyRepository CompanyRepository { get; set; }
        public IShoppingCartRepository ShoppingCartRepository { get; set; }
        public IApplicationUserRepository ApplicationUserRepository { get; set; }
        public IOrderHeaderRepository OrderHeaderRepository { get; set; }
        public IOrderDetailRepository OrderDetailRepository { get; set; }
        public IProductImageRepository ProductImageRepository { get; set; }

		void Save();
    }
}
