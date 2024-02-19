using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace Codeworx.PackageVersion
{
    public class PackageOptions
    {
        [Option('m', "major", Required = true, HelpText = "The major version of the package")]
        public int Major { get; set; }

        [Option('n', "minor", Required = true, HelpText = "The minor version of the package")]
        public int Minor { get; set; }

        [Option('b', "build-offset", Required = false, Default = 0, HelpText = "The build number start offset")]
        public int BuildNumberOffset { get; set; }

        [Option('p', "pre", Required = false, HelpText = "Pre realease flag! e.g. (beta1, rc1,...)")]
        public string? PreRelease { get; set; }

        [Option('o', "output", Required = false, HelpText = "The output formatter", Default = OutputFormatter.Text)]
        public OutputFormatter OutputFormatter { get; set; }


        [Value(0, MetaName = "package", Required = true, HelpText = "The package name.")]
        public string Package { get; set; } = null!;

    }
}
