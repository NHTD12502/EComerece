using Catalog.Core.Entities;
using Catalog.Core.Repositories;
using Catalog.Core.Specifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Catalog.Infrastructure.Settings;


namespace Catalog.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    // private readonly IMongoCollection<Product> _products;
    // private readonly IMongoCollection<ProductBrand> _brands;
    // private readonly IMongoCollection<ProductType> _types;


    // public ProductRepository(IConfiguration config)
    // {
    //     var client = new MongoClient(config["DatabaseSettings:ConnectionString"]);
    //     var db = client.GetDatabase(config["DatabaseSettings:DatabaseName"]);
    //     _types = db.GetCollection<ProductType>(config["DatabaseSettings:TypeCollectionName"]);
    //     _brands = db.GetCollection<ProductBrand>(config["DatabaseSettings:BrandCollectionName"]);
    //     _products = db.GetCollection<Product>(config["DatabaseSettings:ProductCollectionName"]);
    // }
    private readonly IMongoCollection<ProductBrand> _brands;
    private readonly IMongoCollection<ProductType> _types;
    private readonly IMongoCollection<Product> _products;
    public ProductRepository(IOptions<DatabaseSettings> options)
    {
        var settings = options.Value;
        var client = new MongoClient(settings.ConnectionString);
        var db = client.GetDatabase(settings.DatabaseName);
        _brands = db.GetCollection<ProductBrand>(settings.BrandCollectionName);
        _types = db.GetCollection<ProductType>(settings.TypeCollectionName);
        _products = db.GetCollection<Product>(settings.ProductCollectionName);
    }

    public async Task<Product> CreateProduct(Product product)
    {
        await _products.InsertOneAsync((product));
        return product;
    }

    public async Task<bool> DeleteProduct(string productId)
    {
        var deleteProduct = await _products
                                    .DeleteOneAsync(x => x.Id == productId);
        return deleteProduct.IsAcknowledged && deleteProduct.DeletedCount > 0;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _products
            .Find(_ =>true)
            .ToListAsync();
    }

    public async Task<ProductBrand> GetBrandByIdAsync(string brandId)
    {
        return await _brands
        .Find(x => x.Id == brandId)
        .FirstOrDefaultAsync();


    }

    public async Task<Product> GetProduct(string productId)
    {
        return await _products
        .Find(p=> p.Id ==productId)
        .FirstOrDefaultAsync();

    }

    public async Task<Pagination<Product>> GetProducts(CatalogSpecParams catalogSpecParams)
    {
        var builder = Builders<Product>.Filter;
        var filter = builder.Empty;
        if (!string.IsNullOrEmpty(catalogSpecParams.Search))
        {
            filter &= builder.Where(p => p.Name.ToLower().Contains(catalogSpecParams.Search.ToLower()));
        }
        if (!string.IsNullOrEmpty(catalogSpecParams.BrandId))
        {
            filter &= builder.Eq(p => p.Brand.Id, catalogSpecParams.BrandId);
        }
        if (!string.IsNullOrEmpty(catalogSpecParams.TypeId))
        {
            filter &= builder.Eq(p => p.Type.Id, catalogSpecParams.TypeId);
        }

        var totalItems = await _products.CountDocumentsAsync(filter);
        var data = await ApplyDataFilters(catalogSpecParams, filter);
        return new Pagination<Product>(
            catalogSpecParams.PageIndex,
            catalogSpecParams.PageSize,
            (int)totalItems,
            data
        );
    }




    public async Task<IEnumerable<Product>> GetProductsByBrand(string name)
    {
        return await _products
            .Find(p => p.Brand.Name.ToLower() == name.ToLower())
            .ToListAsync();

    }

    public async Task<IEnumerable<Product>> GetProductsByName(string name)
    {
        var filter = Builders<Product>.Filter.Regex(p => p.Name
        , new BsonRegularExpression($".*{name}.*", "i"));
        return await _products
            .Find(filter)
            .ToListAsync();


    }

    public async Task<ProductType> GetTypeByIdAsync(string typeId)
    {
        return await _types
        .Find(p => p.Id == typeId)
        .FirstOrDefaultAsync();


    }

    public async Task<bool> UpdateProduct(Product product)
    {
        var updateProduct = await _products
            .ReplaceOneAsync(p => p.Id == product.Id, product);
        return updateProduct.IsAcknowledged && updateProduct.ModifiedCount > 0;


    }

    private async Task<IReadOnlyCollection<Product>> ApplyDataFilters(CatalogSpecParams catalogSpecParams, FilterDefinition<Product> filter)
    {
        var sortDefn = Builders<Product>.Sort.Ascending("Name");
        if(!string.IsNullOrEmpty(catalogSpecParams.Sort))
        {
            sortDefn = catalogSpecParams.Sort.ToLower() switch
            {
                "priceAsc" => Builders<Product>.Sort.Ascending(p => p.Price),
                "priceDesc" => Builders<Product>.Sort.Descending(p => p.Price),
                _ => Builders<Product>.Sort.Ascending(p => p.Name)
            };
        }
        return await _products
            .Find(filter)
            .Sort(sortDefn)
            .Skip((catalogSpecParams.PageIndex - 1) * catalogSpecParams.PageSize)
            .Limit(catalogSpecParams.PageSize)
            .ToListAsync();



    }

}
