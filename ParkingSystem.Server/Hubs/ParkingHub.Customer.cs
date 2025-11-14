using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Server.Models;
namespace ParkingSystem.Server.Hubs
{
    public partial class ParkingHub 
    {

        // Register Customer
        public async Task<AuthResult> RegisterCustomer(RegisterRequest request)
        {
            try
            {
                // Check for duplicate phone number
                var exists = await _context.Customers
                    .AnyAsync(c => c.Phone == request.Phone );

                if (exists)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Phone number already exists!"
                    };
                }

                // Create new customer
                var customer = new Customer
                {
                    CustomerId = Guid.NewGuid(),
                    FullName = request.FullName,
                    Phone = request.Phone,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                return new AuthResult
                {
                    Success = true,
                    Message = "Registration successful!",
                    UserInfo = new UserInfo
                    {
                        UserId = customer.CustomerId,
                        FullName = customer.FullName,
                        UserType = "Customer",
                        Email = customer.Email,
                        Phone = customer.Phone
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering customer");
                return new AuthResult { Success = false, Message = "System error!" };
            }
        }

        // Login
        public async Task<AuthResult> Login(LoginRequest request)
        {
            try
            {

            if (request.UsernameOrEmail == "admin" &&
                request.Password == "admin123")
            {
                return new AuthResult
                {
                    Success = true,
                    UserInfo = new UserInfo
                    {
                        UserId = Guid.Empty, // Or a specific admin GUID
                        FullName = "System Admin",
                        Email = "admin@parking.com",
                        UserType = "Admin"
                    }
                };
             }
            else
                {
                    if (request.IsStaff)
                    {
                        // Login Staff
                        var staff = await _context.Staff
                            .FirstOrDefaultAsync(s => s.Username == request.UsernameOrEmail);

                        if (staff == null || !BCrypt.Net.BCrypt.Verify(request.Password, staff.PasswordHash))
                        {
                            return new AuthResult
                            {
                                Success = false,
                                Message = "Invalid username or password!"
                            };
                        }

                        return new AuthResult
                        {
                            Success = true,
                            Message = "Login successful!",
                            UserInfo = new UserInfo
                            {
                                UserId = staff.StaffId,
                                FullName = staff.FullName,
                                UserType = "Staff"
                            }
                        };
                    }
                    else
                    {
                        // Login Customer
                        var customer = await _context.Customers
                            .FirstOrDefaultAsync(c => c.Email == request.UsernameOrEmail ||
                                                      c.Phone == request.UsernameOrEmail);

                        if (customer == null || !BCrypt.Net.BCrypt.Verify(request.Password, customer.PasswordHash))
                        {
                            return new AuthResult
                            {
                                Success = false,
                                Message = "Incorrect email/phone or password!"
                            };
                        }

                        return new AuthResult
                        {
                            Success = true,
                            Message = "Login successful!",
                            UserInfo = new UserInfo
                            {
                                UserId = customer.CustomerId,
                                FullName = customer.FullName,
                                UserType = "Customer",
                                Email = customer.Email,
                                Phone = customer.Phone
                            }
                        };
                    }
                } 
              
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return new AuthResult { Success = false, Message = "System error!" };
            }
        }

        // ============ GET ALL CUSTOMERS ============
        public async Task<List<Customer>> GetAllCustomers()
        {
            try
            {
                var customers = await _context.Customers
                    .Where(c => !c.IsDeleted) // Only get non-deleted customers
                    .Include(c => c.Vehicles)
                    .ThenInclude(v => v.ParkingRegistrations)
                    .OrderBy(c => c.FullName)
                    .ToListAsync();

                return customers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all customers");
                throw new HubException("Unable to retrieve customer list");
            }
        }

        // ============ GET CUSTOMER BY ID ============
        public async Task<Customer> GetCustomerById(Guid customerId)
        {
            var customer = await _context.Customers
                .Where(c => !c.IsDeleted) // Only get non-deleted customers
                .Include(c => c.Vehicles)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
                throw new HubException("Customer not found");

            return customer;
        }

        // ============ ADD CUSTOMER ============
        public async Task<Customer> AddCustomer(Customer customer)
        {
            customer.CustomerId = Guid.NewGuid();
            customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(customer.PasswordHash);
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
                throw new HubException("Customer not found");

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
                throw new HubException("Customer not found");
            
            var hasActiveParking = await _context.Vehicles
                .Where(v => v.CustomerId == customerId)
                .SelectMany(v => v.ParkingRegistrations)
                .AnyAsync(pr => pr.Status == "Active" && pr.CheckOutTime == null);

            if (hasActiveParking)
            {
                throw new HubException("Cannot delete customer with vehicles currently parked!");
            }

            customer.IsDeleted = true; // Mark as deleted
            _context.Customers.Update(customer); // Update status
            await _context.SaveChangesAsync();

            await Clients.All.SendAsync("CustomerDeleted", customerId);
        }

        // ============ SEARCH ============
        public async Task<List<Customer>> SearchCustomers(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await GetAllCustomers();

            return await _context.Customers
                .Where(c=> !c.IsDeleted && (c.FullName.Contains(keyword)
                         || c.Phone.Contains(keyword)
                         || c.Email.Contains(keyword)))
                .Include(c => c.Vehicles)
                .ToListAsync();
        }

        public async Task<Customer?> GetCustomerWithFullDetails(Guid customerId)
        {
            try
            {
                var customer = await _context.Customers
                    .Where(c => !c.IsDeleted && c.CustomerId == customerId) // Only get non-deleted customers
                    .Include(c => c.Vehicles)
                    .ThenInclude(v => v.ParkingRegistrations.OrderByDescending(pr => pr.CheckInTime))
                    .ThenInclude(pr => pr.Slot)
                    .Include(c => c.Vehicles)
                    .ThenInclude(v => v.ParkingRegistrations)
                    .ThenInclude(pr => pr.Payments)
                    .AsNoTracking()
                    .AsSplitQuery()
                    .FirstOrDefaultAsync();

                return customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting customer details {customerId}");
                throw new HubException("Unable to retrieve customer details");
            }
        }
        // Get all vehicles for a customer
        public async Task<List<ParkingSystem.Server.Models.Vehicle>> GetMyVehicles(Guid customerId)
        {
            try
            {
                var vehicles = await _context.Vehicles
                    .Where(v => v.CustomerId == customerId)
                    .Include(v => v.ParkingRegistrations)
                    .OrderBy(v => v.PlateNumber)
                    .ToListAsync();

                return vehicles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicles for customer {CustomerId}", customerId);
                throw new HubException("Unable to retrieve vehicle list");
            }
        }

        // Add new vehicle
        public async Task<ParkingSystem.Server.Models.Vehicle> AddVehicle(Guid customerId, string plateNumber, string vehicleType)
        {
            try
            {
                // Check if plate number already exists
                var exists = await _context.Vehicles
                    .AnyAsync(v => v.PlateNumber == plateNumber);

                if (exists)
                {
                    throw new HubException("This license plate is already registered in the system!");
                }

                var vehicle = new ParkingSystem.Server.Models.Vehicle
                {
                    VehicleId = Guid.NewGuid(),
                    PlateNumber = plateNumber,
                    VehicleType = vehicleType,
                    CustomerId = customerId
                };

                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Vehicle {PlateNumber} added for customer {CustomerId}", plateNumber, customerId);

                // Send real-time notification
                await Clients.User(customerId.ToString()).SendAsync("VehicleAdded", vehicle);

                return vehicle;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding vehicle");
                throw new HubException(ex.Message.Contains("already registered") || ex.Message.Contains("tồn tại") ? ex.Message : "Unable to add vehicle");
            }
        }

        // Update vehicle
        public async Task<ParkingSystem.Server.Models.Vehicle> UpdateVehicle(Guid vehicleId, string plateNumber, string vehicleType)
        {
            try
            {
                var vehicle = await _context.Vehicles.FindAsync(vehicleId);

                if (vehicle == null)
                {
                    throw new HubException("Vehicle not found");
                }

                // Check if the new plate number already exists for other vehicle
                var duplicate = await _context.Vehicles
                    .AnyAsync(v => v.PlateNumber == plateNumber && v.VehicleId != vehicleId);

                if (duplicate)
                {
                    throw new HubException("This license plate already exists!");
                }

                vehicle.PlateNumber = plateNumber;
                vehicle.VehicleType = vehicleType;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Vehicle {VehicleId} updated", vehicleId);

                // Send real-time notification
                await Clients.User(vehicle.CustomerId.ToString()).SendAsync("VehicleUpdated", vehicle);

                return vehicle;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vehicle");
                throw new HubException(ex.Message.Contains("exists") || ex.Message.Contains("tồn tại") ? ex.Message : "Unable to update vehicle");
            }
        }

        // Delete vehicle
        public async Task DeleteVehicle(Guid vehicleId)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .Include(v => v.ParkingRegistrations)
                    .FirstOrDefaultAsync(v => v.VehicleId == vehicleId);

                if (vehicle == null)
                {
                    throw new HubException("Vehicle not found");
                }

                // Check if vehicle is currently parked
                var hasActiveParking = vehicle.ParkingRegistrations
                    .Any(pr => pr.Status == "InUse" && pr.CheckOutTime == null);

                if (hasActiveParking)
                {
                    throw new HubException("Cannot delete vehicle while it is still parked!");
                }

                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Vehicle {VehicleId} deleted", vehicleId);

                // Send real-time notification
                await Clients.User(vehicle.CustomerId.ToString()).SendAsync("VehicleDeleted", vehicleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vehicle");
                throw new HubException(ex.Message.Contains("still parked") || ex.Message.Contains("đang đỗ") ? ex.Message : "Unable to delete vehicle");
            }
        }

