using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HarmonyPatchChangeParser
{

    /// <summary>
    /// Parses C# files for Harmony Patch attributes, and returns the results.
    /// This is a more accurate version of the Harmony patch searches, compared
    /// to the other search which is purely a text match on Harmony.
    /// </summary>
    internal class HarmonyPatchCodeParser
    {

        /// <summary>
        /// Get the Harmony Patch info.
        /// </summary>
        /// <param name="modsBasePath">The root directory of the mods to search.</param>
        /// <returns></returns>
        public List<HarmonyPatchInfo> GetModsHarmonyPatches(string modsBasePath)
        {
            var results = new List<HarmonyPatchInfo>();

            if (string.IsNullOrWhiteSpace(modsBasePath) || !System.IO.Directory.Exists(modsBasePath))
                return results;

            foreach (string file in Directory.EnumerateFiles(modsBasePath, "*.cs", System.IO.SearchOption.AllDirectories))
            {
                string source;
                source = File.ReadAllText(file);


                // Quick filter before Roslyn parse
                if (source.IndexOf("HarmonyPatch", StringComparison.Ordinal) < 0)
                    continue;

                SyntaxTree tree;
                tree = CSharpSyntaxTree.ParseText(source);

                var root = tree.GetRoot();

                var attributes = root.DescendantNodes()
                                     .OfType<AttributeSyntax>()
                                     .Where(a =>
                                     {
                                         var simple = GetSimpleAttributeName(a);
                                         return simple.EndsWith("HarmonyPatch", StringComparison.Ordinal);
                                     });

                foreach (var attr in attributes)
                {
                    var positionalArgs = new List<string>();
                    var namedArgs = new Dictionary<string, string>(StringComparer.Ordinal);

                    string? targetType = null;
                    string? targetMember = null;

                    if (attr.ArgumentList == null) continue;

                    foreach (AttributeArgumentSyntax arg in attr.ArgumentList!.Arguments)
                    {
                        var expr = arg.Expression;

                        // Collect raw
                        var exprText = GetExpressionText(expr);
                        if (arg.NameEquals is { } nameEquals)
                        {
                            var key = nameEquals.Name.Identifier.Text;
                            namedArgs[key] = exprText;
                        }
                        else
                        {
                            positionalArgs.Add(exprText);
                        }

                        // Heuristic extraction
                        targetType ??= TryExtractTypeName(expr);
                        targetMember ??= TryExtractMemberName(expr);
                    }

                    results.Add(new HarmonyPatchInfo
                    {
                        FilePath = file,
                        AttributeName = GetSimpleAttributeName(attr),
                        AttributeText = attr.ToFullString().Trim(),
                        PositionalArguments = positionalArgs,
                        NamedArguments = namedArgs,
                        TargetType = targetType ?? "",
                        TargetMember = targetMember ?? "",
                    });
                }
            }

            return results;
        }


        private string GetExpressionText(ExpressionSyntax expr)
        {
            // Return a readable text form; unwrap string literals
            if (expr is LiteralExpressionSyntax les &&
                les.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return les.Token.ValueText;
            }

            return expr.ToString();
        }

        private string GetSimpleAttributeName(AttributeSyntax attribute)
        {
            return attribute.Name switch
            {
                QualifiedNameSyntax q => q.Right.Identifier.Text,
                AliasQualifiedNameSyntax aq => aq.Name.Identifier.Text,
                IdentifierNameSyntax id => id.Identifier.Text,
                _ => attribute.Name.ToString()
            };
        }

        private string? TryExtractMemberName(ExpressionSyntax expr)
        {
            // nameof(...)
            if (expr is InvocationExpressionSyntax inv &&
                inv.Expression is IdentifierNameSyntax id &&
                id.Identifier.Text == "nameof" &&
                inv.ArgumentList.Arguments.Count == 1)
            {
                var inner = inv.ArgumentList.Arguments[0].Expression;
                return inner switch
                {
                    MemberAccessExpressionSyntax ma => ma.Name.Identifier.Text,
                    IdentifierNameSyntax iid => iid.Identifier.Text,
                    _ => inner.ToString()
                };
            }

            // "MethodName"
            if (expr is LiteralExpressionSyntax les &&
                les.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return les.Token.ValueText;
            }

            // MethodType.Whatever etc. -> not a member name; ignore
            return null;
        }

        private string? TryExtractTypeName(ExpressionSyntax expr)
        {
            if (expr is TypeOfExpressionSyntax typeOfExpr)
            {
                return typeOfExpr.Type.ToString();
            }
            return null;
        }
    }
}