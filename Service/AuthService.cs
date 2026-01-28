using BrainDumpster.Data;
using BrainDumpster.Model;
using Microsoft.JSInterop;

namespace BrainDumpster.Services
{
    public class AuthService
    {
        private readonly AppDbContext _db;
        private readonly IJSRuntime _jsRuntime;

        public AuthService(AppDbContext db, IJSRuntime jsRuntime)
        {
            _db = db;
            _jsRuntime = jsRuntime;
        }

        // Login logic
        public async Task<(bool Success, string Message, int? UserId)> LoginAsync(string username, string pin)
        {
            try
            {
                // Validation logic moved here
                if (string.IsNullOrWhiteSpace(username))
                    return (false, "Please enter your username", null);

                if (string.IsNullOrWhiteSpace(pin) || pin.Length < 4)
                    return (false, "PIN must be 4-6 digits", null);

                // Database check
                var user = _db.Users.FirstOrDefault(u => u.Username == username && u.Pin == pin);

                if (user == null)
                    return (false, "Invalid username or PIN!", null);

                // Store in localStorage
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "loggedUserId", user.Id.ToString());
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "loggedUsername", user.Username);


                return (true, "Login successful!", user.Id);
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}", null);
            }
        }

        // Check if user is logged in
        public async Task<bool> IsLoggedInAsync()
        {
            try
            {
                var userId = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "loggedUserId");
                return !string.IsNullOrEmpty(userId);
            }
            catch
            {
                return false;
            }
        }

        // Logout
        public async Task LogoutAsync()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "loggedUserId");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "loggedUsername");
        }
    }
}