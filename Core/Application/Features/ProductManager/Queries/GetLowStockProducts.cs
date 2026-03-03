using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProductManager.Queries;


public record GetLowStockProductsDto
{
    public string? ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? ProductNumber { get; init; }
    public string? WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public double? CurrentStock { get; init; }
    public int Threshold { get; init; }
}


public class GetLowStockProductsProfile : Profile
{
    public GetLowStockProductsProfile()
    {
    }
}

public class GetLowStockProductsResult
{
    public List<GetLowStockProductsDto>? Data { get; init; }
}

public class GetLowStockProductsRequest : IRequest<GetLowStockProductsResult>
{
    public int Threshold { get; init; } = 10;
}


public class GetLowStockProductsHandler : IRequestHandler<GetLowStockProductsRequest, GetLowStockProductsResult>
{
    private readonly IQueryContext _context;

    public GetLowStockProductsHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetLowStockProductsResult> Handle(GetLowStockProductsRequest request, CancellationToken cancellationToken)
    {
        var query = _context
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
            .Select(group => new GetLowStockProductsDto
            {
                WarehouseId = group.Key.WarehouseId,
                ProductId = group.Key.ProductId,
                WarehouseName = group.Max(x => x.Warehouse!.Name),
                ProductName = group.Max(x => x.Product!.Name),
                ProductNumber = group.Max(x => x.Product!.Number),
                CurrentStock = group.Sum(x => x.Stock),
                Threshold = request.Threshold
            })
            .Where(x => x.CurrentStock <= request.Threshold)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        return new GetLowStockProductsResult
        {
            Data = entities
        };
    }
}
