using System.Reflection;
using System.Text.RegularExpressions;

namespace ECommerce.ArchitectureTests;

/// <summary>
/// No-PAN regression guard : no raw card number may ever be defined as a persistent field.
/// Payment data must be stored as provider tokens only.
/// </summary>
public sealed class PaymentSecurityTests
{
    [Fact]
    public void No_Raw_Pan_Field_Is_Defined_In_Domain_Or_Infrastructure()
    {
        var assemblies = new[]
        {
            typeof(ECommerce.Domain.AssemblyMarker).Assembly,
            typeof(ECommerce.Infrastructure.DependencyInjection).Assembly
        };

        var matches = new List<string>();
        var pattern = new Regex(@"^(cardNumber|card_number|pan|panNumber|cardnum|ccnumber)$", RegexOptions.IgnoreCase);

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes().Where(IsCandidate))
            {
                var names = type
                    .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Select(property => property.Name)
                    .Concat(type
                        .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Select(field => field.Name));

                foreach (var name in names)
                {
                    if (pattern.IsMatch(name))
                    {
                        matches.Add($"{type.FullName}.{name}");
                    }
                }
            }
        }

        Assert.True(
            matches.Count == 0,
            "Raw PAN-like fields detected (tokenization only is required): " + string.Join(", ", matches));
    }

    private static bool IsCandidate(Type type) =>
        type.IsClass && !type.IsAbstract && !type.IsGenericTypeDefinition && !type.IsInterface;
}
