using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using Xunit;

namespace Crm.Tests;

public class WikiAccessTests
{
    private static WikiSection Section(int id, string mode = WikiSection.AccessAll, int companyId = 1)
        => new() { Id = id, CompanyId = companyId, Name = "s", AccessMode = mode };

    private static WikiUserContext User(bool manager = false, bool companyEditor = false, int[]? editorOf = null, int[]? granted = null, int companyId = 1)
        => new()
        {
            UserId = 10,
            CompanyId = companyId,
            IsManager = manager,
            IsCompanyEditor = companyEditor,
            EditorSectionIds = (editorOf ?? Array.Empty<int>()).ToHashSet(),
            GrantedSectionIds = (granted ?? Array.Empty<int>()).ToHashSet(),
        };

    [Fact]
    public void Reader_reads_open_section_but_cannot_edit()
    {
        var ctx = User();
        Assert.True(WikiAccess.CanRead(ctx, Section(1)));
        Assert.False(WikiAccess.CanEdit(ctx, Section(1)));
        Assert.False(WikiAccess.CanManage(ctx));
    }

    [Fact]
    public void Restricted_section_is_hidden_unless_granted_or_section_editor()
    {
        var restricted = Section(5, WikiSection.AccessSelected);
        Assert.False(WikiAccess.CanRead(User(), restricted));
        Assert.True(WikiAccess.CanRead(User(granted: new[] { 5 }), restricted));
        Assert.True(WikiAccess.CanRead(User(editorOf: new[] { 5 }), restricted));
        Assert.True(WikiAccess.CanRead(User(manager: true), restricted));
    }

    [Fact]
    public void Company_editor_cannot_see_restricted_section_without_access()
    {
        var restricted = Section(5, WikiSection.AccessSelected);
        var ctx = User(companyEditor: true);
        Assert.False(WikiAccess.CanRead(ctx, restricted));
        Assert.False(WikiAccess.CanEdit(ctx, restricted));
        Assert.True(WikiAccess.CanEdit(ctx, Section(1)));
    }

    [Fact]
    public void Section_editor_edits_only_own_section()
    {
        var ctx = User(editorOf: new[] { 3 });
        Assert.True(WikiAccess.CanEdit(ctx, Section(3)));
        Assert.False(WikiAccess.CanEdit(ctx, Section(4)));
    }

    [Fact]
    public void Granted_reader_of_restricted_section_still_cannot_edit()
    {
        Assert.False(WikiAccess.CanEdit(User(granted: new[] { 5 }), Section(5, WikiSection.AccessSelected)));
    }

    [Fact]
    public void Other_company_is_never_accessible_even_for_manager()
    {
        var ctx = User(manager: true, companyEditor: true, companyId: 2);
        Assert.False(WikiAccess.CanRead(ctx, Section(1)));
        Assert.False(WikiAccess.CanEdit(ctx, Section(1)));
    }

    [Theory]
    [InlineData("Admin", true)]
    [InlineData("admin", true)]
    [InlineData("Employee", false)]
    [InlineData(null, false)]
    public void Manager_role(string? role, bool expected) => Assert.Equal(expected, WikiAccess.IsManagerRole(role));
}
