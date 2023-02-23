using RepoService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoService.DataManagement.ProductMaker
{
    public interface IProductFactory
    {
        public ProductModel GetFromMsi(string PathToMsiDir, bool OverWriteFile = false);

        public ProductModel GetFromZip(string PathToZip, bool OverWriteFile = false);
    }
}
