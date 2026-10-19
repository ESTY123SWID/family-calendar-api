using AutoMapper;
using my_project.Core.Common;
using my_project.Core.DTOs.Users;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Infrastructure;
using my_project.Core.Interfaces.Services;
using my_project.Service.Common;

namespace my_project.Service.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public UserService(IUnitOfWork unitOfWork, ICurrentUser currentUser, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<ServiceResult<UserDto>> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(_currentUser.UserId, cancellationToken);

        return user is null
            ? ServiceResult<UserDto>.NotFound(ErrorMessages.UserNotFound)
            : ServiceResult<UserDto>.Ok(_mapper.Map<UserDto>(user));
    }

    public async Task<ServiceResult> UpdateMeAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(_currentUser.UserId, cancellationToken);

        if (user is null)
        {
            return ServiceResult.NotFound(ErrorMessages.UserNotFound);
        }

        user.FullName = request.FullName.Trim();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<UserDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var me = _currentUser.UserId;

        var allowed = id == me
            || _currentUser.IsSystemAdmin
            || await _unitOfWork.Users.ShareFamilyAsync(me, id, cancellationToken);

        if (!allowed)
        {
            return ServiceResult<UserDto>.Forbidden(ErrorMessages.CannotViewUser);
        }

        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);

        return user is null
            ? ServiceResult<UserDto>.NotFound(ErrorMessages.UserNotFound)
            : ServiceResult<UserDto>.Ok(_mapper.Map<UserDto>(user));
    }
}
