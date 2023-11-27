using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Security.Cryptography;
using System.Text;

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
        using MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(id));
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
}
