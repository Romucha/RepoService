using Microsoft.Extensions.Logging;
using RepoService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoService.DataManagement.ProductMaking
{
    public class ProductFactory : IProductFactory
    {
        private readonly ILogger<ProductFactory> _logger;

        public ProductFactory(ILogger<ProductFactory> logger) 
        {
            _logger = logger;
        }

        public ProductModel GetFromMsi(string PathToMsi)
        {
            throw new NotImplementedException();
        }

        public ProductModel GetFromZip(string PathToZip)
        {
            throw new NotImplementedException();
        }
    }
}
