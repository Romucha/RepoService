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
using RepoService.DataAccess;

namespace RepoService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ILogger<ProductsController> _logger;

        private readonly IConfiguration _configuration;

        private readonly RepoDbContext _repoDbContext;

        private readonly string _repositoryLocation;

        public ProductsController(ILogger<ProductsController> logger, IConfiguration configuration, RepoDbContext repoDbContext)
        {
            this._logger = logger;
            this._configuration = configuration;
            this._repoDbContext = repoDbContext;
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
            _repoDbContext.Products.Add(null);
            /*
            if (string.IsNullOrEmpty(repoDir)
                || !Directory.Exists(repoDir)
                || !Directory.Exists(productsDir))
            {
                return NotFound();
            }
            var jsonFiles = Directory.EnumerateFiles(productsDir, "*.json", SearchOption.AllDirectories);
            List<ProductModel> models = new();
            foreach (var jsonFile in jsonFiles)
            {
                ProductModel? model = null;
                try
                {
                    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
                    using (FileStream fs = new FileStream(jsonFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        model = await JsonSerializer.DeserializeAsync<ProductModel>(fs);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
                models.Add(model);
            }
            if (!string.IsNullOrEmpty(productName))
            {
                models = models.Where(c => c.Name.ToLower().Contains(productName.ToLower())).ToList();
            }
            return Ok(models);
            */
            return NotFound();
        }

        [HttpGet("{guid}")]
        public async Task<ActionResult<ProductModel>> GetByGuid(string guid, bool download = false)
        {
            /*
            if (Guid.TryParse(guid, out Guid productGuid)
                || !string.IsNullOrEmpty(repoDir)
                    || Directory.Exists(repoDir)
                    || Directory.Exists(productsDir))
            {
                var jsonFiles = Directory.EnumerateFiles(productsDir, "*.json", SearchOption.AllDirectories);
                foreach (var jsonFile in jsonFiles)
                {
                    ProductModel? model = null;
                    try
                    {
                        using (FileStream fs = new FileStream(jsonFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            model = await JsonSerializer.DeserializeAsync<ProductModel>(fs);
                            if (model.UpgradeCode == productGuid)
                            {
                                if (download)
                                {
                                    string filePath = model.ShareFilePath;
                                    string fileName = Path.GetFileName(model.ShareFilePath);

                                    byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

                                    return File(fileBytes, "application/force-download", fileName);
                                }
                                return model;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                }
            }
            return NotFound(guid);
            */
            return NotFound();
        }

        //upload file with curl like this:
        //curl -v -X POST -F "files=@path\to\file\filename.ext" https://localhost:7193/api/products
        [HttpPost]
        public async Task<ActionResult<ProductModel>> Post(List<IFormFile> files)
        {
            /*
            //look for msi files
            //if there are none or more than one, notify about incorrect input
            //else
            //copy msi file to temp dir
            //find package code of the msi files, create product model
            //create %package_code% directory
            //copy all files into new directory
            //create json file with product model inside the new directory
            //return ok with product model
            //long size = files.Sum(f => f.Length);

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
                //copy msi file to temp dir
                var tempmsi = Path.Combine(tempMsiDir, msifile.FileName);

                using (FileStream fs = System.IO.File.Create(tempmsi))
                {
                    await msifile.CopyToAsync(fs);
                }
                //read msi properties into product model
                ProductModel model;
                string msiFinalDir = string.Empty;
                string msiFinalName = string.Empty;
                Guid upgradeCode;
                using (InstallPackage package = new InstallPackage(tempmsi, DatabaseOpenMode.ReadOnly))
                {
                    upgradeCode = new Guid(package.Property["UpgradeCode"]);
                    //user revision number to receive package code
                    msiFinalDir = Path.Combine(productsDir, upgradeCode.ToString());
                    msiFinalName = Path.Combine(msiFinalDir, msifile.FileName);
                    if (!Directory.Exists(msiFinalDir))
                    {
                        Directory.CreateDirectory(msiFinalDir);
                    }
                    model = new ProductModel()
                    {
                        FileDirectory = msiFinalDir,
                        FilePath = msiFinalName,
                        UpgradeCode = upgradeCode,
                        ProductCode = new Guid(package.Property["ProductCode"]),
                        IsX64 = package.SummaryInfo.Template.Contains("64"),
                        IsAsconProduct = package.SummaryInfo.Keywords.ToLower().Contains("ascon") ? 1 : 0,
                        IsArchive = false,
                        ShareFilePath = msiFinalName,
                        Name = package.Property["ProductName"],
                    };
                }
                //msi stream can't be read again and must be copied
                //if file already exists, error shows up
                if (!System.IO.File.Exists(msiFinalName))
                {
                    System.IO.File.Copy(tempmsi, msiFinalName);
                    //write all files to product directory
                    foreach (var file in files)
                    {
                        using (FileStream fs = System.IO.File.Create(tempmsi))
                        {
                            await file.CopyToAsync(fs);
                        }
                    }

                    //create json file
                    System.IO.File.WriteAllText(Path.Combine(msiFinalDir, $"{upgradeCode}.json"), JsonSerializer.Serialize(model));
                    //delete leftovers
                    if (Directory.Exists(tempMsiDir))
                    {
                        Directory.Delete(tempMsiDir, true);
                    }
                    return Ok(model);
                }
            }

            return BadRequest(files);
            */
            return NotFound();
        }

        //use
        //curl -v -X DELETE https://localhost:7193/api/products/:guid
        [HttpDelete("{guid}")]
        public async Task<ActionResult<ProductModel>> Delete(string guid)
        {
            /*
            if (string.IsNullOrEmpty(repoDir)
                || !Directory.Exists(repoDir)
                || !Directory.Exists(productsDir))
            {
                return NotFound();
            }
            Guid msiGuid = new Guid(guid);
            string msiDir = Path.Combine(productsDir, msiGuid.ToString());
            if (!Directory.Exists(msiDir))
            {
                return NotFound();
            }
            string msiJsonFile = Path.Combine(msiDir, $"{msiGuid}.json");
            if (!System.IO.File.Exists(msiJsonFile))
            {
                return NotFound();
            }
            ProductModel model;
            using (FileStream fs = new FileStream(msiJsonFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                model = await JsonSerializer.DeserializeAsync<ProductModel>(fs);
            }
            Directory.Delete(msiDir, true);
            return Ok(model);
            */
            return NotFound();
        }
    }
}
