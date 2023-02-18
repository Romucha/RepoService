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

namespace RepoService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ILogger<ProductsController> _logger;

        private readonly IConfiguration _configuration;

        private readonly string repoDir;
        private readonly string productsDir;

        public ProductsController(ILogger<ProductsController> logger, IConfiguration configuration)
        {
            this._logger = logger;
            this._configuration = configuration;
            repoDir = _configuration["RepoStorage"];
            productsDir = Path.Combine(repoDir, "Products");
        }

        [HttpGet]
        public async IAsyncEnumerable<ProductModel?> Get()
        {
            if (string.IsNullOrEmpty(repoDir)
                || !Directory.Exists(repoDir)
                || !Directory.Exists(productsDir))
            {
                yield break;
            }
            var jsonFiles = Directory.EnumerateFiles(productsDir, "*.json", SearchOption.AllDirectories);
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
                catch(Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
                yield return model;
            }
        }

        [HttpGet("{guid}")]
        public async Task<ProductModel> Get(string guid)
        {
            if (Guid.TryParse(guid, out Guid productGuid)
                || string.IsNullOrEmpty(repoDir)
                    || !Directory.Exists(repoDir)
                    || !Directory.Exists(productsDir))
            {
                var jsonFiles = Directory.EnumerateFiles(productsDir, "*.json", SearchOption.AllDirectories);
                foreach (var jsonFile in jsonFiles)
                {
                    ProductModel? model = null;
                    try
                    {
                        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
                        using (FileStream fs = new FileStream(jsonFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            model = await JsonSerializer.DeserializeAsync<ProductModel>(fs);
                            if (model.PackageCode == productGuid)
                            {
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
            return null;
        }

        //upload file with curl like this:
        //curl -v -X POST -F "files=@path\to\file\filename.ext" https://localhost:7193/api/products
        [HttpPost]
        public async Task<IActionResult> Post(List<IFormFile> files)
        {
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

            return NotFound(new { error = "need 1 msi" });
        }
    }
}
