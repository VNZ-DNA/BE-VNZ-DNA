using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace VNZ.Service.Utils.SlugService;

public sealed class Service : IService
{
    private static readonly Dictionary<string, string> TechnologyAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Objective-C++", "objective-cpp" },
        { "Objective-C", "objective-c" },
        { "C++/CLI", "cpp-cli" },
        { "C++/WinRT", "cpp-winrt" },
        { "C++", "cpp" },
        { "CPP", "cpp" },
        { "C#", "csharp" },
        { "CSharp", "csharp" },
        { "F#", "fsharp" },
        { "FSharp", "fsharp" },
        { ".NET Framework", "dotnet-framework" },
        { ".NET MAUI", "dotnet-maui" },
        { ".NET Core", "dotnet-core" },
        { ".NET", "dotnet" },
        { "DotNet", "dotnet" },
        { "ASP.NET Web API", "aspnet-web-api" },
        { "ASP.NET Core", "aspnet-core" },
        { "ASP.NET MVC", "aspnet-mvc" },
        { "ASP.NET", "aspnet" },
        { "ADO.NET", "adonet" },
        { "VB.NET", "vbnet" },
        { "Visual Basic .NET", "vbnet" },
        { "TensorFlow.js", "tensorflowjs" },
        { "TensorFlowJS", "tensorflowjs" },
        { "Node.js", "nodejs" },
        { "NodeJS", "nodejs" },
        { "Node JS", "nodejs" },
        { "Next.js", "nextjs" },
        { "NextJS", "nextjs" },
        { "Nest.js", "nestjs" },
        { "NestJS", "nestjs" },
        { "Nuxt.js", "nuxt" },
        { "NuxtJS", "nuxt" },
        { "Nuxt", "nuxt" },
        { "React.js", "react" },
        { "ReactJS", "react" },
        { "React", "react" },
        { "Vue.js", "vue" },
        { "VueJS", "vue" },
        { "Vue", "vue" },
        { "AngularJS", "angularjs" },
        { "Angular.js", "angularjs" },
        { "Express.js", "express" },
        { "ExpressJS", "express" },
        { "Express", "express" },
        { "Ember.js", "ember" },
        { "EmberJS", "ember" },
        { "Ember", "ember" },
        { "Backbone.js", "backbone" },
        { "BackboneJS", "backbone" },
        { "Backbone", "backbone" },
        { "Three.js", "threejs" },
        { "ThreeJS", "threejs" },
        { "D3.js", "d3js" },
        { "D3JS", "d3js" },
        { "Chart.js", "chartjs" },
        { "ChartJS", "chartjs" },
        { "Socket.IO", "socketio" },
        { "SocketIO", "socketio" }
    };

    private static readonly Regex TechnologyAliasRegex = new(
        @"(?<![\p{L}\p{N}\p{M}_])(?:" +
        string.Join("|", TechnologyAliases.Keys
            .OrderByDescending(alias => alias.Length)
            .Select(alias => Regex.Escape(alias))) +
        @")(?![\p{L}\p{N}\p{M}_])",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex SeparatorRegex = new(
        @"[^a-z0-9]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    public string Normalize(string title)
    {
        ArgumentNullException.ThrowIfNull(title);

        // Quy đổi tên công nghệ trước khi loại bỏ dấu và ký tự đặc biệt.
        var technologyNormalizedTitle = TechnologyAliasRegex.Replace(
            title,
            match => TechnologyAliases[match.Value]);

        var lowercaseTitle = technologyNormalizedTitle.ToLowerInvariant().Replace('đ', 'd');
        var decomposedTitle = lowercaseTitle.Normalize(NormalizationForm.FormD);
        var titleWithoutDiacritics = new StringBuilder(decomposedTitle.Length);

        foreach (var character in decomposedTitle)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);

            if (category == UnicodeCategory.NonSpacingMark ||
                category == UnicodeCategory.SpacingCombiningMark ||
                category == UnicodeCategory.EnclosingMark)
            {
                continue;
            }

            titleWithoutDiacritics.Append(character);
        }

        var baseSlug = SeparatorRegex.Replace(titleWithoutDiacritics.ToString(), "-");

        // Slug rỗng và giới hạn độ dài/hậu tố được xử lý tại service của từng module.
        return baseSlug.Trim('-');
    }
}
