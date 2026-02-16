using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Domain.Service.Contract.Dto.PaginationDto;
using Humanizer;
using Microsoft.EntityFrameworkCore;

namespace Domain.Service.Extension;

[ExcludeFromCodeCoverage]
public static class QueryableExtension
{
    extension<T>(IQueryable<T> query)
    {
        public IQueryable<T> ProcessPaginationRequest(PaginationServiceRequest request)
        {
            return query
                .ApplyFiltering(request.FilterServiceRequests)
                .ApplySorting(request.SortServiceRequest)
                .ApplyPagination(request.PageNumber, request.PageSize);
        }

        public PaginationServiceResponse<T> ToPagedResponse(
            int pageNumber,
            int pageSize,
            Func<int> totalCountAction)
        {
            var totalPages = (int)Math.Ceiling((double)totalCountAction() / pageSize);

            return new PaginationServiceResponse<T>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber < totalPages,
                TotalPages = totalPages,
                Data = query.ToArray(),
            };
        }

        public async Task<PaginationServiceResponse<T>> ToPagedResponseAsync(
            int pageNumber,
            int pageSize,
            Func<CancellationToken, Task<int>> totalCountFunc,
            CancellationToken ct)
        {
            var totalPages = (int)Math.Ceiling((double)await totalCountFunc(ct) / pageSize);

            return new PaginationServiceResponse<T>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber < totalPages,
                TotalPages = totalPages,
                Data = query.ToArray(),
            };
        }

        private IQueryable<T> ApplyPagination(
            int pageNumber,
            int pageSize)
        {
            return query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);
        }

        private IQueryable<T> ApplySorting(params IEnumerable<SortServiceRequest> sortRequests)
        {
            var hasFirstSort = false;

            foreach (var sortRequest in sortRequests)
            {
                if (!hasFirstSort)
                {
                    query = !sortRequest.IsDescending
                        ? query.OrderBy(x => EF.Property<T>(x!, sortRequest.PropertyName.Dehumanize()))
                        : query.OrderByDescending(x => EF.Property<T>(x!, sortRequest.PropertyName.Dehumanize()));

                    hasFirstSort = true;
                }
                else
                {
                    query = !sortRequest.IsDescending
                        ? (query as IOrderedQueryable<T>)!.ThenBy(x =>
                            EF.Property<T>(x!, sortRequest.PropertyName.Dehumanize()))
                        : (query as IOrderedQueryable<T>)!.ThenByDescending(x =>
                            EF.Property<T>(x!, sortRequest.PropertyName.Dehumanize()));
                }
            }

            return query;
        }

        private IQueryable<T> ApplyFiltering(params IEnumerable<FilterServiceRequest> filterRequests)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var expressionTreeResult = BuildExpressionTree(filterRequests, parameter);

            if (expressionTreeResult is not null)
            {
                query = query.Where(Expression.Lambda<Func<T, bool>>(expressionTreeResult, parameter));
            }

            return query;

            // ReSharper disable once VariableHidesOuterVariable
            Expression? BuildExpressionTree(IEnumerable<FilterServiceRequest> filterRequests, ParameterExpression param)
            {
                Expression? expression = null;

                foreach (var filterRequest in filterRequests)
                {
                    var expressionResult = BuildExpression(filterRequest, param);
                    // ReSharper disable once VariableHidesOuterVariable
                    var expressionTreeResult = BuildExpressionTree(filterRequest.Filters, param);

                    if (expression is null)
                    {
                        expression = expressionResult;
                    }
                    else
                    {
                        expression = filterRequest.FilterLogic switch
                        {
                            FilterServiceRequest.Logic.And => Expression.AndAlso(expression, expressionResult),
                            FilterServiceRequest.Logic.Or => Expression.OrElse(expression, expressionResult),
                            _ => throw new ArgumentOutOfRangeException(),
                        };
                    }

                    if (expressionTreeResult is not null)
                    {
                        expression = filterRequest.FilterLogic switch
                        {
                            FilterServiceRequest.Logic.And => Expression.AndAlso(expression, expressionTreeResult),
                            FilterServiceRequest.Logic.Or => Expression.OrElse(expression, expressionTreeResult),
                            _ => throw new ArgumentOutOfRangeException(),
                        };
                    }
                }

                return expression;

                Expression BuildExpression(FilterServiceRequest filterRequest, ParameterExpression param)
                {
                    object? typedValue;
                    
                    var property = Expression.Property(param, filterRequest.PropertyName.Dehumanize());
                    var targetType = property.Type;

                    try
                    {
                        typedValue = TypeDescriptor.GetConverter(targetType)
                            .ConvertFromInvariantString(filterRequest.Value);
                    }
                    catch
                    {
                        typedValue = Activator.CreateInstance(targetType);
                    }
                    
                    var constant = Expression.Constant(typedValue, targetType);
                    var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;

                    return filterRequest.FilterOperator switch
                    {
                        FilterServiceRequest.Operator.Equal => Expression.Equal(
                            property,
                            constant),
                        FilterServiceRequest.Operator.NotEqual => Expression.NotEqual(
                            property,
                            constant),
                        FilterServiceRequest.Operator.Contains => Expression.Call(
                            Expression.Call(property, toLowerMethod),
                            typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!,
                            Expression.Call(constant, toLowerMethod)),
                        FilterServiceRequest.Operator.NotContains => Expression.Not(Expression.Call(
                            Expression.Call(property, toLowerMethod),
                            typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!,
                            Expression.Call(constant, toLowerMethod))),
                        FilterServiceRequest.Operator.StartWith => Expression.Call(
                            Expression.Call(property, toLowerMethod),
                            typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!,
                            Expression.Call(constant, toLowerMethod)),
                        FilterServiceRequest.Operator.EndWith => Expression.Call(
                            Expression.Call(property, toLowerMethod),
                            typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!,
                            Expression.Call(constant, toLowerMethod)),
                        FilterServiceRequest.Operator.GreaterThan => Expression.GreaterThan(
                            property,
                            constant),
                        FilterServiceRequest.Operator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(
                            property,
                            constant),
                        FilterServiceRequest.Operator.LessThan => Expression.LessThan(
                            property,
                            constant),
                        FilterServiceRequest.Operator.LessThanOrEqual => Expression.LessThanOrEqual(
                            property,
                            constant),
                        _ => throw new ArgumentOutOfRangeException(),
                    };
                }
            }
        }
    }
}