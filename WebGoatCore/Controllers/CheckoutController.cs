﻿using WebGoatCore.Models;
using WebGoatCore.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using WebGoatCore.ViewModels;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;

namespace WebGoatCore.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly CustomerRepository _customerRepository;
        private readonly ShipperRepository _shipperRepository;
        private readonly OrderRepository _orderRepository;
        private CheckoutViewModel _model;
        private string _resourcePath;

        public CheckoutController(UserManager<IdentityUser> userManager, CustomerRepository customerRepository, IHostEnvironment hostEnvironment, IConfiguration configuration, ShipperRepository shipperRepository, OrderRepository orderRepository)
        {
            _userManager = userManager;
            _customerRepository = customerRepository;
            _shipperRepository = shipperRepository;
            _orderRepository = orderRepository;
            _resourcePath = configuration.GetValue(Constants.WEBGOAT_ROOT, hostEnvironment.ContentRootPath);
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            if(_model == null)
            {
                InitializeModel();
            }

            return View(_model);
        }

        private void InitializeModel()
        {
            _model = new CheckoutViewModel();
            var customer = GetCustomerOrAddError();
            var creditCard = GetCreditCardForUser();

            try
            {
                creditCard.GetCardForUser();
                _model.CreditCard = creditCard.Number;
                _model.ExpirationMonth = creditCard.Expiry.Month;
                _model.ExpirationYear = creditCard.Expiry.Year;
            }
            catch (NullReferenceException)
            {
            }

            _model.Cart = HttpContext.Session.Get<Cart>("Cart");
            if (_model.Cart == null || _model.Cart.OrderDetails.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "You have no items in your cart.");
            }

            if (customer != null)
            {
                _model.ShipTarget = customer.CompanyName;
                _model.Address = customer.Address;
                _model.City = customer.City;
                _model.Region = customer.Region;
                _model.PostalCode = customer.PostalCode;
                _model.Country = customer.Country;
            }

            _model.AvailableExpirationYears = Enumerable.Range(1, 5).Select(i => DateTime.Now.Year + i).ToList();
            _model.ShippingOptions = _shipperRepository.GetShippingOptions(_model.Cart?.SubTotal ?? 0);
        }

        [HttpPost]
        public IActionResult Checkout(CheckoutViewModel model)
        {
            model.Cart = HttpContext.Session.Get<Cart>("Cart")!;

            var customer = GetCustomerOrAddError();
            if(customer == null)
            {
                return View(model);
            }

            var creditCard = GetCreditCardForUser();
            try
            {
                creditCard.GetCardForUser();
            }
            catch (NullReferenceException)
            {
            }

            //Get form of payment
            //If old card is null or if the number, month or year were changed then take what was on the form.
            if (creditCard.Number.Length <= 4)
            {
                creditCard.Number = model.CreditCard;
                creditCard.Expiry = new DateTime(model.ExpirationYear, model.ExpirationMonth, 1);
            }
            else
            {
                if (model.CreditCard.Substring(model.CreditCard.Length - 4) !=
                    creditCard.Number.Substring(creditCard.Number.Length - 4))
                {
                    creditCard.Number = model.CreditCard;
                }

                if (model.ExpirationMonth != creditCard.ExpiryMonth || model.ExpirationYear != creditCard.ExpiryYear)
                {
                    creditCard.Expiry = new DateTime(model.ExpirationYear, model.ExpirationMonth, 1);
                }
            }

            //Authorize payment through our bank or Authorize.net or someone.
            if (!creditCard.IsValid())
            {
                ModelState.AddModelError(string.Empty, "That card is not valid. Please enter a valid card.");
                _model = model;
                return View(_model);
            }

            if (model.RememberCreditCard)
            {
                creditCard.SaveCardForUser();
            }

            var order = new Order
            {
                ShipVia = model.ShippingMethod,
                ShipName = model.ShipTarget,
                ShipAddress = model.Address,
                ShipCity = model.City,
                ShipRegion = model.Region,
                ShipPostalCode = model.PostalCode,
                ShipCountry = model.Country,
                OrderDetails = model.Cart.OrderDetails,
                CustomerId = customer.CustomerId,
                OrderDate = DateTime.Now,
                RequiredDate = DateTime.Now.AddDays(7),
                Freight = Math.Round(_shipperRepository.GetShipperByShipperId(model.ShippingMethod).GetShippingCost(model.Cart.SubTotal), 2),
                EmployeeId = 1,
            };

            var approvalCode = creditCard.ChargeCard(order.Total);

            order.Shipment = new Shipment()
            {
                ShipmentDate = DateTime.Today.AddDays(1),
                ShipperId = order.ShipVia,
                TrackingNumber = _shipperRepository.GetNextTrackingNumber(_shipperRepository.GetShipperByShipperId(order.ShipVia)),
            };

            //Create the order itself.
            var orderId = _orderRepository.CreateOrder(order);

            //Create the payment record.
            _orderRepository.CreateOrderPayment(orderId, order.Total, creditCard.Number, creditCard.Expiry, approvalCode);

            HttpContext.Session.SetInt32("OrderId", orderId);
            HttpContext.Session.Remove("Cart");
            return RedirectToAction("Receipt");
        }

        [HttpGet]
        [Authorize]
        public IActionResult Receipt(int? id)
        {
            var orderId = HttpContext.Session.GetInt32("OrderId");
            if (id != null)
            {
                orderId = id;
            }

            if (orderId == null)
            {
                ModelState.AddModelError(string.Empty, "No order specified. Please try again.");
                return View();
            }

            Order order;
            try
            {
                order = _orderRepository.GetOrderById(orderId.Value);

            }
            catch (InvalidOperationException)
            {
                ModelState.AddModelError(string.Empty, string.Format("Order {0} was not found.", orderId));
                return View();
            }
            

            if (order.Customer.ContactName == _userManager.GetUserName(User))

            {
                return View(order);
            }

            else
            {
                return View("Failure");
            }
                
        }

        public IActionResult Failure()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult Receipts()
        {
            var customer = GetCustomerOrAddError();
            if(customer == null)
            {
                return View();
            }
            if(customer.ContactName==_userManager.GetUserName(User))
            {
                return View(_orderRepository.GetAllOrdersByCustomerId(customer.CustomerId));

            }

            else
            {
                return View("Failure");
            }
        }

        [HttpGet]
        public IActionResult PackageTracking(string? carrier, string? trackingNumber)
        {
            var model = new PackageTrackingViewModel()
            {
                SelectedCarrier = carrier,
                SelectedTrackingNumber = trackingNumber,
            };

            var customer = GetCustomerOrAddError();
            if (customer != null)
            {
                model.Orders = _orderRepository.GetAllOrdersByCustomerId(customer.CustomerId);
            }
            
            return View(model);
        }

        public IActionResult GoToExternalTracker(string carrier, string trackingNumber)
        {
            return Redirect(Order.GetPackageTrackingUrl(carrier, trackingNumber));
        }

        private Customer? GetCustomerOrAddError()
        {
            var username = _userManager.GetUserName(User);
            var customer = _customerRepository.GetCustomerByUsername(username);
            if (customer == null)
            {
                ModelState.AddModelError(string.Empty, "I can't identify you. Please log in and try again.");
                return null;
            }

            return customer;
        }

        private CreditCard GetCreditCardForUser()
        {
            return new CreditCard()
            {
                Filename = Path.Combine(_resourcePath, "StoredCreditCards.xml"),
                Username = _userManager.GetUserName(User)
            };
        }
    }
}