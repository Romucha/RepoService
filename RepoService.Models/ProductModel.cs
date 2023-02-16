using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoService.Models
{
    public class ProductModel
    {
        public int Type { get; set; }
        public string Name { get; set; }
        public Version Version { get; set; }
        public string FilePath { get; set; }
        public string FileDirectory { get; set; }
        public string ShareFilePath { get; set; }
        public Guid ProductCode { get; set; }
        public Guid UpgradeCode { get; set; }
        public Guid PackageCode { get; set; }
        public Guid PatchCode { get; set; }
        public Guid TargetProductCode { get; set; }
        public Version TargetVersion { get; set; }
        public bool IsX64 { get; set; }
        public bool IsArchive { get; set; }
        public List<object> Childs { get; set; }
        public List<Scenario> Scenarios { get; set; }
        public object Arguments { get; set; }
        public int IsAsconProduct { get; set; }
    }

    public class Scenario
    {
        public int Type { get; set; }
        public string Name { get; set; }
        public object Version { get; set; }
        public object FilePath { get; set; }
        public object FileDirectory { get; set; }
        public object ShareFilePath { get; set; }
        public Guid ProductCode { get; set; }
        public Guid UpgradeCode { get; set; }
        public Guid PackageCode { get; set; }
        public Guid PatchCode { get; set; }
        public Guid TargetProductCode { get; set; }
        public Version TargetVersion { get; set; }
        public bool IsX64 { get; set; }
        public bool IsArchive { get; set; }
        public List<object> Childs { get; set; }
        public List<Scenario> Scenarios { get; set; }
        public object Arguments { get; set; }
        public int IsAsconProduct { get; set; }
    }
}
