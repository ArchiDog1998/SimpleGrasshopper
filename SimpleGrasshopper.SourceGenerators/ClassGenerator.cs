using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

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
}
