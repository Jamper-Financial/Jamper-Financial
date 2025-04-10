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
using Microsoft.JSInterop;
using Jamper_Financial.Shared.Utilities;

var builder = WebApplication.CreateBuilder(args);

// Configure QuestPDF license
QuestPDF.Settings.License = LicenseType.Community;

var dbPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "AppDatabase.db");
var connectionString = $"Data Source={dbPath}";

// Add states
builder.Services.AddSingleton<GoalState>();
builder.Services.AddSingleton<UserStateService>();
builder.Services.AddSingleton<LoginStateService>();

// Add services
builder.Services.AddScoped<SearchService>();
builder.Services.AddSingleton<DatabaseHelperFactory>();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddBlazorBootstrap();
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddSingleton<AuthenticationService>();
builder.Services.AddSingleton<TransactionParser>();
builder.Services.AddScoped<UpcomingTransactionService>();


builder.Services.AddScoped<IUserService, UserService>(sp => new UserService(connectionString));
builder.Services.AddScoped<IAccountService, AccountService>(sp => new AccountService(connectionString));
builder.Services.AddScoped<IBudgetInsightsService, BudgetInsightsService>(sp => new BudgetInsightsService(connectionString));
builder.Services.AddScoped<IExpenseService, ExpenseService>(sp => new ExpenseService(connectionString)); // Register IExpenseService

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
    // Parse query parameters
    var reportName = context.Request.Query["reportName"];
    var description = context.Request.Query["description"];
    var fromDateStr = context.Request.Query["fromDate"];
    var toDateStr = context.Request.Query["toDate"];
    var categoriesStr = context.Request.Query["categories"];

    // Convert string to DateTime
    DateTime fromDate = DateTime.Now.AddMonths(-6);
    DateTime toDate = DateTime.Now;
    DateTime.TryParse(fromDateStr, out fromDate);
    DateTime.TryParse(toDateStr, out toDate);

    // Parse categories
    var catList = (categoriesStr.ToString() ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(c => c.Trim())
        .ToList();

    int userId = 1;
    var allTransactions = await TransactionHelper.GetTransactionsAsync(userId);

    var filtered = allTransactions
        .Where(t => catList.Contains("All") || catList.Contains(t.CategoryID.ToString()))
        .Where(t => t.Date >= fromDate && t.Date <= toDate)
        .ToList();

    // Calculate totals
    decimal totalDebit = filtered.Where(t => t.TransactionType == "e").Sum(t => t.Amount);
    decimal totalCredit = filtered.Where(t => t.TransactionType == "i").Sum(t => t.Amount);

    // Build CSV
    //    Now 7 columns: Description,Category,Date,Debit,Credit,Frequency,EndDate
    var csv = new StringBuilder();
    // Title & info lines
    csv.AppendLine("Jamper Financial Report");
    csv.AppendLine($"Report Name: {reportName}");
    csv.AppendLine($"Report from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");
    csv.AppendLine($"Description: {description}");
    csv.AppendLine();

    // CSV header
    csv.AppendLine("Description,Category,Date,Debit,Credit,Frequency,EndDate");

    foreach (var t in filtered)
    {
        var debitStr = (t.TransactionType == "e" && t.Amount != 0) ? t.Amount.ToString("C") : "";
        var creditStr = (t.TransactionType == "i" && t.Amount != 0) ? t.Amount.ToString("C") : "";
        var freq = string.IsNullOrEmpty(t.Frequency) ? "" : t.Frequency;
        var endDate = t.EndDate.HasValue ? t.EndDate.Value.ToString("yyyy-MM-dd") : "";

        csv.AppendLine($"\"{t.Description}\"," +
                       $"\"{t.CategoryID}\"," +
                       $"\"{t.Date:yyyy-MM-dd}\"," +
                       $"\"{debitStr}\"," +
                       $"\"{creditStr}\"," +
                       $"\"{freq}\"," +
                       $"\"{endDate}\"");
    }


    csv.AppendLine($",,,\"{totalDebit:C}\",\"{totalCredit:C}\",,");

    var csvBytes = Encoding.UTF8.GetBytes(csv.ToString());
    var fileName = $"Report_{DateTime.Now:yyyyMMddHHmmss}.csv";
    return Results.File(csvBytes, "text/csv", fileName);
});

