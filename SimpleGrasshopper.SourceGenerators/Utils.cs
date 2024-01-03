using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Text.RegularExpressions;

namespace SimpleGrasshopper.SourceGenerators;

internal static class Utils
{
    public static string GetFullMetadataName(this ISymbol s)
    {
        if (s == null || s is INamespaceSymbol)
        {
            return string.Empty;
        }

        while (s != null && s is not ITypeSymbol)
        {
            s = s.ContainingSymbol;
        }

        if (s == null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(s.MetadataName);

        s = s.ContainingSymbol;
        while (!IsRootNamespace(s))
        {
            sb.Insert(0, s.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) + '.');

            s = s.ContainingSymbol;
        }

        return sb.ToString();

        static bool IsRootNamespace(ISymbol symbol)
        {
            return symbol is INamespaceSymbol s && s.IsGlobalNamespace;
        }
    }

    public static bool IsObsolete(this MemberDeclarationSyntax syntax)
    {
        return syntax.AttributeLists.Any(list => list.Attributes.Any(attr => attr.Name.ToString() is "Obsolete" or "ObsoleteAttribute" or "System.Obsolete" or "System.ObsoleteAttribute"));
    }

    public static string GetGuid(params string[] ids)
    {
        var id = string.Join(".", ids);

        byte[] hash = HashAlgorithmMD5.Calculate(Encoding.UTF8.GetBytes(id));
        return new Guid(hash).ToString("B");
    }

    public static void DiagnosticWrongKeyword(this SourceProductionContext spc, Location loc, string message)
    {
        spc.DiagnosticWarning(loc, "SG0001", "Wrong Keyword", message);
    }

    public static void DiagnosticTooManyInstances(this SourceProductionContext spc, Location loc, string message)
    {
        spc.DiagnosticWarning(loc, "SG0002", "Too Many Instances", message);
    }

    public static void DiagnosticWrongType(this SourceProductionContext spc, Location loc, string message)
    {
        spc.DiagnosticWarning(loc, "SG0003", "Wrong Type", message);
    }

    public static void DiagnosticWrongName(this SourceProductionContext spc, Location loc, string message)
    {
        spc.DiagnosticWarning(loc, "SG0004", "Wrong Name", message);
    }

    public static void DiagnosticPropertyGetSet(this SourceProductionContext spc, Location loc, string message)
    {
        spc.DiagnosticWarning(loc, "SG0005", "Property Get Set", message);
    }

    public static void DiagnosticAttributeUsing(this SourceProductionContext spc, Location loc, string message)
    {
        spc.DiagnosticWarning(loc, "SG0006", "Attribute Using", message);
    }

    private static void DiagnosticWarning(this SourceProductionContext spc, Location loc, string id, string title, string message)
    {
        var desc = new DiagnosticDescriptor(id, title, message, "Problem",
        DiagnosticSeverity.Warning, true);

        spc.ReportDiagnostic(Diagnostic.Create(desc, loc));
    }

    public static string ToPascalCase(this string input)
    {
        return string.Join(".", input.Split('.').Select(ConvertToPascalCase));

        static string ConvertToPascalCase(string input)
        {
            Regex invalidCharsRgx = new(@"[^_a-zA-Z0-9]");
            Regex whiteSpace = new(@"(?<=\s)");
            Regex startsWithLowerCaseChar = new("^[a-z]");
            Regex firstCharFollowedByUpperCasesOnly = new("(?<=[A-Z])[A-Z0-9]+$");
            Regex lowerCaseNextToNumber = new("(?<=[0-9])[a-z]");
            Regex upperCaseInside = new("(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))");

            var pascalCase = invalidCharsRgx.Replace(whiteSpace.Replace(input, "_"), string.Empty)
                .Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => startsWithLowerCaseChar.Replace(w, m => m.Value.ToUpper()))
                .Select(w => firstCharFollowedByUpperCasesOnly.Replace(w, m => m.Value.ToLower()))
                .Select(w => lowerCaseNextToNumber.Replace(w, m => m.Value.ToUpper()))
                .Select(w => upperCaseInside.Replace(w, m => m.Value.ToLower()));

            return string.Concat(pascalCase);
        }
    }
}
