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
    // PARSE QUERY PARAMETERS
    var reportType = context.Request.Query["reportType"];
    var reportName = context.Request.Query["reportName"];
    var description = context.Request.Query["description"];
    var fromDateStr = context.Request.Query["fromDate"];
    var toDateStr = context.Request.Query["toDate"];
    var categoriesStr = context.Request.Query["categories"];
    var userIdStr = context.Request.Query["userId"]; // CAPSLOCK: Read userId from query

    DateTime fromDate = DateTime.Now.AddMonths(-6);
    DateTime toDate = DateTime.Now;
    DateTime.TryParse(fromDateStr, out fromDate);
    DateTime.TryParse(toDateStr, out toDate);

    // PARSE CATEGORIES
    var catList = (categoriesStr.ToString() ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(c => c.Trim())
        .ToList();

    // CAPSLOCK: GET THE CURRENT USER ID
    int userId = 1;
    if (!string.IsNullOrEmpty(userIdStr))
    {
        int.TryParse(userIdStr, out userId);
    }

    var allTransactions = await TransactionHelper.GetTransactionsAsync(userId);

    // FILTER BY CATEGORIES (if not "All")
    var filtered = allTransactions;
    if (!catList.Contains("All"))
    {
        filtered = filtered.Where(t => catList.Contains(t.CategoryID.ToString())).ToList();
    }
    // FILTER BY DATE RANGE
    filtered = filtered.Where(t => t.Date >= fromDate && t.Date <= toDate).ToList();

    // CAPSLOCK: IF REPORT IS MONTHLY, FILTER BY TRANSACTION TYPE
    if (reportType == "monthlyExpenses")
    {
        filtered = filtered.Where(t => t.TransactionType == "e").ToList();
    }
    else if (reportType == "monthlySavings")
    {
        filtered = filtered.Where(t => t.TransactionType == "i").ToList();
    }

    var csv = new StringBuilder();
    csv.AppendLine("Jamper Financial Report");
    csv.AppendLine($"Report Name: {reportName}");
    csv.AppendLine($"Report from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");
    csv.AppendLine($"Description: {description}");
    csv.AppendLine();

    if (reportType == "monthlyExpenses" || reportType == "monthlySavings")
    {
        // BUILD 4-COLUMN CSV: Description, Category, Date, Amount
        csv.AppendLine("Description,Category,Date,Amount");
        foreach (var t in filtered)
        {
            csv.AppendLine($"\"{t.Description}\"," +
                           $"\"{t.CategoryID}\"," +
                           $"\"{t.Date:yyyy-MM-dd}\"," +
                           $"\"{t.Amount.ToString("C")}\"");
        }
    }
    else
    {
        // BUILD 7-COLUMN CSV: Description, Category, Date, Debit, Credit, Frequency, EndDate
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
    }

    var csvBytes = Encoding.UTF8.GetBytes(csv.ToString());
    var fileName = $"Report_{DateTime.Now:yyyyMMddHHmmss}.csv";
    return Results.File(csvBytes, "text/csv", fileName);
});

