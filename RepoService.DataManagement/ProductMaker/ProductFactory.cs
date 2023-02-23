using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Deployment.WindowsInstaller.Package;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RepoService.DataManagement.RepositoryLocator;
using RepoService.Models;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoService.DataManagement.ProductMaker
{
    public class ProductFactory : IProductFactory
    {
        private readonly ILogger<ProductFactory> _logger;
        private readonly IConfiguration _configuration;

        private readonly IRepositoryLocation _repositoryLocation;

        public ProductFactory(ILogger<ProductFactory> logger, IConfiguration configuration, IRepositoryLocation repositoryLocation) 
        {
            _logger = logger;
            _configuration = configuration;
            _repositoryLocation = repositoryLocation;
        }

        public ProductModel GetFromMsi(string PathToMsiDir)
        {
            try
            {
                ProductModel model = null;
                var msiFile = Directory.GetFiles(PathToMsiDir, "*.msi").FirstOrDefault();
                if (msiFile != null)
                {
                    using (InstallPackage package = new InstallPackage(Path.Combine(PathToMsiDir, msiFile), DatabaseOpenMode.ReadOnly))
                    {
                        model = new ProductModel();
                        model.ARPCONTACT = package.Property[nameof(model.ARPCONTACT)];
                        model.ARPHELPLINK = package.Property[nameof(model.ARPHELPLINK)];
                        model.ARPHELPTELEPHONE = package.Property[nameof(model.ARPHELPTELEPHONE)];
                        model.ARPURLINFOABOUT = package.Property[nameof(model.ARPURLINFOABOUT)];
                        model.ARPURLUPDATEINFO = package.Property[nameof(model.ARPURLUPDATEINFO)];
                        model.Manufacturer = package.SummaryInfo.Author;
                        model.IsX64 = package.SummaryInfo.Template.Contains("64");
                        model.PackageCode = new Guid(package.SummaryInfo.RevisionNumber);
                        model.ProductCode = new Guid(package.Property[nameof(model.ProductCode)]);
                        model.ProductName = package.Property[nameof(model.ProductName)];
                        model.ProductVersion = package.Property[nameof(model.ProductVersion)];
                        model.UpgradeCode = new Guid(package.Property[nameof(model.UpgradeCode)]);
                    }
                    ZipFile.CreateFromDirectory(PathToMsiDir, Path.Combine(_repositoryLocation.Location, $"{model.PackageCode}.zip"));
                }
                return model;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
            finally
            {
                if (Directory.Exists(PathToMsiDir))
                {
                    Directory.Delete(PathToMsiDir, true);
                }
            }
        }

        public ProductModel GetFromZip(string PathToZip)
        {
            try
            {
                var tempZipDir = Path.Combine(Path.GetTempPath(), $"RepoService_{Path.GetFileName(PathToZip)}_{DateTime.Now.ToString("dd.MM.yyyy_hh.mm.ss")}");
                if (!Directory.Exists(tempZipDir))
                {
                    Directory.CreateDirectory(tempZipDir);
                }
                ZipFile.ExtractToDirectory(PathToZip, tempZipDir);
                ProductModel model = GetFromMsi(tempZipDir);
                File.Delete(PathToZip);
                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }
    }
}
