using ZboSql.Core.Infrastructure;
using ZboSql.Demo.Components;
using ZboSql.Demo.Services;
using ZboSql.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 注册 ZboSql 数据库客户端
builder.Services.AddSingleton(sp =>
{
    var config = DbConfig.Create()
        .SetConnectionString(builder.Configuration.GetConnectionString("DefaultConnection")!)
        .SetDbType(DbType.PostgreSql)
        .SetPrintSql(builder.Environment.IsDevelopment())  // 开发环境打印 SQL
        .SetAutoCloseConnection(true)
        .Build();

    return new ZboSqlClient(config);
});

// 注册服务
builder.Services.AddScoped<ProductService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
