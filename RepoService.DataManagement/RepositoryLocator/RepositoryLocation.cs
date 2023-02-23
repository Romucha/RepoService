using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoService.DataManagement.RepositoryLocator
{
    public class RepositoryLocation : IRepositoryLocation
    {
        private readonly IConfiguration _configuration;
        public string Location { get; }

        public RepositoryLocation(IConfiguration configuration)
        {
            _configuration = configuration;
            Location = Path.Combine(
                _configuration["RepositoryLocation"] ??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RepoService"),
                "Products");
            if (!Directory.Exists(Location))
            {
                Directory.CreateDirectory(Location);
            }
        }
    }
}
