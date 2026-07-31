using ECommerce.Domain.Common;

namespace ECommerce.UnitTests;

public sealed class BaseEntityTests
{
    private sealed class TestEntity : BaseEntity<Guid>
    {
        public static TestEntity Create() => new();
    }

    [Fact]
    public void New_Entity_Has_Default_Audit_Values()
    {
        var entity = TestEntity.Create();

        Assert.Equal(Guid.Empty, entity.Id);
        Assert.False(entity.IsDeleted);
        Assert.InRange(entity.CreatedAt, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow);
        Assert.InRange(entity.UpdatedAt, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow);
    }
}
