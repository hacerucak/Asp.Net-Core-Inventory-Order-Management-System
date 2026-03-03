using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SalesOrderManager.Queries;


public record GetSalesSummaryReportDto
{
    public int TotalOrders { get; init; }
    public double? TotalAmount { get; init; }
    public double? AverageOrderAmount { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}


public class GetSalesSummaryReportProfile : Profile
{
    public GetSalesSummaryReportProfile()
    {
    }
}

public class GetSalesSummaryReportResult
{
    public GetSalesSummaryReportDto? Data { get; init; }
}

public class GetSalesSummaryReportRequest : IRequest<GetSalesSummaryReportResult>
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}


public class GetSalesSummaryReportHandler : IRequestHandler<GetSalesSummaryReportRequest, GetSalesSummaryReportResult>
{
    private readonly IQueryContext _context;

    public GetSalesSummaryReportHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetSalesSummaryReportResult> Handle(GetSalesSummaryReportRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .SalesOrder
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.OrderStatus == Domain.Enums.SalesOrderStatus.Confirmed)
            .AsQueryable();

        if (request.StartDate.HasValue)
        {
            query = query.Where(x => x.OrderDate >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(x => x.OrderDate <= request.EndDate.Value);
        }

        var orders = await query.ToListAsync(cancellationToken);

        var totalOrders = orders.Count;
        var totalAmount = orders.Sum(x => x.AfterTaxAmount ?? 0);
        var averageAmount = totalOrders > 0 ? totalAmount / totalOrders : 0;

        return new GetSalesSummaryReportResult
        {
            Data = new GetSalesSummaryReportDto
            {
                TotalOrders = totalOrders,
                TotalAmount = totalAmount,
                AverageOrderAmount = averageAmount,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            }
        };
    }
}
