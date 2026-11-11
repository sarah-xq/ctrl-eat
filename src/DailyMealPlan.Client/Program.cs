using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using DailyMealPlan.Client.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<DailyMealPlanApiClient>(client =>
{
    var apiUrl = builder.Configuration["DailyMealPlanApi:BaseUrl"]
        ?? "http://localhost:5104";
    client.BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/");
});
builder.Services.AddHttpContextAccessor();
if (builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Demo:EphemeralSessions"))
    builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/MealPlans");
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
})
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Authentication:Authority"]
        ?? "http://localhost:8082/realms/ctrl-eat";
    options.ClientId = builder.Configuration["Authentication:ClientId"]
        ?? "ctrleat-mealplans-client";
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.ResponseMode = OpenIdConnectResponseMode.Query;
    options.UsePkce = true;
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = false;
    options.CallbackPath = "/signin-oidc";
    options.SignedOutCallbackPath = "/signout-callback-oidc";
    options.SignedOutRedirectUri = "/";
    var localDemo = builder.Environment.IsDevelopment()
        && Uri.TryCreate(options.Authority, UriKind.Absolute, out var issuer) && issuer.IsLoopback;
    options.RequireHttpsMetadata = !localDemo;
    if (localDemo)
    {
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.NonceCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.NonceCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    }
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = options.Authority.TrimEnd('/'),
        ValidateAudience = true,
        ValidAudience = options.ClientId,
        NameClaimType = "preferred_username",
        RoleClaimType = ClaimTypes.Role
    };
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("daily-meal-plans.read");
    options.Scope.Add("daily-meal-plans.write");
    options.Scope.Add("recipes.read");
    options.Scope.Add("ingredients.read");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => Results.Content("<!doctype html><html lang=en><title>Ctrl+Eat</title><main><h1>Ctrl+Eat</h1><a href=/MealPlans>Open meal planner</a></main></html>", "text/html"));
app.MapRazorPages();
app.Run();

public partial class Program;