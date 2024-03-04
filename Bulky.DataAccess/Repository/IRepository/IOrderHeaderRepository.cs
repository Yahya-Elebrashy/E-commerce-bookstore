using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IOrderHeaderRepository : IRepository<OrderHeader>
    {
        void Update(OrderHeader orderHeader);
		void UpdateStripePaymentID(int id, string sessionId, string paymentItentId);
		void UpdateStatus(int id, String orderStatus, String? paymentStatus = null);

	}
}