// --------------------------------------------------
// PDF Endpoint
// --------------------------------------------------
app.MapGet("/export/pdf", async (HttpContext context) =>
{
    // 1) Parse query parameters
    var reportName = context.Request.Query["reportName"];
    var description = context.Request.Query["description"];
    var fromDateStr = context.Request.Query["fromDate"];
    var toDateStr = context.Request.Query["toDate"];
    var categoriesStr = context.Request.Query["categories"];

    // Convert string to DateTime
    DateTime fromDate = DateTime.Now.AddMonths(-6);
    DateTime toDate = DateTime.Now;
    DateTime.TryParse(fromDateStr, out fromDate);
    DateTime.TryParse(toDateStr, out toDate);

    // Parse categories
    var catList = (categoriesStr.ToString() ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(c => c.Trim())
        .ToList();

    int userId = 1;
    var allTransactions = await TransactionHelper.GetTransactionsAsync(userId);

    var filtered = allTransactions
        .Where(t => catList.Contains("All") || catList.Contains(t.CategoryID.ToString()))
        .Where(t => t.Date >= fromDate && t.Date <= toDate)
        .ToList();

    // Totals
    decimal totalDebit = filtered.Where(t => t.TransactionType == "e").Sum(t => t.Amount);
    decimal totalCredit = filtered.Where(t => t.TransactionType == "i").Sum(t => t.Amount);

    // Build PDF with 7 columns
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

                // Big green title
                headerCol.Item().AlignCenter().Text("Jamper Financial Report")
                    .FontSize(24)
                    .SemiBold()
                    .FontColor("#62AD41");

                // Sub-title: "Report Name: ____"
                headerCol.Item().AlignLeft().Text($"Report Name: {reportName}")
                    .FontSize(16)
                    .Bold();
            });

            // Content
            page.Content().Column(contentCol =>
            {
                contentCol.Spacing(20);

                // Show date range
                contentCol.Item().Text($"Report from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");

                // Table with 7 columns
                contentCol.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(); // Description
                        cols.RelativeColumn(); // Category
                        cols.RelativeColumn(); // Date
                        cols.RelativeColumn(); // Debit
                        cols.RelativeColumn(); // Credit
                        cols.RelativeColumn(); // Frequency
                        cols.RelativeColumn(); // EndDate
                    });

                    // Header row
                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyleHeader).Text("Description");
                        header.Cell().Element(CellStyleHeader).Text("Category");
                        header.Cell().Element(CellStyleHeader).Text("Date");
                        header.Cell().Element(CellStyleHeader).Text("Debit");
                        header.Cell().Element(CellStyleHeader).Text("Credit");
                        header.Cell().Element(CellStyleHeader).Text("Frequency");
                        header.Cell().Element(CellStyleHeader).Text("End Date");
                    });

                    // Rows
                    foreach (var t in filtered)
                    {
                        var debitStr = (t.TransactionType == "e" && t.Amount != 0) ? t.Amount.ToString("C") : "";
                        var creditStr = (t.TransactionType == "i" && t.Amount != 0) ? t.Amount.ToString("C") : "";
                        var freq = string.IsNullOrEmpty(t.Frequency) ? "" : t.Frequency;
                        var endDate = t.EndDate.HasValue ? t.EndDate.Value.ToString("yyyy-MM-dd") : "";

                        table.Cell().Element(CellStyleData).Text(t.Description);
                        table.Cell().Element(CellStyleData).Text(t.CategoryID);
                        table.Cell().Element(CellStyleData).Text($"{t.Date:yyyy-MM-dd}");
                        table.Cell().Element(CellStyleData).Text(debitStr);
                        table.Cell().Element(CellStyleData).Text(creditStr);
                        table.Cell().Element(CellStyleData).Text(freq);
                        table.Cell().Element(CellStyleData).Text(endDate);
                    }

                    // Totals row => 7 columns
                    table.Cell().Element(CellStyleTotals).Text("");
                    table.Cell().Element(CellStyleTotals).Text("");
                    table.Cell().Element(CellStyleTotals).Text("Totals:");
                    table.Cell().Element(CellStyleTotals).Text(totalDebit.ToString("C"));
                    table.Cell().Element(CellStyleTotals).Text(totalCredit.ToString("C"));
                    table.Cell().Element(CellStyleTotals).Text("");
                    table.Cell().Element(CellStyleTotals).Text("");
                });

                // Show the user-provided description under the table
                contentCol.Item().Text($"Description: {description}")
                    .FontSize(12)
                    .Italic();
            });

            // Footer with page numbers
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

// Table header style
static IContainer CellStyleHeader(IContainer container)
{
    return container
        .Border(1)
        .BorderColor(Colors.Grey.Lighten2)
        .Background(Colors.Grey.Lighten3)
        .Padding(5)
        .DefaultTextStyle(x => x.SemiBold());
}

// Table data style
static IContainer CellStyleData(IContainer container)
{
    return container
        .Border(1)
        .BorderColor(Colors.Grey.Lighten2)
        .Padding(5);
}

// Totals row style
static IContainer CellStyleTotals(IContainer container)
{
    return container
        .Border(1)
        .BorderColor(Colors.Black)
        .Padding(5)
        .DefaultTextStyle(x => x.Bold());
}

// --------------------------------------------------


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Jamper_Financial.Shared._Imports).Assembly);

app.Run();
