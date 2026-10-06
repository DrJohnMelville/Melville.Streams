using Melville.IntersectionTypes.CodeGen;
using System.Collections.Generic;

namespace Test.IntersectionTypes;


internal class MemberEqualityComparerTest
{

    private static MemberForwarder MF(string name, string? types) =>
        new("int", "", name, types, null!);

    private IEnumerable<(MemberForwarder, MemberForwarder, bool)> Cases() => 
        [
          new (MF("A", null), MF("A", null), true),
          new (MF("A", "int"), MF("A", null), true),
          new (MF("A", ""), MF("A", null), true),
          new (MF("A", null), MF("A", "int"), true),
          new (MF("A", null), MF("A", ""), true),
          new (MF("A", "int"), MF("A", "int"), true),
          new (MF("A", "int"), MF("A", "float"), false),
          new (MF("A", "int"), MF("A", "int\r\nint"), false),
          new (MF("A", null), MF("B", null), false)
        ];

    [Test]
    [InstanceMethodDataSource(nameof(Cases))]
    public void Test(MemberForwarder a, MemberForwarder b, bool match) =>
        MemberForwarder.Comparer.Equals(a, b).Should().Be(match);
}