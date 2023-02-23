using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using RepoService.Models;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Deployment.WindowsInstaller.Package;
using RepoService.DataManagement;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO.Compression;
using RepoService.DataManagement.ProductMaking;

namespace RepoService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ILogger<ProductsController> _logger;

        private readonly IConfiguration _configuration;

        private readonly RepoDbContext _repoDbContext;

        private readonly IProductFactory _productFactory;

        private readonly string _repositoryLocation;

        public ProductsController(ILogger<ProductsController> logger, IConfiguration configuration, RepoDbContext repoDbContext, IProductFactory productFactory)
        {
            this._logger = logger;
            this._configuration = configuration;
            this._repoDbContext = repoDbContext;
            this._productFactory = productFactory;
            _repositoryLocation = Path.Combine(
                _configuration["RepositoryLocation"] ?? 
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RepoService"),
                "Products");
            if (!Directory.Exists(_repositoryLocation))
            {
                Directory.CreateDirectory(_repositoryLocation);
            }
        }

        //Get with search by product name
        [HttpGet]
        public async Task<ActionResult<IAsyncEnumerable<ProductModel>>> Get(string? productName)
        {
            if (string.IsNullOrEmpty(_repositoryLocation)
                || !Directory.Exists(_repositoryLocation))
            {
                Directory.CreateDirectory(_repositoryLocation);
                return NotFound();
            }
            IQueryable<ProductModel> products = _repoDbContext.Products;
            if (!string.IsNullOrEmpty(productName))
            {
                products = products.Where(c => c.ProductName.ToLower().Contains(productName.ToLower()));
            }
            return Ok(products);
        }

        [HttpGet("{guid}")]
        public async Task<ActionResult<ProductModel>> GetByGuid(string guid, bool download = false)
        {
            if (string.IsNullOrEmpty(_repositoryLocation)
                || !Directory.Exists(_repositoryLocation))
            {
                Directory.CreateDirectory(_repositoryLocation);
                return NotFound();
            }
            Guid actualGuid;
            if (!Guid.TryParse(guid, out actualGuid))
            {
                return BadRequest(guid);
            }
            var product = _repoDbContext.Products.FirstOrDefault(c => c.PackageCode == actualGuid);
            if (product == null)
            {
                return NotFound(guid);
            }
            if (download)
            {
                string fileName = $"{product.PackageCode}.zip";
                string filePath = Path.Combine(_repositoryLocation, fileName);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(filePath);
                }
                return File(await System.IO.File.ReadAllBytesAsync(filePath), "application/force-download", fileName);
            }
            else
            {
                return Ok(product);
            }
        }

        //upload file with curl like this:
        //curl -v -X POST -F "files=@path\to\file\filename.ext" https://localhost:7193/api/products
        [HttpPost]
        public async Task<ActionResult<ProductModel>> Post(List<IFormFile> files)
        {

            /*
             * Option 1:
             * 1. Look for msi file.
             * 2. If there's only 1 msi file, copy all files to temp dir.
             * 3. Parse msi file into new ProductModel.
             * 4. Copy all files into zip archive.
             * 5. Delete temp dir.
             * 6. Update database.
             * 
             * Option 2:
             * 1. Zip archive... TODO
             */
            if (string.IsNullOrEmpty(_repositoryLocation)
                || !Directory.Exists(_repositoryLocation))
            {
                Directory.CreateDirectory(_repositoryLocation);
            }
            var msifiles = files.Where(c => c.FileName.EndsWith(".msi"));
            //check if there's exactly 1 msi file
            if (msifiles.Count() == 1)
            {
                var msifile = msifiles.First();
                var tempMsiDir = Path.Combine(Path.GetTempPath(), $"RepoService_{msifile.FileName}_{DateTime.Now.ToString("dd.MM.yyyy_hh.mm.ss")}");
                if (!Directory.Exists(tempMsiDir))
                {
                    Directory.CreateDirectory(tempMsiDir);
                }
                //copy ALL files to temp dir
                foreach (var file in files)
                {
                    var tempfile = Path.Combine(tempMsiDir, file.FileName);

                    using (FileStream fs = System.IO.File.Create(tempfile))
                    {
                        await file.CopyToAsync(fs);
                    }

                }
                //read msi properties into product model
                ProductModel model;
                using (InstallPackage package = new InstallPackage(Path.Combine(tempMsiDir, msifile.FileName), DatabaseOpenMode.ReadOnly))
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
                //create zip archive from temp directory
                try
                {
                    ZipFile.CreateFromDirectory(tempMsiDir, Path.Combine(_repositoryLocation, $"{model.PackageCode}.zip"));
                    await _repoDbContext.Products.AddAsync(model);
                    await _repoDbContext.SaveChangesAsync();
                    Directory.Delete(tempMsiDir, true);
                    return Ok(model);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    return BadRequest(ex.Message);
                }
            }

            return BadRequest(files);
        }

        //use
        //curl -v -X DELETE https://localhost:7193/api/products/:guid
        [HttpDelete("{guid}")]
        public async Task<ActionResult<ProductModel>> Delete(string guid)
        {
            
            if (string.IsNullOrEmpty(_repositoryLocation)
                || !Directory.Exists(_repositoryLocation))
            {
                Directory.CreateDirectory(_repositoryLocation);
                return NotFound();
            }
            Guid actualGuid;
            if (!Guid.TryParse(guid, out actualGuid))
            {
                return BadRequest(guid);
            }
            ProductModel model = _repoDbContext.Products.FirstOrDefault(c => c.PackageCode == actualGuid);
            if (model == null)
            {
                return NotFound(guid);
            }
            string zipName = $"{model.PackageCode}.zip";
            string msiZip = Path.Combine(_repositoryLocation, zipName);
            if (!System.IO.File.Exists(msiZip))
            {
                return NotFound(zipName);
            }
            try
            {
                System.IO.File.Delete(msiZip);
                _repoDbContext.Products.Remove(model);
                _repoDbContext.SaveChanges();
                return Ok(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }
    }
}
