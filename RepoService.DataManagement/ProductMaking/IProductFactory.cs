using RepoService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoService.DataManagement.ProductMaking
{
    public interface IProductFactory
    {
        public ProductModel GetFromMsi(string PathToMsi);

        public ProductModel GetFromZip(string PathToZip);
    }
}
