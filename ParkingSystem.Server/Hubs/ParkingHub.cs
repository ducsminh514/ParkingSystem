using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Server.Models;

namespace ParkingSystem.Server.Hubs
{
    public class ParkingHub : Hub
    {
        private readonly ParkingManagementContext _context;
        private readonly ILogger<ParkingHub> _logger;

        public ParkingHub(ParkingManagementContext context, ILogger<ParkingHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ============ GET ALL CUSTOMERS ============
        public async Task<List<Customer>> GetAllCustomers()
        {
            try
            {
                var customers = await _context.Customers
                    .Include(c => c.Vehicles)
                    .OrderBy(c => c.FullName)
                    .ToListAsync();

                return customers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all customers");
                throw new HubException("Không thể lấy danh sách khách hàng");
            }
        }

        // ============ GET CUSTOMER BY ID ============
        public async Task<Customer> GetCustomerById(Guid customerId)
        {
            var customer = await _context.Customers
                .Include(c => c.Vehicles)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
                throw new HubException("Không tìm thấy khách hàng");

            return customer;
        }

        // ============ ADD CUSTOMER ============
        public async Task<Customer> AddCustomer(Customer customer)
        {
            customer.CustomerId = Guid.NewGuid();
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await Clients.All.SendAsync("CustomerAdded", customer);
            return customer;
        }

        // ============ UPDATE CUSTOMER ============
        public async Task<Customer> UpdateCustomer(Customer customer)
        {
            var existing = await _context.Customers.FindAsync(customer.CustomerId);
            if (existing == null)
                throw new HubException("Không tìm thấy khách hàng");

            existing.FullName = customer.FullName;
            existing.Phone = customer.Phone;
            existing.Email = customer.Email;
            existing.PasswordHash = customer.PasswordHash;

            await _context.SaveChangesAsync();
            await Clients.All.SendAsync("CustomerUpdated", existing);

            return existing;
        }

        // ============ DELETE CUSTOMER ============
        public async Task DeleteCustomer(Guid customerId)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
                throw new HubException("Không tìm thấy khách hàng");

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            await Clients.All.SendAsync("CustomerDeleted", customerId);
        }

        // ============ SEARCH ============
        public async Task<List<Customer>> SearchCustomers(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await GetAllCustomers();

            return await _context.Customers
                .Where(c => c.FullName.Contains(keyword)
                         || c.Phone.Contains(keyword)
                         || c.Email.Contains(keyword))
                .Include(c => c.Vehicles)
                .ToListAsync();
        }
    }
}
