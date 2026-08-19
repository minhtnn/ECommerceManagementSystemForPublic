using AutoMapper;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Models.PromotionRules;

namespace ECommerceManagementSystem.Coffee.Application.Common.Mappers;

public class PromotionRuleMapper : Profile
{
    public PromotionRuleMapper()
    {
        #region Map PromotionRules to GetPromotionRulesResponse

        CreateMap<PromotionRules, GetPromotionRulesResponse>()
            .ForMember(dest => dest.Id, 
                opt =>
                    opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Code, 
                opt =>
                    opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Name, 
                opt =>
                    opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.ShortDescription, 
                opt =>
                    opt.MapFrom(src => src.ShortDescription))
            .ForMember(dest => dest.Priority, 
                opt =>
                    opt.MapFrom(src => src.Priority))
            .ForMember(dest => dest.StartDate, 
                opt =>
                    opt.MapFrom(src => src.StartDate))
            .ForMember(dest => dest.EndDate, 
                opt =>
                    opt.MapFrom(src => src.EndDate))
            .ForMember(dest => dest.Status, 
                opt =>
                    opt.MapFrom(src => src.Status));

        #endregion
        
        #region Mapp PromotionRules to GetPromotionRuleByIdResponse

        CreateMap<PromotionRules, GetPromotionRuleByIdResponse>()
            .ForMember(dest => dest.Id, 
                opt =>
                    opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Code, 
                opt =>
                    opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Name, 
                opt =>
                    opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.ShortDescription, 
                opt =>
                    opt.MapFrom(src => src.ShortDescription))
            .ForMember(dest => dest.Description, 
                opt =>
                    opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Status, 
                opt =>
                    opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.PromotionType, 
                opt =>
                    opt.MapFrom(src => src.PromotionType))
            .ForMember(dest => dest.Priority, 
                opt =>
                    opt.MapFrom(src => src.Priority))
            .ForMember(dest => dest.GlobalDiscountCap, 
                opt =>
                    opt.MapFrom(src => src.GlobalDiscountCap))
            .ForMember(dest => dest.StartDate, 
                opt =>
                    opt.MapFrom(src => src.StartDate))
            .ForMember(dest => dest.EndDate, 
                opt =>
                    opt.MapFrom(src => src.EndDate))
            .ForMember(dest => dest.CreatedDate, 
                opt =>
                    opt.MapFrom(src => src.CreatedDate))
            .ForMember(dest => dest.LastModifiedDate, 
                opt =>
                    opt.MapFrom(src => src.LastModifiedDate))
            .ForMember(dest => dest.RuleConditions, 
                opt =>
                    opt.MapFrom(src => src.RuleConditions))
            .ForMember(dest => dest.RuleActions, 
                opt =>
                    opt.MapFrom(src => src.RuleActions));

        #endregion

        #region Map RuleConditions to GetBrandPromotionRuleCondition

        CreateMap<RuleConditions,GetBrandPromotionRuleCondition>()
            .ForMember(dest => dest.Id, 
                opt =>
                    opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.PromotionRuleId, 
                opt =>
                    opt.MapFrom(src => src.PromotionRuleId))
            .ForMember(dest => dest.ConditionType, 
                opt =>
                    opt.MapFrom(src => src.ConditionType))
            .ForMember(dest => dest.Operator, 
                opt =>
                    opt.MapFrom(src => src.Operator))
            .ForMember(dest => dest.Value, 
                opt =>
                    opt.MapFrom(src => src.Value));

        #endregion

        #region Map RuleActions to GetBrandPromotionRuleAction

        CreateMap<RuleActions,GetBrandPromotionRuleAction>()
            .ForMember(dest => dest.Id, 
                opt =>
                    opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.PromotionRuleId, 
                opt =>
                    opt.MapFrom(src => src.PromotionRuleId))
            .ForMember(dest => dest.ActionType, 
                opt =>
                    opt.MapFrom(src => src.ActionType))
            .ForMember(dest => dest.Value, 
                opt =>
                    opt.MapFrom(src => src.Value))
            .ForMember(dest => dest.MaxDiscountAmountForPercentage, 
                opt =>
                    opt.MapFrom(src => src.MaxDiscountAmountForPercentage))
            .ForMember(dest => dest.RuleActionTargets, 
                opt =>
                    opt.MapFrom(src => src.RuleActionTargets));

        #endregion

        #region Map 

        CreateMap<RuleActionTargets, GetBrandPromotionRuleActionTargets>()
            .ForMember(dest => dest.Id, 
                opt =>
                    opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.RuleActionId, 
                opt =>
                    opt.MapFrom(src => src.RuleActionId))
            .ForMember(dest => dest.TargetType, 
                opt =>
                    opt.MapFrom(src => src.TargetType))
            .ForMember(dest => dest.TargetId, 
                opt =>
                    opt.MapFrom(src => src.TargetId))
            .ForMember(dest => dest.Quantity, 
                opt =>
                    opt.MapFrom(src => src.Quantity))
            .ForMember(dest => dest.Role, 
                opt =>
                    opt.MapFrom(src => src.Role));

        #endregion

    }
}