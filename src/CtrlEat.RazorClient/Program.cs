using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using CtrlEat.RazorClient.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/StorageLocations");
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("StorageLocationApi", client =>
{
    var baseUrl = builder.Configuration["StorageLocationApi:BaseUrl"]
        ?? throw new InvalidOperationException("StorageLocationApi:BaseUrl is required.");
    if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var apiUri)
        || apiUri.Scheme is not ("http" or "https")
        || (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing") && apiUri.Scheme != Uri.UriSchemeHttps))
    {
        throw new InvalidOperationException("StorageLocationApi:BaseUrl must be an HTTPS URL outside Development and Testing.");
    }
    client.BaseAddress = apiUri;
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddScoped<StorageLocationApiClient>();

var authority = builder.Configuration["Authentication:Authority"]
    ?? throw new InvalidOperationException("Authentication:Authority is required.");
var clientId = builder.Configuration["Authentication:ClientId"]
    ?? throw new InvalidOperationException("Authentication:ClientId is required.");
if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing")
    && (!Uri.TryCreate(authority, UriKind.Absolute, out var authorityUri) || authorityUri.Scheme != Uri.UriSchemeHttps))
{
    throw new InvalidOperationException("Authentication:Authority must be an HTTPS URL outside Development and Testing.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options => options.AccessDeniedPath = "/AccessDenied")
.AddOpenIdConnect(options =>
{
    options.Authority = authority;
    options.ClientId = clientId;
    options.ClientSecret = builder.Configuration["Authentication:ClientSecret"];
    options.ResponseType = "code";
    options.UsePkce = true;
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = false;
    options.RequireHttpsMetadata = builder.Configuration.GetValue("Authentication:RequireHttpsMetadata", true);
    options.Scope.Clear();
    foreach (var scope in (builder.Configuration["Authentication:Scopes"]
        ?? "openid profile storage-locations.read storage-locations.write").Split(' ', StringSplitOptions.RemoveEmptyEntries))
    {
        options.Scope.Add(scope);
    }
    options.TokenValidationParameters.NameClaimType = "preferred_username";
});
builder.Services.AddAuthorization();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/login", () => Results.Challenge(new AuthenticationProperties { RedirectUri = "/StorageLocations" }))
    .AllowAnonymous();
app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/" });
});
app.MapRazorPages();
app.Run();

public partial class Program { }