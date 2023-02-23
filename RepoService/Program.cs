using RepoService.DataManagement;
using RepoService.DataManagement.ProductMaker;
using RepoService.DataManagement.RepositoryLocator;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<RepoDbContext>();
builder.Services.AddTransient<IDbInitializer, DbInitializer>();
builder.Services.AddScoped<IProductFactory, ProductFactory>();

builder.Services.AddScoped<IRepositoryLocation, RepositoryLocation>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () =>
{
    return "Welcome to repository service!";
});
using (var scope = app.Services.CreateScope())
{
    ((IDbInitializer)scope.ServiceProvider.GetService(typeof(IDbInitializer))).Initialize();
}
app.Run();
