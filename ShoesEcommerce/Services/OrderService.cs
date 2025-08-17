using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Orders;
using ShoesEcommerce.Models.ViewModels;
using ShoesEcommerce.Models.Carts;
using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;

        public OrderService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<OrderViewModel> GetOrderByIdAsync(int orderId, int customerId)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.ShippingAddress)
                .Include(o => o.Payment)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId);

            if (order == null)
                return null;

            return new OrderViewModel
            {
                Id = order.Id,
                OrderNumber = $"ORD{order.Id:D6}",
                CreatedAt = order.CreatedAt,
                TotalAmount = order.TotalAmount,
                Status = "Đang xử lý", // Có thể thêm enum cho status
                PaymentStatus = order.Payment?.Status ?? "Chưa thanh toán",
                OrderDetails = order.OrderDetails.Select(od => new OrderDetailViewModel
                {
                    Id = od.Id,
                    ProductVariant = new ProductVariantViewModel
                    {
                        Id = od.ProductVariant.Id,
                        Name = od.ProductVariant.Product.Name,
                        ImageUrl = od.ProductVariant.Product.ImageUrl,
                        Color = od.ProductVariant.Color,
                        Size = od.ProductVariant.Size,
                        Price = od.UnitPrice
                    },
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    SubTotal = od.Quantity * od.UnitPrice
                }).ToList(),
                ShippingAddress = new ShippingAddressViewModel
                {
                    Id = order.ShippingAddress.Id,
                    FullName = order.ShippingAddress.FullName,
                    PhoneNumber = order.ShippingAddress.PhoneNumber,
                    Address = order.ShippingAddress.Address,
                    City = order.ShippingAddress.City,
                    District = order.ShippingAddress.District
                },
                Payment = new PaymentViewModel
                {
                    Id = order.Payment?.Id ?? 0,
                    Method = order.Payment?.Method ?? "",
                    Status = order.Payment?.Status ?? "",
                    PaidAt = order.Payment?.PaidAt
                }
            };
        }

        public async Task<List<OrderViewModel>> GetCustomerOrdersAsync(int customerId)
        {
            var orders = await _context.Orders
                .Include(o => o.ShippingAddress)
                .Include(o => o.Payment)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(order => new OrderViewModel
            {
                Id = order.Id,
                OrderNumber = $"ORD{order.Id:D6}",
                CreatedAt = order.CreatedAt,
                TotalAmount = order.TotalAmount,
                Status = "Đang xử lý",
                PaymentStatus = order.Payment?.Status ?? "Chưa thanh toán",
                OrderDetails = order.OrderDetails.Select(od => new OrderDetailViewModel
                {
                    Id = od.Id,
                    ProductVariant = new ProductVariantViewModel
                    {
                        Id = od.ProductVariant.Id,
                        Name = od.ProductVariant.Product.Name,
                        ImageUrl = od.ProductVariant.Product.ImageUrl,
                        Color = od.ProductVariant.Color,
                        Size = od.ProductVariant.Size,
                        Price = od.UnitPrice
                    },
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    SubTotal = od.Quantity * od.UnitPrice
                }).ToList(),
                ShippingAddress = new ShippingAddressViewModel
                {
                    Id = order.ShippingAddress.Id,
                    FullName = order.ShippingAddress.FullName,
                    PhoneNumber = order.ShippingAddress.PhoneNumber,
                    Address = order.ShippingAddress.Address,
                    City = order.ShippingAddress.City,
                    District = order.ShippingAddress.District
                },
                Payment = new PaymentViewModel
                {
                    Id = order.Payment?.Id ?? 0,
                    Method = order.Payment?.Method ?? "",
                    Status = order.Payment?.Status ?? "",
                    PaidAt = order.Payment?.PaidAt
                }
            }).ToList();
        }

        public async Task<Order> CreateOrderAsync(CreateOrderViewModel model, int customerId)
        {
            // Lấy thông tin giỏ hàng thông qua Customer.CartId
            var customer = await _context.Customers
                .Include(c => c.Cart)
                    .ThenInclude(cart => cart.CartItems)
                        .ThenInclude(ci => ci.ProductVariant)
                            .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(c => c.Id == customerId);
            
            var cart = customer?.Cart;

            if (cart?.CartItems == null || !cart.CartItems.Any())
                throw new InvalidOperationException("Giỏ hàng trống");

            // Tạo đơn hàng
            var order = new Order
            {
                CustomerId = customerId,
                ShippingAddressId = model.ShippingAddressId,
                CreatedAt = DateTime.Now,
                TotalAmount = cart.CartItems.Sum(ci => ci.Quantity * ci.ProductVariant.Product.Price),
                OrderDetails = new List<OrderDetail>()
            };

            // Tạo chi tiết đơn hàng
            foreach (var cartItem in cart.CartItems)
            {
                order.OrderDetails.Add(new OrderDetail
                {
                    ProductVariantId = cartItem.ProductVarientId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.ProductVariant.Product.Price
                });
            }

            // Tạo thanh toán
            order.Payment = new Payment
            {
                Method = model.PaymentMethod,
                Status = "Pending",
                OrderId = order.Id
            };

            // Tạo hóa đơn
            order.Invoice = new Invoice
            {
                InvoiceNumber = await GenerateOrderNumberAsync(),
                IssuedAt = DateTime.Now,
                Amount = order.TotalAmount,
                OrderId = order.Id
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Xóa giỏ hàng sau khi tạo đơn hàng thành công
            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();

            return order;
        }

        public async Task<CheckoutViewModel> GetCheckoutDataAsync(int customerId)
        {
            // Lấy thông tin giỏ hàng thông qua Customer.CartId
            var customer = await _context.Customers
                .Include(c => c.Cart)
                    .ThenInclude(cart => cart.CartItems)
                        .ThenInclude(ci => ci.ProductVariant)
                            .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(c => c.Id == customerId);
            
            var cart = customer?.Cart;

            var addresses = await GetCustomerShippingAddressesAsync(customerId);

            var cartItems = cart?.CartItems?.Select(ci => new CartItemViewModel
            {
                Id = ci.Id,
                ProductVariant = new ProductVariantViewModel
                {
                    Id = ci.ProductVariant.Id,
                    Name = ci.ProductVariant.Product.Name,
                    ImageUrl = ci.ProductVariant.Product.ImageUrl,
                    Color = ci.ProductVariant.Color,
                    Size = ci.ProductVariant.Size,
                    Price = ci.ProductVariant.Product.Price
                },
                Quantity = ci.Quantity,
                UnitPrice = ci.ProductVariant.Product.Price,
                SubTotal = ci.Quantity * ci.ProductVariant.Product.Price
            }).ToList() ?? new List<CartItemViewModel>();

            var subTotal = cartItems.Sum(ci => ci.SubTotal);
            var shippingFee = 0m; // Có thể tính dựa trên địa chỉ
            var totalAmount = subTotal + shippingFee;

            return new CheckoutViewModel
            {
                CartItems = cartItems,
                AvailableAddresses = addresses.Select(a => new ShippingAddressViewModel
                {
                    Id = a.Id,
                    FullName = a.FullName,
                    PhoneNumber = a.PhoneNumber,
                    Address = a.Address,
                    City = a.City,
                    District = a.District
                }).ToList(),
                OrderInfo = new CreateOrderViewModel(),
                SubTotal = subTotal,
                ShippingFee = shippingFee,
                TotalAmount = totalAmount
            };
        }

        public async Task<ShippingAddress> CreateShippingAddressAsync(CreateShippingAddressViewModel model, int customerId)
        {
            var address = new ShippingAddress
            {
                CustomerId = customerId,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                City = model.City,
                District = model.District
            };

            _context.ShippingAddresses.Add(address);
            await _context.SaveChangesAsync();

            return address;
        }

        public async Task<List<ShippingAddress>> GetCustomerShippingAddressesAsync(int customerId)
        {
            return await _context.ShippingAddresses
                .Where(sa => sa.CustomerId == customerId)
                .ToListAsync();
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            // Có thể thêm logic cập nhật status
            // Note: Order model không có Status property
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdatePaymentStatusAsync(int orderId, string status)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == orderId);
            
            if (payment == null) return false;

            payment.Status = status;
            if (status == "Paid")
                payment.PaidAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> GenerateOrderNumberAsync()
        {
            var lastOrder = await _context.Orders
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            var nextId = (lastOrder?.Id ?? 0) + 1;
            return $"ORD{nextId:D6}";
        }

        public async Task<decimal> CalculateShippingFeeAsync(string city, string district)
        {
            // Logic tính phí vận chuyển dựa trên thành phố và quận/huyện
            // Có thể lấy từ database hoặc tính toán theo quy tắc
            return 30000m; // Mặc định 30,000 VND
        }
    }
}
