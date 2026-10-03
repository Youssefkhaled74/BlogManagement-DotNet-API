namespace BlogManagement.Application.Security;

public static class Permissions
{
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    public const string RolesRead = "roles.read";
    public const string RolesManage = "roles.manage";
    public const string CategoriesRead = "categories.read";
    public const string CategoriesManage = "categories.manage";
    public const string BlogsRead = "blogs.read";
    public const string BlogsCreate = "blogs.create";
    public const string BlogsUpdate = "blogs.update";
    public const string BlogsDelete = "blogs.delete";
    public const string BlogsSubmit = "blogs.submit";
    public const string BlogsReview = "blogs.review";
    public const string BlogsPublish = "blogs.publish";
    public const string EmployeesRead = "employees.read";
    public const string EmployeesManage = "employees.manage";
    public const string AuditRead = "audit.read";

    public static readonly IReadOnlyCollection<string> All =
    [
        UsersRead, UsersManage, RolesRead, RolesManage,
        CategoriesRead, CategoriesManage,
        BlogsRead, BlogsCreate, BlogsUpdate, BlogsDelete, BlogsSubmit, BlogsReview, BlogsPublish,
        EmployeesRead, EmployeesManage, AuditRead
    ];
}

public static class SystemRoles
{
    public const string Admin = "Admin";
    public const string Editor = "Editor";
    public const string Author = "Author";
}
