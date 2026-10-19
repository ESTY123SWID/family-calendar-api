using my_project.Core.Entities;
using my_project.Core.Models;

namespace my_project.Core.Interfaces.Infrastructure;

public interface ITokenService
{
    AccessToken CreateToken(User user);
}
