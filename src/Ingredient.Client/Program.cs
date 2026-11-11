using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages(o => o.Conventions.AuthorizePage("/Index"));
builder.Services.AddHttpClient("Api", client => client.BaseAddress = new Uri(
    builder.Configuration["Api:BaseUrl"] ?? throw new InvalidOperationException("Api:BaseUrl required")));
builder.Services.AddAuthentication(options => {
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
}).AddCookie().AddOpenIdConnect(options => {
    options.Authority = builder.Configuration["Authentication:Authority"];
    options.ClientId = builder.Configuration["Authentication:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:ClientSecret"];
    options.ResponseType = "code";
    options.UsePkce = true;
    options.SaveTokens = true;
    options.RequireHttpsMetadata = builder.Configuration.GetValue("Authentication:RequireHttpsMetadata", true);
    options.Scope.Clear();
    foreach (var scope in (builder.Configuration["Authentication:Scopes"] ?? "openid profile").Split(' ', StringSplitOptions.RemoveEmptyEntries))
        options.Scope.Add(scope);
});
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseAuthentication(); app.UseAuthorization();
app.MapGet("/login", () => Results.Challenge(new AuthenticationProperties { RedirectUri = "/" })).AllowAnonymous();
app.MapGet("/logout", async (HttpContext context) => {
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/login" });
});
app.MapRazorPages(); app.Run();
