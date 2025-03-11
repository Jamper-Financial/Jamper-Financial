using Jamper_Financial.Shared.Services;
using Jamper_Financial.Web.Components;
using Jamper_Financial.Web.Services;
using Microsoft.Extensions.FileProviders;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Builder.Extensions;
using Jamper_Financial.Shared.Data;

// Additional usings
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using Blazorise;

var builder = WebApplication.CreateBuilder(args);

// Configure QuestPDF license
QuestPDF.Settings.License = LicenseType.Community;

var dbPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "AppDatabase.db");
var connectionString = $"Data Source={dbPath}";

// Add states
builder.Services.AddSingleton<GoalState>();
builder.Services.AddSingleton<UserStateService>();

// Add services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddBlazorBootstrap();
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddSingleton<LoginStateService>();

builder.Services.AddScoped<IUserService, UserService>(sp => new UserService(connectionString));
builder.Services.AddScoped<IBudgetInsightsService, BudgetInsightsService>(sp => new BudgetInsightsService(connectionString));

builder.Services.AddSingleton<FirebaseService>();
DatabaseHelper.InitializeDatabase();

FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile("../Jamper-Financial.Shared/wwwroot/credentials/jamper-finance-firebase-adminsdk-dsr42-13bb4f4464.json")
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Make the Shared project's wwwroot available
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), @"../Jamper-Financial.Shared/wwwroot")),
    RequestPath = new PathString("")
});

app.UseAntiforgery();

