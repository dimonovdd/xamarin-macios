using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using Xamarin.Provisioning;
using Xamarin.Provisioning.Model;

// Provision Mono
//
// Overrides:
// * The current commit can be overridden by setting the PROVISION_FROM_COMMIT variable.

var commit = Environment.GetEnvironmentVariable ("BUILD_SOURCEVERSION");
var provision_from_commit = Environment.GetEnvironmentVariable ("PROVISION_FROM_COMMIT") ?? commit;


bool IsAtLeastVersion(string actualVer, string minVer)
{
    if (actualVer.Equals(minVer, StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    var actualVerChars = actualVer.ToCharArray();
    var minVerChars = minVer.ToCharArray();

    var length = actualVerChars.Length > minVerChars.Length ? minVerChars.Length : actualVerChars.Length;

    var i = 0;
    while (i < length)
    {
        if (actualVerChars[i] > minVerChars[i])
        {
            return true;
        }
        else if (minVerChars[i] > actualVerChars[i])
        {
            return false;
        }
        i++;
    }

    if (actualVerChars.Length == minVerChars.Length)
    {
        return true;
    }

    if (actualVerChars.Length > minVerChars.Length )
    {
        return true;
    }
    else
    {
        return false;
    }
}


// Looks for a variable either in the environment, or in current repo's Make.config.
// Returns null if the variable couldn't be found.
IEnumerable<string> make_config = null;
string FindConfigurationVariable (string variable, string hash = "HEAD")
{
	var value = Environment.GetEnvironmentVariable (variable);
	if (!string.IsNullOrEmpty (value))
		return value;

	if (make_config == null) {
		try {
			make_config = Exec ("git", "show", $"{hash}:Make.config");
		} catch {
			Console.WriteLine ("Could not find a Make.config");
			return null;	
		}
	}
	foreach (var line in make_config) {
		if (line.StartsWith (variable + "=", StringComparison.Ordinal))
			return line.Substring (variable.Length + 1);
	}

	return null;
}

string FindVariable (string variable)
{
	var value = FindConfigurationVariable (variable, provision_from_commit);
	if (!string.IsNullOrEmpty (value))
		return value;

	throw new Exception ($"Could not find {variable} in environment nor in the commit's ({commit}) manifest.");
}

// Provision mono
Console.WriteLine ($"Provisioning mono from {provision_from_commit}...");

var mono_version_file = "/Library/Frameworks/Mono.framework/Versions/Current/VERSION";
var min_mono_version = FindVariable("MIN_MONO_VERSION");
var max_mono_version = FindVariable("MAX_MONO_VERSION");
Console.WriteLine($"min_mono_version: {min_mono_version}");
Console.WriteLine($"max_mono_version: {max_mono_version}");

var current_mono_version = Exec("cat", mono_version_file)[0];
Console.WriteLine($"current_mono_version: {current_mono_version}");

// is_at_least_version $ACTUAL_MONO_VERSION $MIN_MONO_VERSION
var currentWithMin = IsAtLeastVersion(current_mono_version, min_mono_version);
var currentWithMax = IsAtLeastVersion(current_mono_version, max_mono_version);

Console.WriteLine($"IsAtLeastVersion: Current version '{current_mono_version}' compared with minimum version '{min_mono_version}': {currentWithMin}");
Console.WriteLine($"IsAtLeastVersion: Current version '{current_mono_version}' compared with maximum version '{max_mono_version}': {currentWithMax}");
