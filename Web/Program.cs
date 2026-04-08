using FacturationApp.Web.Components;
using Microsoft.EntityFrameworkCore;
using FacturationApp.Data;
using FacturationApp.Services.Contracts;
using FacturationApp.Services.Implementations;
using FacturationApp.Web.Data;
using FacturationApp.Web.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IProduitService, ProduitService>();
builder.Services.AddScoped<IFactureService, FactureService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IParametreService, ParametreService>();
builder.Services.AddScoped<IInnovationService, InnovationService>();
builder.Services.AddScoped<IFacturePdfService, FacturePdfService>();

var app = builder.Build();

await AppDataSeeder.SeedAsync(app.Services);

QuestPDF.Settings.License = LicenseType.Community;

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapGet("/api/factures/{id:int}/pdf", async (int id, IFactureService factureService, IParametreService parametreService, IFacturePdfService pdfService, CancellationToken cancellationToken) =>
{
    var facture = await factureService.GetByIdAsync(id, cancellationToken);
    if (facture is null)
    {
        return Results.NotFound();
    }

    var parametre = await parametreService.GetCurrentAsync(cancellationToken);
    var pdf = pdfService.Generate(facture, parametre);
    return Results.File(pdf, "application/pdf", $"{facture.NumeroFacture}.pdf");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
