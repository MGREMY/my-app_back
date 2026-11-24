using System.Globalization;
using System.Runtime.CompilerServices;

namespace MyApp.Tests;

/// <summary>
/// Runs every test project in en-US whatever the locale of the machine: expected messages and formatted values do not
/// depend on who runs the tests. Compiled into each <c>*.Tests</c> project by <c>Tests/Directory.Build.props</c> (a
/// module initializer only runs for the assembly that declares it).
/// </summary>
/// <remarks>
/// A test may still switch the culture for its own flow (<c>CultureInfo.CurrentCulture = ...</c>): the change does not
/// leak to other tests.
/// </remarks>
internal static class TestCulture
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

    [ModuleInitializer]
    internal static void Initialize()
    {
        CultureInfo.DefaultThreadCurrentCulture = Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Culture;
        CultureInfo.CurrentCulture = Culture;
        CultureInfo.CurrentUICulture = Culture;
    }
}
