using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.SystemConfigs.Command.UpdateSystemConfig;

public class UpdateSystemConfigCommandHandler : IRequestHandler<UpdateSystemConfigCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly IClaimService _claimService;
    private readonly ILogger _logger;

    public UpdateSystemConfigCommandHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        IClaimService claimService,
        ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _claimService = claimService;
        _logger = logger;
    }

    public async ValueTask<ApiResponse> Handle(UpdateSystemConfigCommand request, CancellationToken cancellationToken)
    {
        #region Authentication

        var role = _claimService.GetCurrentRoleEnum();
        if (role == null || role != ERole.SystemAdmin)
            return new ApiResponse { Status = StatusCodes.Status401Unauthorized, Message = "Bạn không có quyền này!" };

        #endregion

        #region Validate

        var configKey = await _unitOfWork.GetRepository<SystemConfigKeys>().SingleOrDefaultAsync(
            predicate: x => x.Id == request.Id
        );
        if (configKey == null)
            throw new BadHttpRequestException("System config không tồn tại");

        // Validate DefaultValue với DataType hiện tại
        if (!string.IsNullOrWhiteSpace(request.DefaultValue))
        {
            var isValidDefault = configKey.DataType switch
            {
                EConfigDataType.Boolean => bool.TryParse(request.DefaultValue, out _),
                EConfigDataType.Number  => decimal.TryParse(request.DefaultValue, out _),
                EConfigDataType.Json    => IsValidJson(request.DefaultValue),
                EConfigDataType.String  => true,
                _                       => false
            };
            if (!isValidDefault)
                throw new BadHttpRequestException($"DefaultValue không hợp lệ với kiểu dữ liệu {configKey.DataType}");
        }

        // Validate Value mới với DataType hiện tại
        if (!string.IsNullOrWhiteSpace(request.Value))
        {
            var isValidValue = configKey.DataType switch
            {
                EConfigDataType.Boolean => bool.TryParse(request.Value, out _),
                EConfigDataType.Number  => decimal.TryParse(request.Value, out _),
                EConfigDataType.Json    => IsValidJson(request.Value),
                EConfigDataType.String  => true,
                _                       => false
            };
            if (!isValidValue)
                throw new BadHttpRequestException($"Value không hợp lệ với kiểu dữ liệu {configKey.DataType}");
        }

        // Nếu IsRequired = true và đang ClearValue → không cho phép
        if (request.IsRequired && request.ClearValue)
            throw new BadHttpRequestException($"Key '{configKey.Key}' bắt buộc phải có value, không thể xóa");

        // Validate dependencies mới nếu có
        if (request.Dependencies != null && request.Dependencies.Any())
        {
            foreach (var dep in request.Dependencies)
            {
                // Không được tự phụ thuộc vào chính mình
                if (dep.TriggerKeyId == request.Id)
                    throw new BadHttpRequestException("Key không thể phụ thuộc vào chính nó");

                var triggerKey = await _unitOfWork.GetRepository<SystemConfigKeys>().SingleOrDefaultAsync(
                    predicate: x => x.Id == dep.TriggerKeyId
                );
                if (triggerKey == null)
                    throw new BadHttpRequestException($"TriggerKey '{dep.TriggerKeyId}' không tồn tại");

                var isValid = triggerKey.DataType switch
                {
                    EConfigDataType.Boolean => bool.TryParse(dep.TriggerValue, out _),
                    EConfigDataType.Number  => decimal.TryParse(dep.TriggerValue, out _),
                    EConfigDataType.String  => true,
                    _                       => false
                };
                if (!isValid)
                    throw new BadHttpRequestException(
                        $"TriggerValue '{dep.TriggerValue}' không hợp lệ với kiểu dữ liệu {triggerKey.DataType} của key '{triggerKey.Key}'"
                    );

                // Kiểm tra circular dependency
                var circular = await _unitOfWork.GetRepository<SystemConfigDependencies>().SingleOrDefaultAsync(
                    predicate: x => x.TriggerKeyId == request.Id && x.DependentKeyId == dep.TriggerKeyId
                );
                if (circular != null)
                    throw new BadHttpRequestException(
                        $"Phát hiện circular dependency với key '{triggerKey.Key}'"
                    );
            }

            var duplicateCheck = request.Dependencies
                .GroupBy(x => new { x.TriggerKeyId, x.TriggerValue })
                .Any(g => g.Count() > 1);
            if (duplicateCheck)
                throw new BadHttpRequestException("Tồn tại dependency rule trùng lặp trong request");
        }

        #endregion

        #region Update

        var beginResult = await _unitOfWork.BeginTransactionAsync();
        if (!beginResult.IsSuccess)
        {
            _logger.Error($"Failed to begin transaction: {beginResult.Message}");
            return new ApiResponse { Status = StatusCodes.Status500InternalServerError, Message = beginResult.Message };
        }

        // 1. Update key info
        configKey.Title           = request.Title.Trim();
        configKey.Description     = request.Description?.Trim();
        configKey.IsRequired      = request.IsRequired;
        configKey.DefaultValue    = request.DefaultValue?.Trim();
        configKey.DisplayOrder    = request.DisplayOrder;
        configKey.IsSecure    = request.IsSecure;
        configKey.LastModifiedDate = DateTime.UtcNow;
        _unitOfWork.GetRepository<SystemConfigKeys>().UpdateAsync(configKey);

        // 2. Upsert / clear value
        var existingValue = await _unitOfWork.GetRepository<SystemConfigValues>().SingleOrDefaultAsync(
            predicate: x => x.ConfigKeyId == request.Id
        );

        if (request.ClearValue)
        {
            if (existingValue != null)
                _unitOfWork.GetRepository<SystemConfigValues>().DeleteAsync(existingValue);
        }
        else if (!string.IsNullOrWhiteSpace(request.Value))
        {
            if (existingValue == null)
            {
                await _unitOfWork.GetRepository<SystemConfigValues>().InsertAsync(new SystemConfigValues
                {
                    Id               = Guid.CreateVersion7(),
                    ConfigKeyId      = request.Id,
                    Value            = request.Value.Trim(),
                    CreatedDate      = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                });
            }
            else
            {
                existingValue.Value            = request.Value.Trim();
                existingValue.LastModifiedDate = DateTime.UtcNow;
                _unitOfWork.GetRepository<SystemConfigValues>().UpdateAsync(existingValue);
            }
        }
        // Value == null → giữ nguyên, không làm gì

        // 3. Replace toàn bộ dependencies nếu client có truyền lên
        if (request.Dependencies != null)
        {
            var oldDeps = await _unitOfWork.GetRepository<SystemConfigDependencies>().GetListAsync(
                predicate: x => x.DependentKeyId == request.Id
            );
            foreach (var old in oldDeps)
                _unitOfWork.GetRepository<SystemConfigDependencies>().DeleteAsync(old);

            foreach (var dep in request.Dependencies)
            {
                await _unitOfWork.GetRepository<SystemConfigDependencies>().InsertAsync(new SystemConfigDependencies
                {
                    Id             = Guid.CreateVersion7(),
                    TriggerKeyId   = dep.TriggerKeyId,
                    TriggerValue   = dep.TriggerValue.Trim(),
                    DependentKeyId = request.Id,
                    CreatedDate    = DateTime.UtcNow
                });
            }
        }
        // Dependencies == null → giữ nguyên, không làm gì

        var commitResult = await _unitOfWork.CommitTransactionAsync();
        if (!commitResult.IsSuccess)
        {
            _logger.Error($"Transaction failed: {commitResult.Message}", commitResult.Exception);
            await _unitOfWork.RollbackTransactionAsync();
            throw new Exception("Không thể cập nhật system config!");
        }

        #endregion

        _logger.Information("Cập nhật SystemConfig thành công: Key={Key}", configKey.Key);
        return new ApiResponse
        {
            Status  = StatusCodes.Status200OK,
            Message = "Cập nhật system config thành công",
            Data    = configKey.Id
        };
    }

    private static bool IsValidJson(string value)
    {
        try { System.Text.Json.JsonDocument.Parse(value); return true; }
        catch { return false; }
    }
}