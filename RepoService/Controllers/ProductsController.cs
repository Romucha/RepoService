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
using RepoService.DataManagement;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO.Compression;
using RepoService.DataManagement.ProductMaker;
using RepoService.DataManagement.RepositoryLocator;
using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore;

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

        private readonly IRepositoryLocation _repositoryLocation;

        public ProductsController(ILogger<ProductsController> logger, IConfiguration configuration, RepoDbContext repoDbContext, IProductFactory productFactory, IRepositoryLocation repositoryLocation)
        {
            this._logger = logger;
            this._configuration = configuration;
            this._repoDbContext = repoDbContext;
            this._productFactory = productFactory;
            _repositoryLocation = repositoryLocation;
        }

        //Get with search by product name
        [HttpGet]
        public async Task<ActionResult<IAsyncEnumerable<ProductModel>>> Get(string? productName)
        {
            IQueryable<ProductModel> products = _repoDbContext.Products;
            if (!string.IsNullOrEmpty(productName))
            {
                products = products.Where(c => c.ProductName.ToLower().Contains(productName.ToLower()));
            }
            return Ok(products);
        }

        //get by guid and optionally download file
        [HttpGet("{guid}")]
        public async Task<ActionResult<ProductModel>> GetByGuid(string guid, bool download = false)
        {
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
                string filePath = Path.Combine(_repositoryLocation.Location, fileName);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(filePath);
                }
                return new PhysicalFileResult(filePath, "application/octet-stream")
                {
                    FileDownloadName = fileName,
                };
                    //(await System.IO.File.ReadAllBytesAsync(filePath), "application/force-download", fileName, true);
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
            ProductModel model = await parseInputFiles(files);
            if (model != null)
            {
                await _repoDbContext.Products.AddAsync(model);
                await _repoDbContext.SaveChangesAsync();
                return Ok(model);
            }

            return StatusCode(500, files);
        }

        //delete
        //curl -v -X DELETE https://localhost:7193/api/products/:guid
        [HttpDelete("{guid}")]
        public async Task<ActionResult<ProductModel>> Delete(string guid)
        {
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
            string msiZip = Path.Combine(_repositoryLocation.Location, zipName);
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
                return StatusCode(500, ex.Message);
            }
        }

        //put
        //curl -v -X PUT -F "files=@path\to\file\filename.ext" https://localhost:7193/api/products
        public async Task<ActionResult<ProductModel>> Put(List<IFormFile> files)
        {
            ProductModel model = await parseInputFiles(files, true);
            if (model != null)
            {
                if (_repoDbContext.Products.Where(c => c.PackageCode == model.PackageCode).Any())
                {
                    _repoDbContext.Update(model);
                    await _repoDbContext.SaveChangesAsync();
                    return Ok(model);
                }
            }

            return StatusCode(500, files);
        }


        private async Task<ProductModel> parseInputFiles(List<IFormFile> files, bool overWriteFile = false)
        {
            /*
             * Option 1 - msi file:
             * 1. Look for msi file.
             * 2. If there's only 1 msi file, copy all files to temp dir.
             * 3. Give temp dir location to product factory.
             * 4. Inside product factory:
             * 4.1. Look for a msi file in the directory.
             * 4.2. Parse file into product model.
             * 4.3. Add all files into zip archive placed into products directory. If archive already exists, it gets ovewritten.
             * 4.4. Delete temp dir and return model.
             * 5. Update database.
             * 
             * Option 2 - zip acrhive:
             * 1. Check if zip archive is a single entry.
             * 2. Copy archive to temp directory.
             * 3. Give archive location to product factory.
             * 4. Inside product factory:
             * 4.1. Extract archive into another temp directory
             * 4.2. Pass directory location to option 1 method.
             * 4.3. Delete first temp directory with archive.
             * 5. Update database.
             */
            try
            {
                ProductModel model = null;
                var msifiles = files.Where(c => c.FileName.EndsWith(".msi"));
                var zipfiles = files.Where(c => c.FileName.EndsWith(".zip"));
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
                    model = _productFactory.GetFromMsi(tempMsiDir, overWriteFile);
                }
                else if (zipfiles.Count() == 1)
                {
                    var zipfile = zipfiles.First();
                    var tempZipFile = Path.Combine(Path.GetTempPath(), zipfile.FileName);
                    using (FileStream fs = System.IO.File.Create(tempZipFile))
                    {
                        await zipfile.CopyToAsync(fs);
                    }
                    model = _productFactory.GetFromZip(tempZipFile, overWriteFile);
                }
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
