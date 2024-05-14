using MediatR;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SimpleShop.Application.Commands.Carts;
using SimpleShop.Application.Commands.Orders;
using SimpleShop.Application.Queries.Carts;
using SimpleShop.Domain.Entities;

using System.ComponentModel.DataAnnotations;
using Net.payOS;
using Net.payOS.Types;
using System.Security.Policy;
using System;
using SimpleShop.Application.Queries.Products;

#nullable disable

namespace SimpleShop.Web.Pages.Cart
{
    public class CheckoutModel : PageModel
    {
        private readonly IMediator _mediator;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CheckoutModel(IMediator mediator, UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _mediator = mediator;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public Domain.Entities.Cart Cart { get; set; }
        [BindProperty]
        public InputModel Input { get; set; }

        public string clientId = "6fc78d5c-5a42-46c3-8d3e-3648f1e176db";
        public string apiKey = "0fd0be5a-201d-479b-9b80-b09425472a79";

        public string checksumKey = "3058928b5edc6597b004e215d1a8c2008644ee6c7fe4c460dd9c1ca2bf538247";

        public string cancelUrl { get; set; }

        public string returnUrl { get; set; }

        private static readonly Random random = new Random();


        public class InputModel
        {
            [Required(ErrorMessage = "Tên phải bắt buộc")]
            [RegularExpression(@"^[^0-9_!¡?÷?¿/\\+=@#$%ˆ&*(){}|~<>;:[\]]{2,}$", ErrorMessage = "Định dạng tên không chính xác")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Họ phải bắt buộc")]
            [RegularExpression(@"^[^0-9_!¡?÷?¿/\\+=@#$%ˆ&*(){}|~<>;:[\]]{2,}$", ErrorMessage = "Định dạng họ không chính xác")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "Địa chỉ phải bắt buộc")]
            [RegularExpression(@"^[^0-9_!¡?÷?¿/\\+=@#$%ˆ&*(){}|~<>;:[\]]{2,}$", ErrorMessage = "Địa chỉ không đúng định dạng")]
            public string Street { get; set; }

            [Required(ErrorMessage = "Sô nhà bắt buộc")]
            [RegularExpression(@"[1-9]\d*(\s*[-/]\s*[1-9]\d*)?(\s?[a-zA-Z])?", ErrorMessage = "Số nhà không đúng định dạng")]
            public string Apartment { get; set; }

            [Required(ErrorMessage = "Thành phố bắt buộc")]
            [RegularExpression(@"^[^0-9_!¡?÷?¿/\\+=@#$%ˆ&*(){}|~<>;:[\]]{2,}$", ErrorMessage = "Thành phố không chính xác")]
            public string City { get; set; }

            [Required(ErrorMessage = "Mã bưu chính bắt buộc")]
            [RegularExpression(@"^[a-z0-9][a-z0-9\- ]{0,10}[a-z0-9]$", ErrorMessage = "Mã bưu chính không đúng định dạng")]
            public string PostalCode { get; set; }

            [Required(ErrorMessage = "Số điện thoại bắt buộc")]
            [RegularExpression(@"(?<!\w)(\(?(\+|00)?48\)?)?[ -]?\d{3}[ -]?\d{3}[ -]?\d{3}(?!\w)", ErrorMessage = "Số điện thoại không đúng định dạng")]
            public string Phone { get; set; }

            [Required(ErrorMessage = "Email bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Phương thức thanh toán bắt buộc")]
            public string PaymentMethod { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);

            Cart = await _mediator.Send(new GetCart.Query(userId));
            if (Cart.IsEmpty)
            {
                return RedirectToPage("Index");
            }
            return Page();
        }
     

        public async Task<IActionResult> paymentMethod(string idUrl, List<Product> product)
        {
            Console.WriteLine("data product >>>" + product[0].Name);
            Random random = new Random();
            int orderCode = random.Next(1,10000000);
            var httpResponse = _httpContextAccessor.HttpContext.Response;
            PayOS payOS = new PayOS(clientId, apiKey, checksumKey);
            List<ItemData> items = new List<ItemData>();
            foreach (var x in product)
            {
                ItemData item = new ItemData(x.Name, 2000, 2000);
                items.Add(item);
            }

            PaymentData paymentData = new PaymentData(orderCode, 2000, "Thanh toan don hang",
                items, cancelUrl = "https://localhost:7157/Error", returnUrl = $"https://localhost:7157/Cart/Summary/{idUrl}");

            CreatePaymentResult createPayment = await payOS.createPaymentLink(paymentData);
            Console.WriteLine("Create Payment >>>>>>>>>>>>" + createPayment);
            return new RedirectResult(createPayment.checkoutUrl);
        }
        public async Task<IActionResult> OnPostAsync()
        {
            var userId = _userManager.GetUserId(User);

            Cart = await _mediator.Send(new GetCart.Query(userId));
            if (Cart.IsEmpty)
            {
                return RedirectToPage("Index");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Number = new Random().Next(1000, 4000)
            };

            var success = await _mediator.Send(new CreateOrder.Command(order));
           
            if (success)
            {

                var orderItems = new List<OrderItem>();
                var getProducts = new List<Product>();
                foreach (var cartItem in Cart.Items)
                {
                    orderItems.Add(new OrderItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        ProductId = cartItem.ProductId,
                        OrderId = order.Id,
                        Quantity = cartItem.Quantity,
                        Size = cartItem.Size
                    });
                }

                for (var i = 0; i < orderItems.Count; i++)
                {
                    var valueProduct = await _mediator.Send(new GetProduct.Query(orderItems[i].ProductId));
                    getProducts.Add(valueProduct);
                }

                
                var orderDetails = new OrderDetails
                {
                    Id = Guid.NewGuid().ToString(),
                    OrderId = order.Id,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    Street = Input.Street,
                    Apartment = Input.Apartment,
                    City = Input.City,
                    PostalCode = Input.PostalCode,
                    Phone = Input.Phone,
                    Email = Input.Email,
                    PaymentMethod = Input.PaymentMethod,
                    Total = Cart.GetTotal()
                };
               return await paymentMethod(order.Id, getProducts);


                await _mediator.Send(new AddOrderItems.Command(orderItems));
                await _mediator.Send(new AddOrderDetails.Command(orderDetails));
                await _mediator.Send(new ClearCart.Command(Cart.Id));

            }
             
           

            return RedirectToPage("Summary", new { orderId = order.Id });
        }
    }
}
