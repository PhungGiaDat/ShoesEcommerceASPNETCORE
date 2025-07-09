namespace ShoesEcommerce.Models.Orders
{
    public class Invoice
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }

        public string InvoiceNumber { get; set; }
        public DateTime IssuedAt { get; set; }

        public decimal Amount { get; set; }
    }
}
