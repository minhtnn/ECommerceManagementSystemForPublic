using System.Linq.Expressions;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Filter;

public interface IFilter<T>
{
    Expression<Func<T, bool>> ToExpression();
}