using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codeworx.PackageVersion
{
    public class NextVersionResult
    {
        public NextVersionResult(int major, int minor, int build, int revision, string? prerelease)
        {
            this.Major = major;
            this.Minor = minor;
            this.Build = build;
            this.Revision = revision;
            this.PreRelease = prerelease;

            if (prerelease != null)
            {
                PackageVersion = $"{Major}.{Minor}.{Revision}-{PreRelease}-{Build:D5}";
            }
            else
            {
                PackageVersion = $"{Major}.{Minor}.{Revision}";
            }

            FileVersion = $"{Major}.{Minor}.{Build}.{Revision}";
        }

        public int Major { get; }

        public int Minor { get; }

        public int Build { get; }

        public int Revision { get; }

        public string? PreRelease { get; }

        public string PackageVersion { get; }

        public string FileVersion { get; }
    }
}
