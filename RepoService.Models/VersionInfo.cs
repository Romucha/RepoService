using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoService.Models
{
    public class VersionInfo
    {
        public Version Version { get; set; } = new Version();

        public VersionInfo() => GetVersionInfo(0);
        public VersionInfo(string Version) => GetVersionInfo(Version);
        public VersionInfo(Version Version) => GetVersionInfo(Version);
        public VersionInfo(int Major, int Minor = 0, int Build = 0, int Revision = 0) => GetVersionInfo(Major, Minor, Build, Revision);

        private void GetVersionInfo(string Version)
        {
            GetVersionInfo(new Version(Version));
        }
        private void GetVersionInfo(Version Version)
        {
            int Major = Version.Major < 0 ? 0 : Version.Major;
            int Minor = Version.Minor < 0 ? 0 : Version.Minor;
            int Build = Version.Build < 0 ? 0 : Version.Build;
            int Revision = Version.Revision < 0 ? 0 : Version.Revision;

            this.Version = new Version(Major, Minor, Build, Revision);
        }
        private void GetVersionInfo(int Major, int Minor = 0, int Build = 0, int Revision = 0)
        {
            this.Version = new Version(Major, Minor, Build, Revision);
        }

        public override string ToString()
        {
            return Version.ToString();
        }
    }
}