        // Get vehicle details with parking history
        public async Task<ParkingSystem.Server.Models.Vehicle?> GetVehicleDetails(Guid vehicleId)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .Include(v => v.ParkingRegistrations.OrderByDescending(pr => pr.CheckInTime))
                        .ThenInclude(pr => pr.Slot)
                    .Include(v => v.ParkingRegistrations)
                        .ThenInclude(pr => pr.Payments)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.VehicleId == vehicleId);

                return vehicle;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicle details");
                throw new HubException("Unable to retrieve vehicle information");
            }
        }

        // Change password
        public async Task ChangePassword(Guid customerId, ChangePasswordModel model)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(model.CurrentPassword) || 
                    string.IsNullOrEmpty(model.NewPassword) || 
                    string.IsNullOrEmpty(model.ConfirmPassword))
                {
                    throw new HubException("Please fill in all required information");
                }

                if (model.NewPassword != model.ConfirmPassword)
                {
                    throw new HubException("Confirmation password does not match");
                }

                if (model.NewPassword.Length < 6)
                {
                    throw new HubException("New password must be at least 6 characters long");
                }

                // Get customer
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null)
                {
                    throw new HubException("Customer information not found");
                }

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, customer.PasswordHash))
                {
                    throw new HubException("Current password is incorrect");
                }

                // Update password
                customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();
            }
            catch (HubException)
            {
                throw; // Re-throw HubException as is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for customer {CustomerId}", customerId);
                throw new HubException("An error occurred while changing the password. Please try again later.");
            }
        }
    }
}
