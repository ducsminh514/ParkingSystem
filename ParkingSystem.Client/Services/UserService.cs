namespace ParkingSystem.Client.Services;

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using ParkingSystem.Shared.Models;

public class UserService
{
    private readonly AuthenticationStateProvider _authStateProvider;

    public UserService(AuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    public async Task<UserInfo?> GetCurrentUser()
    {
        if (_authStateProvider is SimpleAuthStateProvider authProvider)
        {
            return await authProvider.GetCurrentUser();
        }
        return null;
    }

    public async Task<Guid> GetCurrentUserId()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
    
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("User is not authenticated");
    
        if (Guid.TryParse(userIdClaim, out var userId))
            return userId;
    
        throw new InvalidOperationException("Invalid user ID format");
    }

    public async Task<bool> IsInRole(string role)
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        return user.IsInRole(role);
    }
}
