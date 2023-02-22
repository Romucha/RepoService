using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RepoService.Models;

namespace RepoService.DataAccess
{
    public class RepoDbContext : DbContext
    {
        private readonly IConfiguration _configuration;

        private readonly ILogger _logger;

        public DbSet<ProductModel> Products { get; set; }

        public RepoDbContext(IConfiguration configuration, DbContextOptions<RepoDbContext> options, ILogger<RepoDbContext> logger) : base(options)
        {
            _configuration = configuration;
            _logger = logger;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            try
            {
                options.UseNpgsql(_configuration.GetConnectionString("DefaultConnection"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}