// --------------------------------------------------
// CSV Endpoint
// --------------------------------------------------
app.MapGet("/export/csv", async (HttpContext context) =>
{
    var reportName    = context.Request.Query["reportName"];
    var description   = context.Request.Query["description"];
    var fromDateStr   = context.Request.Query["fromDate"];
    var toDateStr     = context.Request.Query["toDate"];
    var categoriesStr = context.Request.Query["categories"];

    DateTime fromDate = DateTime.Now.AddMonths(-6);
    DateTime toDate   = DateTime.Now;
    DateTime.TryParse(fromDateStr, out fromDate);
    DateTime.TryParse(toDateStr, out toDate);

    var catList = (categoriesStr.ToString() ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(c => c.Trim())
        .ToList();

    var allTransactions = await TransactionHelper.GetTransactionsAsync();

    // Filter
    var filtered = allTransactions
        .Where(t => catList.Contains("All") || catList.Contains(t.Category))
        .Where(t => t.Date >= fromDate && t.Date <= toDate)
        .ToList();

    // Calculate totals
    decimal totalDebit = filtered.Sum(t => t.Debit);
    decimal totalCredit = filtered.Sum(t => t.Credit);

    // Build CSV: 5 columns: Description, Category, Date, Debit, Credit
    var csv = new StringBuilder();
    csv.AppendLine("Jamper Financial Report");
    csv.AppendLine($"Report Name: {reportName}");
    csv.AppendLine($"Report from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");
    csv.AppendLine($"Description: {description}");
    csv.AppendLine();

    csv.AppendLine("Description,Category,Date,Debit,Credit");

    foreach (var t in filtered)
    {
        var debitStr = t.Debit != 0 ? t.Debit.ToString("C") : "";
        var creditStr = t.Credit != 0 ? t.Credit.ToString("C") : "";
        csv.AppendLine($"\"{t.Description}\",\"{t.Category}\",\"{t.Date:yyyy-MM-dd}\",\"{debitStr}\",\"{creditStr}\"");
    }

    // Totals row (3 empty columns, then total Debit, total Credit)
    csv.AppendLine($",,,\"{totalDebit:C}\",\"{totalCredit:C}\"");

    var csvBytes = Encoding.UTF8.GetBytes(csv.ToString());
    var fileName = $"Report_{DateTime.Now:yyyyMMddHHmmss}.csv";
    return Results.File(csvBytes, "text/csv", fileName);
});

// --------------------------------------------------
// PDF Endpoint
// --------------------------------------------------
app.MapGet("/export/pdf", async (HttpContext context) =>
{
    var reportName    = context.Request.Query["reportName"];
    var description   = context.Request.Query["description"];
    var fromDateStr   = context.Request.Query["fromDate"];
    var toDateStr     = context.Request.Query["toDate"];
    var categoriesStr = context.Request.Query["categories"];

    DateTime fromDate = DateTime.Now.AddMonths(-6);
    DateTime toDate   = DateTime.Now;
    DateTime.TryParse(fromDateStr, out fromDate);
    DateTime.TryParse(toDateStr, out toDate);

    var catList = (categoriesStr.ToString() ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(c => c.Trim())
        .ToList();

    var allTransactions = await TransactionHelper.GetTransactionsAsync();

    var filtered = allTransactions
        .Where(t => catList.Contains("All") || catList.Contains(t.Category))
        .Where(t => t.Date >= fromDate && t.Date <= toDate)
        .ToList();

    // Totals
    decimal totalDebit = filtered.Sum(t => t.Debit);
    decimal totalCredit = filtered.Sum(t => t.Credit);

    var doc = Document.Create(document =>
    {
        document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.PageColor(Colors.White);

            // Header
            page.Header().Column(headerCol =>
            {
                headerCol.Spacing(10);

                // Title
                headerCol.Item().AlignCenter().Text("Jamper Financial Report")
                    .FontSize(24)
                    .SemiBold()
                    .FontColor("#62AD41"); // brand color

                // Sub-title
                headerCol.Item().AlignLeft().Text($"Report Name: {reportName}")
                    .FontSize(16)
                    .Bold();
            });

            // Content
            page.Content().Column(contentCol =>
            {
                contentCol.Spacing(20);

                // Date range
                contentCol.Item().Text($"Report from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");

                // 5 columns: Description, Category, Date, Debit, Credit
                contentCol.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(); // Desc
                        cols.RelativeColumn(); // Cat
                        cols.RelativeColumn(); // Date
                        cols.RelativeColumn(); // Debit
                        cols.RelativeColumn(); // Credit
                    });

                    // Header row
                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyleHeader).Text("Description");
                        header.Cell().Element(CellStyleHeader).Text("Category");
                        header.Cell().Element(CellStyleHeader).Text("Date");
                        header.Cell().Element(CellStyleHeader).Text("Debit");
                        header.Cell().Element(CellStyleHeader).Text("Credit");
                    });

                    // Data rows
                    foreach (var t in filtered)
                    {
                        table.Cell().Element(CellStyleData).Text(t.Description);
                        table.Cell().Element(CellStyleData).Text(t.Category);
                        table.Cell().Element(CellStyleData).Text($"{t.Date:yyyy-MM-dd}");

                        // Debit
                        var debitStr = t.Debit != 0 ? t.Debit.ToString("C") : "";
                        table.Cell().Element(CellStyleData).Text(debitStr);

                        // Credit
                        var creditStr = t.Credit != 0 ? t.Credit.ToString("C") : "";
                        table.Cell().Element(CellStyleData).Text(creditStr);
                    }

                    // Totals row: 3 empty cells, then totalDebit, totalCredit
                    table.Cell().Element(CellStyleTotals).Text("");
                    table.Cell().Element(CellStyleTotals).Text("");
                    table.Cell().Element(CellStyleTotals).Text("Totals:");
                    table.Cell().Element(CellStyleTotals).Text(totalDebit.ToString("C"));
                    table.Cell().Element(CellStyleTotals).Text(totalCredit.ToString("C"));
                });

                // Description
                contentCol.Item().Text($"Description: {description}")
                    .FontSize(12)
                    .Italic();
            });

            // Footer
            page.Footer().AlignCenter().Text(footerTxt =>
            {
                footerTxt.Span("Page ").FontSize(10);
                footerTxt.CurrentPageNumber();
                footerTxt.Span(" of ").FontSize(10);
                footerTxt.TotalPages();
            });
        });
    });

    var pdfBytes = doc.GeneratePdf();
    var fileName = $"Report_{DateTime.Now:yyyyMMddHHmmss}.pdf";
    return Results.File(pdfBytes, "application/pdf", fileName);
});

// Table header styling
static IContainer CellStyleHeader(IContainer container)
{
    return container
        .Border(1).BorderColor(Colors.Grey.Lighten2)
        .Background(Colors.Grey.Lighten3)
        .Padding(5);
}

// Table data styling
static IContainer CellStyleData(IContainer container)
{
    return container
        .Border(1).BorderColor(Colors.Grey.Lighten2)
        .Padding(5);
}

// Totals row styling
static IContainer CellStyleTotals(IContainer container)
{
    return container
        .Border(1).BorderColor(Colors.Black)
        .Padding(5)
        .DefaultTextStyle(x => x.Bold());
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Jamper_Financial.Shared._Imports).Assembly);

app.Run();
