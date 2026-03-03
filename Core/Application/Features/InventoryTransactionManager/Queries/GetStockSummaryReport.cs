using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.InventoryTransactionManager.Queries;


public record GetStockSummaryReportDto
{
    public string? WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public int TotalProducts { get; init; }
    public double? TotalStock { get; init; }
    public double? TotalStockValue { get; init; }
}


public class GetStockSummaryReportProfile : Profile
{
    public GetStockSummaryReportProfile()
    {
    }
}

public class GetStockSummaryReportResult
{
    public List<GetStockSummaryReportDto>? Data { get; init; }
}

public class GetStockSummaryReportRequest : IRequest<GetStockSummaryReportResult>
{
}


public class GetStockSummaryReportHandler : IRequestHandler<GetStockSummaryReportRequest, GetStockSummaryReportResult>
{
    private readonly IQueryContext _context;

    public GetStockSummaryReportHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetStockSummaryReportResult> Handle(GetStockSummaryReportRequest request, CancellationToken cancellationToken)
    {
        var stockByProduct = _context
            .InventoryTransaction
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Include(x => x.Warehouse)
            .Include(x => x.Product)
            .Where(x =>
                x.Product!.Physical == true &&
                x.Warehouse!.SystemWarehouse == false &&
                x.Status == Domain.Enums.InventoryTransactionStatus.Confirmed
            )
            .GroupBy(x => new { x.WarehouseId, x.ProductId })
            .Select(group => new
            {
                WarehouseId = group.Key.WarehouseId,
                WarehouseName = group.Max(x => x.Warehouse!.Name),
                ProductId = group.Key.ProductId,
                Stock = group.Sum(x => x.Stock),
                UnitPrice = group.Max(x => x.Product!.UnitPrice)
            });

        var query = stockByProduct
            .GroupBy(x => new { x.WarehouseId, x.WarehouseName })
            .Select(group => new GetStockSummaryReportDto
            {
                WarehouseId = group.Key.WarehouseId,
                WarehouseName = group.Key.WarehouseName,
                TotalProducts = group.Count(),
                TotalStock = group.Sum(x => x.Stock),
                TotalStockValue = group.Sum(x => x.Stock * x.UnitPrice)
            })
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        return new GetStockSummaryReportResult
        {
            Data = entities
        };
    }
}
