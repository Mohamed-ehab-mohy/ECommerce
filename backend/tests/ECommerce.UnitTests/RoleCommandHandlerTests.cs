using ECommerce.Domain.Identity;
using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Audit;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Handlers;
using ECommerce.UseCases.Identity.Queries;

namespace ECommerce.UnitTests;

public sealed class RoleCommandHandlerTests
{
    private readonly FakeRoleRepository _roles = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly FakeAuditEntryRepository _auditEntries = new();
    private readonly FakeAuditContextProvider _auditContext = new();

    private CreateRoleCommandHandler CreateHandler =>
        new(_roles, _unitOfWork, _timeProvider, new CreateRoleCommandValidator(),
            new AuditLogWriter(_auditEntries, _auditContext));

    private AssignRolePermissionsCommandHandler AssignPermissionsHandler =>
        new(_roles, _unitOfWork, _timeProvider, new AssignRolePermissionsCommandValidator(),
            new AuditLogWriter(_auditEntries, _auditContext));

    private ListRolesQueryHandler ListHandler => new(_roles);

    [Fact]
    public async Task CreateRole_Adds_Role_And_Audits()
    {
        var result = await CreateHandler.Handle(
            new CreateRoleCommand("Manager", "Line managers", [Permissions.AuditRead]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var role = Assert.Single(_roles.Roles);
        Assert.Equal("Manager", role.Name);
        Assert.Equal([Permissions.AuditRead], role.Permissions.Select(p => p.PermissionCode).ToList());
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Single(_auditEntries.Entries);
    }

    [Fact]
    public async Task CreateRole_With_Duplicate_Name_Returns_Conflict()
    {
        _roles.Roles.Add(Role.Create("Manager", null, DateTime.UtcNow));

        var result = await CreateHandler.Handle(
            new CreateRoleCommand("Manager", null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RoleErrors.NameAlreadyExists, result.Error);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateRole_With_Unknown_Permission_Returns_Validation_Failure()
    {
        var result = await CreateHandler.Handle(
            new CreateRoleCommand("Manager", null, ["not.a.real.permission"]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task AssignPermissions_Replaces_Role_Permissions_And_Audits()
    {
        var role = Role.Create("Manager", null, DateTime.UtcNow);
        role.AssignPermissions([Permissions.CustomersRead], DateTime.UtcNow);
        _roles.Roles.Add(role);

        var result = await AssignPermissionsHandler.Handle(
            new AssignRolePermissionsCommand(role.Id, [Permissions.AuditRead, Permissions.RolesRead]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            [Permissions.AuditRead, Permissions.RolesRead],
            role.Permissions.Select(p => p.PermissionCode).ToList());
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Single(_auditEntries.Entries);
    }

    [Fact]
    public async Task AssignPermissions_With_Unknown_Role_Returns_NotFound()
    {
        var result = await AssignPermissionsHandler.Handle(
            new AssignRolePermissionsCommand(Guid.NewGuid(), [Permissions.AuditRead]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RoleErrors.RoleNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task AssignPermissions_With_Unknown_Permission_Returns_Validation_Failure()
    {
        var role = Role.Create("Manager", null, DateTime.UtcNow);
        _roles.Roles.Add(role);

        var result = await AssignPermissionsHandler.Handle(
            new AssignRolePermissionsCommand(role.Id, ["not.a.real.permission"]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task ListRoles_Returns_All_Roles_Ordered_By_Name()
    {
        _roles.Roles.Add(Role.Create("Zebra", null, DateTime.UtcNow));
        _roles.Roles.Add(Role.Create("Alpha", null, DateTime.UtcNow));

        var result = await ListHandler.Handle(new ListRolesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("Alpha", result.Value[0].Name);
        Assert.Equal("Zebra", result.Value[1].Name);
    }
}
