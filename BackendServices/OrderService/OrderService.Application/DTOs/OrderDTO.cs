using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Application.DTOs
{
    public class OrderDTO
    {
        public Guid OrderId { get; set; }

        public string PaymentId { get; set; }

        public string Street { get; set; }

        public string ZipCode { get; set; }

        public string City { get; set; }

        public string Locality { get; set; }

        public string PhoneNumber { get; set; }

        public long UserId { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? AcceptDate { get; set; }
    }
}
