using ApiAggregationService.Models;

namespace ApiAggregationService.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        bool ValidateCredentials(string username, string password);
        User GetUser(string username);
    }
}