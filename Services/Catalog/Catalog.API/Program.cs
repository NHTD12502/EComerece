using System.Reflection;
using Catalog.Application.Handlers;
using Catalog.Core.Repositories;
using Catalog.Infrastructure.Data;
using Catalog.Infrastructure.Repositories;
using Catalog.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

//Register custom serializer
BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(BsonType.String));


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


//RegisterMediatR
var assemblies = new Assembly[]
{
    Assembly.GetExecutingAssembly(),
    typeof(GetAllBrandsHandler).Assembly,
};
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(assemblies));

//Custom services
builder.Services.AddScoped<IBrandRepository, BrandRepository>();
builder.Services.AddScoped<ITypeRepository, TypeRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();


//Bind strongly-type setting
builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection("DatabaseSettings"));

//Register MongoClient as singleton
builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});


var app = builder.Build();

//Seed mongo db on startup
using (var scope = app.Services.CreateScope())
{
    var options = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseSettings>>();
    await DatabaseSeeder.SeedAsync(options);
}

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//Enable Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
