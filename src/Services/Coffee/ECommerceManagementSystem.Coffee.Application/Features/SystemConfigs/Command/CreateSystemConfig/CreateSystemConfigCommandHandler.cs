using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.SystemConfigs.Command.CreateSystemConfig;

public class CreateSystemConfigCommandHandler : IRequestHandler<CreateSystemConfigCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly IClaimService _claimService;
    private readonly ILogger _logger;

    public CreateSystemConfigCommandHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        IClaimService claimService,
        ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _claimService = claimService;
        _logger = logger;
    }

    public async ValueTask<ApiResponse> Handle(CreateSystemConfigCommand request, CancellationToken cancellationToken)
    {
        #region Authentication

        var role = _claimService.GetCurrentRoleEnum();
        if (role == null || role != ERole.SystemAdmin)
            return new ApiResponse { Status = StatusCodes.Status401Unauthorized, Message = "Bạn không có quyền này!" };

        #endregion

        #region Validate

        var existed = await _unitOfWork.GetRepository<SystemConfigKeys>().SingleOrDefaultAsync(
            predicate: x => x.Key == request.Key.Trim()
        );
        if (existed != null)
            throw new BadHttpRequestException($"Key '{request.Key}' đã tồn tại");

        // Validate tất cả TriggerKeyId trong dependencies có tồn tại không
        if (request.Dependencies != null && request.Dependencies.Any())
        {
            foreach (var dep in request.Dependencies)
            {
                var triggerKey = await _unitOfWork.GetRepository<SystemConfigKeys>().SingleOrDefaultAsync(
                    predicate: x => x.Id == dep.TriggerKeyId
                );
                if (triggerKey == null)
                    throw new BadHttpRequestException($"TriggerKey '{dep.TriggerKeyId}' không tồn tại");

                // TriggerValue phải hợp lệ với DataType của trigger key
                var isValid = triggerKey.DataType switch
                {
                    EConfigDataType.Boolean => bool.TryParse(dep.TriggerValue, out _),
                    EConfigDataType.Number => decimal.TryParse(dep.TriggerValue, out _),
                    EConfigDataType.String => true,
                    _ => false
                };
                if (!isValid)
                    throw new BadHttpRequestException(
                        $"TriggerValue '{dep.TriggerValue}' không hợp lệ với kiểu dữ liệu {triggerKey.DataType} của key '{triggerKey.Key}'"
                    );
            }

            // Kiểm tra duplicate trong chính request
            var duplicateCheck = request.Dependencies
                .GroupBy(x => new { x.TriggerKeyId, x.TriggerValue })
                .Any(g => g.Count() > 1);
            if (duplicateCheck)
                throw new BadHttpRequestException("Tồn tại dependency rule trùng lặp trong request");
        }

        #endregion

        #region Insert

        var beginResult = await _unitOfWork.BeginTransactionAsync();
        if (!beginResult.IsSuccess)
        {
            _logger.Error($"Failed to begin transaction: {beginResult.Message}");
            return new ApiResponse { Status = StatusCodes.Status500InternalServerError, Message = beginResult.Message };
        }

        // 1. Insert key
        var configKey = new SystemConfigKeys
        {
            Id = Guid.CreateVersion7(),
            Key = request.Key.Trim(),
            Title = request.Title.Trim(),
            DataType = request.DataType,
            Description = request.Description?.Trim(),
            IsRequired = request.IsRequired,
            IsSecure = request.IsSecure,
            DefaultValue = request.DefaultValue?.Trim(),
            DisplayOrder = request.DisplayOrder,
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };
        await _unitOfWork.GetRepository<SystemConfigKeys>().InsertAsync(configKey);

        // 2. Insert value nếu có
        var valueToInsert = request.Value ?? request.DefaultValue;
        if (!string.IsNullOrWhiteSpace(valueToInsert))
        {
            await _unitOfWork.GetRepository<SystemConfigValues>().InsertAsync(new SystemConfigValues
            {
                Id = Guid.CreateVersion7(),
                ConfigKeyId = configKey.Id,
                Value = valueToInsert.Trim(),
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            });
        }

        // 3. Insert dependencies nếu có
        if (request.Dependencies != null && request.Dependencies.Any())
        {
            foreach (var dep in request.Dependencies)
            {
                await _unitOfWork.GetRepository<SystemConfigDependencies>().InsertAsync(new SystemConfigDependencies
                {
                    Id = Guid.CreateVersion7(),
                    TriggerKeyId = dep.TriggerKeyId,
                    TriggerValue = dep.TriggerValue.Trim(),
                    DependentKeyId = configKey.Id,
                    CreatedDate = DateTime.UtcNow
                });
            }
        }

        var commitResult = await _unitOfWork.CommitTransactionAsync();
        if (!commitResult.IsSuccess)
        {
            _logger.Error($"Transaction failed: {commitResult.Message}", commitResult.Exception);
            await _unitOfWork.RollbackTransactionAsync();
            throw new Exception("Không thể tạo system config!");
        }

        #endregion

        _logger.Information("Tạo SystemConfig thành công: Key={Key}, HasValue={HasValue}, Dependencies={DepCount}",
            configKey.Key,
            !string.IsNullOrWhiteSpace(valueToInsert),
            request.Dependencies?.Count ?? 0);

        return new ApiResponse
        {
            Status = StatusCodes.Status201Created,
            Message = "Tạo system config thành công",
            Data = configKey.Id
        };
    }
}