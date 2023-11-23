using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

namespace SimpleGrasshopper.SourceGenerators;

public abstract class ClassGenerator<T> : IIncrementalGenerator where T : SyntaxNode
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName("SimpleGrasshopper.Attributes.DocObjAttribute",
            static (node, _) => node is T,
            static (n, ct) => (T)n.TargetNode)
            .Where(m => m is not null);

        context.RegisterSourceOutput(provider.Collect(), Execute);
    }

    protected abstract void Execute(SourceProductionContext context, ImmutableArray<T> syntaxes);

    protected static TS? GetParent<TS>(SyntaxNode? node) where TS : SyntaxNode
    {
        if (node == null) return null;
        if (node is TS result) return result;
        return GetParent<TS>(node.Parent);
    }

    protected static string GetGuid(params string[] ids)
    {
        var id = string.Join(".", ids);
        using MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(id));
        return new Guid(hash).ToString("B");
    }

    protected static bool IsObsolete(MemberDeclarationSyntax syntax)
    {
        return syntax.AttributeLists.Any(list => list.Attributes.Any(attr => attr.Name.ToString() is "Obsolete" or "ObsoleteAttribute" or "System.Obsolete" or "System.ObsoleteAttribute"));
    }
}
