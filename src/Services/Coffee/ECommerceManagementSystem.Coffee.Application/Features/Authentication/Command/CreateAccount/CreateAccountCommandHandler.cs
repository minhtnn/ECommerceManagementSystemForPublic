using AutoMapper;
using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.CreateAccount;

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;

    public CreateAccountCommandHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ICacheInvalidationService cacheInvalidation, ILogger logger, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _cacheInvalidation = cacheInvalidation;
        _logger = logger;
        _mapper = mapper;
    }

    public async ValueTask<ApiResponse> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var existedAccount = await _unitOfWork.GetRepository<Accounts>().SingleOrDefaultAsync(
            predicate: x => x.Username == request.Username || x.Email == request.Email
        );
        if (existedAccount != null)
            throw new BadHttpRequestException("Email hoặc tên đăng nhập đã được sử dụng trước đây");
        var account = _mapper.Map<Accounts>(request);
        account.Id = Guid.CreateVersion7();
        var (passwordHash, passwordSalt) = AuthenUtil.HashPassword(request.PasswordString);
        account.PasswordHash = passwordHash;
        account.PasswordSalt = passwordSalt;
        await _unitOfWork.GetRepository<Accounts>().InsertAsync(account);
        var isSuccess = await _unitOfWork.CommitAsync() > 0;
        if (!isSuccess) 
            throw new Exception("Có lỗi trong khi tạo tài khoản mới");

        return new ApiResponse()
        {
            Status = StatusCodes.Status201Created,
            Message = "Account has been created"
        };
    }
}