// --------------------------------------------------
// PDF Endpoint
// --------------------------------------------------
app.MapGet("/export/pdf", async (HttpContext context) =>
{
    var reportType = context.Request.Query["reportType"];
    var reportName = context.Request.Query["reportName"];
    var description = context.Request.Query["description"];
    var fromDateStr = context.Request.Query["fromDate"];
    var toDateStr = context.Request.Query["toDate"];
    var categoriesStr = context.Request.Query["categories"];
    var userIdStr = context.Request.Query["userId"]; // CAPSLOCK: Read userId from query

    DateTime fromDate = DateTime.Now.AddMonths(-6);
    DateTime toDate = DateTime.Now;
    DateTime.TryParse(fromDateStr, out fromDate);
    DateTime.TryParse(toDateStr, out toDate);

    var catList = (categoriesStr.ToString() ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(c => c.Trim())
        .ToList();

    int userId = 1;
    if (!string.IsNullOrEmpty(userIdStr))
    {
        int.TryParse(userIdStr, out userId);
    }

    var allTransactions = await TransactionHelper.GetTransactionsAsync(userId);

    var filtered = allTransactions;
    if (!catList.Contains("All"))
    {
        filtered = filtered.Where(t => catList.Contains(t.CategoryID.ToString())).ToList();
    }
    filtered = filtered.Where(t => t.Date >= fromDate && t.Date <= toDate).ToList();

    // CAPSLOCK: Filter by transaction type for monthly reports
    if (reportType == "monthlyExpenses")
    {
        filtered = filtered.Where(t => t.TransactionType == "e").ToList();
    }
    else if (reportType == "monthlySavings")
    {
        filtered = filtered.Where(t => t.TransactionType == "i").ToList();
    }

    // USING QuestPDF TO BUILD THE PDF DOCUMENT
    var doc = Document.Create(document =>
    {
        document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.PageColor(Colors.White);

            // HEADER
            page.Header().Column(headerCol =>
            {
                headerCol.Spacing(10);
                headerCol.Item().AlignCenter().Text("Jamper Financial Report")
                    .FontSize(24)
                    .SemiBold()
                    .FontColor("#62AD41");
                headerCol.Item().AlignLeft().Text($"Report Name: {reportName}")
                    .FontSize(16)
                    .Bold();
            });

            // CONTENT
            page.Content().Column(contentCol =>
            {
                contentCol.Spacing(20);
                contentCol.Item().Text($"Report from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");

                if (reportType == "monthlyExpenses" || reportType == "monthlySavings")
                {
                    // CAPSLOCK: 4-COLUMN TABLE: Description, Category, Date, Amount
                    contentCol.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                        });
                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyleHeader).Text("Description");
                            header.Cell().Element(CellStyleHeader).Text("Category");
                            header.Cell().Element(CellStyleHeader).Text("Date");
                            header.Cell().Element(CellStyleHeader).Text("Amount");
                        });
                        foreach (var t in filtered)
                        {
                            table.Cell().Element(CellStyleData).Text(t.Description);
                            table.Cell().Element(CellStyleData).Text(t.CategoryID.ToString());
                            table.Cell().Element(CellStyleData).Text($"{t.Date:yyyy-MM-dd}");
                            table.Cell().Element(CellStyleData).Text(t.Amount.ToString("C"));
                        }
                    });
                }
                else
                {
                    // CAPSLOCK: 7-COLUMN TABLE FOR CUSTOM REPORT
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
                            cols.RelativeColumn(); // End Date
                        });
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
                        foreach (var t in filtered)
                        {
                            var debitStr = (t.TransactionType == "e") ? t.Amount.ToString("C") : "";
                            var creditStr = (t.TransactionType == "i") ? t.Amount.ToString("C") : "";
                            var freq = string.IsNullOrEmpty(t.Frequency) ? "" : t.Frequency;
                            var endDate = t.EndDate.HasValue ? t.EndDate.Value.ToString("yyyy-MM-dd") : "";
                            table.Cell().Element(CellStyleData).Text(t.Description);
                            table.Cell().Element(CellStyleData).Text(t.CategoryID.ToString());
                            table.Cell().Element(CellStyleData).Text($"{t.Date:yyyy-MM-dd}");
                            table.Cell().Element(CellStyleData).Text(debitStr);
                            table.Cell().Element(CellStyleData).Text(creditStr);
                            table.Cell().Element(CellStyleData).Text(freq);
                            table.Cell().Element(CellStyleData).Text(endDate);
                        }
                    });
                }
                contentCol.Item().Text($"Description: {description}")
                    .FontSize(12)
                    .Italic();
            });

            // FOOTER
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

// CAPSLOCK: TABLE CELL STYLE FUNCTIONS
static IContainer CellStyleHeader(IContainer container)
{
    return container
        .Border(1)
        .BorderColor(Colors.Grey.Lighten2)
        .Background(Colors.Grey.Lighten3)
        .Padding(5)
        .DefaultTextStyle(x => x.SemiBold());
}

static IContainer CellStyleData(IContainer container)
{
    return container
        .Border(1)
        .BorderColor(Colors.Grey.Lighten2)
        .Padding(5);
}


// --------------------------------------------------


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Jamper_Financial.Shared._Imports).Assembly);

app.Run();
