using MediatR;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SimpleShop.Application.Commands.Carts;
using SimpleShop.Application.Commands.Orders;
using SimpleShop.Application.Queries.Carts;
using SimpleShop.Domain.Entities;

using System.ComponentModel.DataAnnotations;

#nullable disable

namespace SimpleShop.Web.Pages.Cart
{
    public class CheckoutModel : PageModel
    {
        private readonly IMediator _mediator;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutModel(IMediator mediator, UserManager<ApplicationUser> userManager)
        {
            _mediator = mediator;
            _userManager = userManager;
        }

        public Domain.Entities.Cart Cart { get; set; }
        [BindProperty]
        public InputModel Input { get; set; }

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

                await _mediator.Send(new AddOrderItems.Command(orderItems));
                await _mediator.Send(new AddOrderDetails.Command(orderDetails));
                await _mediator.Send(new ClearCart.Command(Cart.Id));
            }

            return RedirectToPage("Summary", new { orderId = order.Id });
        }
    }
}
