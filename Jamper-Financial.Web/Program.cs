using Jamper_Financial.Shared.Services;
using Jamper_Financial.Web.Components; // Assuming App component is here
using Jamper_Financial.Web.Services; // Assuming AuthEndpoints is here
using Microsoft.Extensions.FileProviders;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Jamper_Financial.Shared.Data; // For Repositories, DatabaseHelperFactory, etc.
using Microsoft.AspNetCore.Components.Authorization;


// Additional usings
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using Jamper_Financial.Shared.Utilities; // For TransactionHelper, TransactionParser?
using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http; // For PathString
using System.IO; // For Path, DirectoryInfo, FileNotFoundException

var cultureInfo = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

var builder = WebApplication.CreateBuilder(args);

// Configure Data Protection to store keys in a persistent location within the container
// IMPORTANT: You MUST map a volume to this path in your docker-compose.yml or docker run command
// for keys to persist across container restarts. E.g., volumes: - ./dpkeys:/app/DataProtection-Keys
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/DataProtection-Keys")) // Standard container path
    .SetApplicationName("JamperFinancialApp");

// Configure QuestPDF license
QuestPDF.Settings.License = LicenseType.Community;

// --- Database Path Configuration ---

// --- FOR DOCKER ---
// The Dockerfile copies AppDatabase.db to /AppDatabase.db in the runtime image
var dbPath = "/AppDatabase.db"; 
// --- END FOR DOCKER ---

/* --- FOR LOCAL RUN ---
var dbPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "AppDatabase.db");
// --- END FOR LOCAL RUN --- */

var connectionString = $"Data Source={dbPath}";
Console.WriteLine($"---> [INFO] Using database path: {dbPath}"); // Log the path being used


// Add states
builder.Services.AddScoped<GoalState>();
builder.Services.AddScoped<UserStateService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
// Standard Blazor auth services are added
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<LoginStateService>();

// Add services
builder.Services.AddScoped<SearchService>();
builder.Services.AddSingleton<DatabaseHelperFactory>(); // Assuming this uses the connectionString correctly
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddBlazorBootstrap();
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddSingleton<TransactionParser>();
builder.Services.AddScoped<UpcomingTransactionService>();

// Configure HttpClient - BaseAddress might need review depending on how client calls server in Docker setup
builder.Services.AddScoped(sp => {
    // Get NavigationManager to determine the base URI if needed, but for self-calls in Docker, use the internal listening URL.
    // var navigationManager = sp.GetRequiredService<NavigationManager>(); // Might not be available here depending on registration timing

    // --- FOR DOCKER ---
    // Use the internal HTTP URL Kestrel should be listening on (e.g., port 8080)
    // Ensure ASPNETCORE_URLS=http://+:8080 is set in your Docker environment.
    var baseAddress = "http://localhost:8080/"; 
    Console.WriteLine($"---> [INFO] Configuring HttpClient with BaseAddress (for Docker): {baseAddress}");
    // --- END FOR DOCKER ---

    /* --- FOR LOCAL RUN ---
    // Use the HTTPS address used during local development
    var baseAddress = builder.Configuration["BaseAddress"] ?? "https://localhost:7062/"; 
    Console.WriteLine($"---> [INFO] Configuring HttpClient with BaseAddress (for Local): {baseAddress}");
    // --- END FOR LOCAL RUN --- */

    return new HttpClient { 
        BaseAddress = new Uri(baseAddress) 
    };
});

builder.Services.AddScoped<IUserRepository, UserRepository>(); 
builder.Services.AddScoped<ISessionRepository, SessionRepository>(); 
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>(); 
builder.Services.AddScoped<IUserService, UserService>(sp => new UserService(connectionString));
builder.Services.AddScoped<IAccountService, AccountService>(sp => new AccountService(connectionString));
builder.Services.AddScoped<IBudgetInsightsService, BudgetInsightsService>(sp => new BudgetInsightsService(connectionString));
builder.Services.AddScoped<IExpenseService, ExpenseService>(sp => new ExpenseService(connectionString)); 

builder.Services.AddScoped<FirebaseService>();
builder.Services.AddAntiforgery(); 

// Initialize DB (ensure connection string is used correctly here if needed)
DatabaseHelper.InitializeDatabase(); // Might need adjustment if it relies on static connection string


// --- Firebase Configuration ---

// --- FOR DOCKER (using Environment Variable) ---
var firebaseCredentialsPath = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_PATH");
if (string.IsNullOrEmpty(firebaseCredentialsPath))
{
    firebaseCredentialsPath = "/app/wwwroot/credentials/jamper-finance-firebase-adminsdk-dsr42-13bb4f4464.json"; // Default path inside container
    Console.WriteLine($"---> [WARN] FIREBASE_CREDENTIALS_PATH env var not set. Falling back to: {firebaseCredentialsPath}");
} else {
     Console.WriteLine($"---> [INFO] Using Firebase credentials from env var: {firebaseCredentialsPath}");
}
// --- END FOR DOCKER ---

/* --- FOR LOCAL RUN ---
var firebaseCredentialsPath = "../Jamper-Financial.Shared/wwwroot/credentials/jamper-finance-firebase-adminsdk-dsr42-13bb4f4464.json";
Console.WriteLine($"---> [INFO] Using Firebase credentials from local path: {firebaseCredentialsPath}");
// --- END FOR LOCAL RUN --- */


if (File.Exists(firebaseCredentialsPath))
{
    if (FirebaseApp.DefaultInstance == null) 
    {
         FirebaseApp.Create(new AppOptions
         {
             Credential = GoogleCredential.FromFile(firebaseCredentialsPath)
         });
         Console.WriteLine("---> [INFO] FirebaseApp initialized.");
    } else {
         Console.WriteLine("---> [INFO] FirebaseApp already initialized.");
    }
}
else
{
    Console.WriteLine($"---> [ERROR] Firebase credentials file not found at {firebaseCredentialsPath}");
    throw new FileNotFoundException("Firebase credentials file not found.", firebaseCredentialsPath);
}

// --- Standard Authentication/Authorization (Currently Commented Out) ---
// Keep commented out if relying solely on custom middleware
// builder.Services.AddAuthentication(options => { ... });
// builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts(); 
}

app.UseHttpsRedirection(); 

// --- Static Files Configuration ---

// Serve files from the main web project (e.g., bundled CSS)
app.UseStaticFiles(); 

// --- FOR DOCKER ---
// Serve files copied from the Shared project's wwwroot to /app/wwwroot
Console.WriteLine("---> [INFO] Configuring static files from /app/wwwroot");
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider("/app/wwwroot"), // Path inside the container
    RequestPath = new PathString("") 
});
// --- END FOR DOCKER ---

/* --- FOR LOCAL RUN ---
// Serve files directly from the Shared project's wwwroot
Console.WriteLine("---> [INFO] Configuring static files from Shared project wwwroot");
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), @"../Jamper-Financial.Shared/wwwroot")),
    RequestPath = new PathString("") 
});
// --- END FOR LOCAL RUN --- */


// --- Middleware Order ---
app.UseRouting(); // Routing first

// Standard Auth (Commented Out)
// app.UseAuthentication(); 
// app.UseAuthorization(); 

app.UseAntiforgery(); // Keep Antiforgery

// Custom Session Middleware
app.UseMiddleware<SessionValidationMiddleware>(); 

// --- Endpoint Mapping ---
app.MapAuthApi();

// --------------------------------------------------
// CSV Endpoint
// --------------------------------------------------
app.MapGet("/export/csv", async (HttpContext context) =>
{
    var reportType = context.Request.Query["reportType"];
    var reportName = context.Request.Query["reportName"];
    var description = context.Request.Query["description"];
    var fromDateStr = context.Request.Query["fromDate"];
    var toDateStr = context.Request.Query["toDate"];
    var categoriesStr = context.Request.Query["categories"];
    var accountsStr = context.Request.Query["accounts"];
    var userIdStr = context.Request.Query["userId"];

    DateTime.TryParse(fromDateStr, out DateTime fromDate);
    DateTime.TryParse(toDateStr, out DateTime toDate);

    var catList = (categoriesStr.ToString() ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(c => c.Trim())
        .ToList();

    int userId = 1;
    if (!string.IsNullOrEmpty(userIdStr))
        int.TryParse(userIdStr, out userId);

    var allTransactions = await TransactionHelper.GetTransactionsAsync(userId);

    // Filter by date range
    var filtered = allTransactions
        .Where(t => t.Date >= fromDate && t.Date <= toDate)
        .ToList();

    // Filter by categories if needed
    if (!catList.Contains("All"))
        filtered = filtered.Where(t => catList.Contains(t.CategoryID.ToString())).ToList();

    // Filter by report type
    if (reportType == "monthlyExpenses")
        filtered = filtered.Where(t => t.TransactionType == "e").ToList();
    else if (reportType == "monthlySavings")
        filtered = filtered.Where(t => t.TransactionType == "i").ToList();

    // Summaries
    var culture = CultureInfo.CreateSpecificCulture("en-US");
    var totalDebit = filtered.Where(t => t.TransactionType == "e").Sum(t => t.Amount);
    var totalCredit = filtered.Where(t => t.TransactionType == "i").Sum(t => t.Amount);
    var netTotal = totalCredit - totalDebit;

    // Build CSV
    var csv = new StringBuilder();

    // Basic report info, each line is fully quoted if it might contain commas
    csv.AppendLine("\"Jamper Financial Report\"");
    csv.AppendLine($"\"Report Name: {reportName}\"");
    csv.AppendLine($"\"Report from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}\"");
    csv.AppendLine($"\"Description: {description}\"");

    // Wrap these lines in quotes so that commas in the amounts do not break columns
    csv.AppendLine($"\"Total Debit: {totalDebit.ToString("C", culture)}\"");
    csv.AppendLine($"\"Total Credit: {totalCredit.ToString("C", culture)}\"");
    csv.AppendLine($"\"Net Total: {netTotal.ToString("C", culture)}\"");

    csv.AppendLine();  // blank line

    // Table header
    csv.AppendLine("Description,Date,Debit,Credit,Frequency,Next Due");

    // Table body rows
    foreach (var t in filtered)
    {
        string freq = string.IsNullOrEmpty(t.Frequency) ? "-" : t.Frequency;
        string nextDue = t.Frequency?.ToLower() switch
        {
            "monthly" => t.Date.AddMonths(1).ToString("yyyy-MM-dd"),
            "yearly" => t.Date.AddYears(1).ToString("yyyy-MM-dd"),
            _ => "-"
        };

        // Debit if expense, Credit if income
        string debit = t.TransactionType == "e" ? t.Amount.ToString("C", culture) : "";
        string credit = t.TransactionType == "i" ? t.Amount.ToString("C", culture) : "";

        // Wrap each field in quotes in case it contains commas
        csv.AppendLine(
            $"\"{t.Description}\"," +
            $"\"{t.Date:yyyy-MM-dd}\"," +
            $"\"{debit}\"," +
            $"\"{credit}\"," +
            $"\"{freq}\"," +
            $"\"{nextDue}\""
        );
    }

    var csvBytes = Encoding.UTF8.GetBytes(csv.ToString());
    var fileName = $"Report_{DateTime.Now:yyyyMMddHHmmss}.csv";
    return Results.File(csvBytes, "text/csv", fileName);
});

// --------------------------------------------------
// PDF Endpoint
// --------------------------------------------------

// Table header style
static IContainer CellStyleHeader(IContainer container)
{
    return container
        .Background("#62AD41")
        .PaddingVertical(10)    // Slightly more padding
        .Border(0)
        .DefaultTextStyle(x => x.SemiBold()
                               .FontColor(Colors.White)
                               .FontSize(11));
}

// alternate rows
Func<IContainer, IContainer> EvenRowStyle = container =>
    container
        .Background("DEF9C4")  // Very light green tint
        .PaddingVertical(8)
        .Border(0)
        .DefaultTextStyle(x => x.FontSize(10)
                               .FontColor("#333333"));  // Darker text

//  main rows
Func<IContainer, IContainer> OddRowStyle = container =>
    container
        .Background(Colors.White)
        .PaddingVertical(8)
        .Border(0)
        .DefaultTextStyle(x => x.FontSize(10)
                               .FontColor("#333333"));

app.MapGet("/export/pdf", async (HttpContext context) =>
{
    try
    {
        var reportType = context.Request.Query["reportType"].ToString();
        var reportName = Uri.UnescapeDataString(context.Request.Query["reportName"]);
        var description = Uri.UnescapeDataString(context.Request.Query["description"]);
        var fromDateStr = context.Request.Query["fromDate"].ToString();
        var toDateStr = context.Request.Query["toDate"].ToString();
        var categoriesStr = context.Request.Query["categories"].ToString();
        var accountsStr = context.Request.Query["accounts"].ToString();
        var userIdStr = context.Request.Query["userId"].ToString();

        // Parse dates
        DateTime.TryParse(fromDateStr, out DateTime fromDate);
        DateTime.TryParse(toDateStr, out DateTime toDate);

        // Parse categories/accounts
        var catList = (categoriesStr ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();

        var acctList = (accountsStr ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();

        // Parse user ID
        int userId = 1;
        if (!string.IsNullOrEmpty(userIdStr))
            int.TryParse(userIdStr, out userId);

        // Load transactions
        var allTransactions = await TransactionHelper.GetTransactionsAsync(userId);

        // Filter by date range
        var filtered = allTransactions
            .Where(t => t.Date >= fromDate && t.Date <= toDate)
            .ToList();

        // Filter by categories
        if (!catList.Contains("All"))
            filtered = filtered.Where(t => catList.Contains(t.CategoryID.ToString())).ToList();

        // Filter by accounts
        if (acctList.Count > 0 && !acctList.Contains("All"))
        {
            var accountIds = acctList.Select(int.Parse).ToList();
            filtered = filtered.Where(t => accountIds.Contains(t.AccountID)).ToList();
        }

        // monthlyExpenses => only expenses; monthlySavings => only income
        if (reportType == "monthlyExpenses")
            filtered = filtered.Where(t => t.TransactionType == "e").ToList();
        else if (reportType == "monthlySavings")
            filtered = filtered.Where(t => t.TransactionType == "i").ToList();

        // Sort descending by date
        filtered = filtered.OrderByDescending(t => t.Date).ToList();

        // Summaries
        decimal totalDebit = filtered.Where(t => t.TransactionType == "e").Sum(t => t.Amount);
        decimal totalCredit = filtered.Where(t => t.TransactionType == "i").Sum(t => t.Amount);
        decimal netTotal = totalCredit - totalDebit;

        // Build PDF
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);

                // HEADER
                page.Header().Column(headerCol =>
                {
                    headerCol.Item().AlignCenter().Text("Jamper Financial")
                        .FontSize(26).Bold().FontColor("#62AD41");
                    headerCol.Item().AlignCenter().Text(reportName)
                        .FontSize(16).SemiBold().FontColor(Colors.Black);
                    headerCol.Item().AlignCenter().Text($"Report from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}")
                        .FontSize(10).FontColor(Colors.Grey.Medium);
                });

                // CONTENT
                page.Content().Column(contentCol =>
                {
                    contentCol.Spacing(20);

                    // Summary Box
                    contentCol.Item().Background("DEF9C4")
                        .Padding(12)
                        .Row(row =>
                        {
                            // Left side
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Report Summary").FontSize(12).SemiBold();
                                col.Item().Text(description).FontSize(10).FontColor(Colors.Grey.Darken1);
                            });
                            // Right side
                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().Text($"Total Debit: {totalDebit:C}")
                                    .FontColor("#c62828").SemiBold();
                                col.Item().Text($"Total Credit: {totalCredit:C}")
                                    .FontColor("#2e7d32").SemiBold();
                                col.Item().Text($"Net Total: {netTotal:C}")
                                    .FontColor(netTotal >= 0 ? "#2e7d32" : "#c62828").SemiBold();
                                col.Item().Text($"Transactions: {filtered.Count}")
                                    .FontSize(10);
                            });
                        });

                    // If no data
                    if (filtered.Count == 0)
                    {
                        contentCol.Item().AlignCenter().Text("No transactions found for the selected filters.")
                            .FontSize(14).Italic().FontColor(Colors.Grey.Medium);
                    }
                    else
                    {
                        // 7 columns: No., Date, Description, Debit, Credit, Freq, Next Due
                        contentCol.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(35);  // No.
                                cols.ConstantColumn(90);  // Date
                                cols.RelativeColumn(2);   // Description
                                cols.ConstantColumn(60);  // Debit
                                cols.ConstantColumn(60);  // Credit
                                cols.ConstantColumn(60);  // Freq
                                cols.ConstantColumn(80);  // Next Due
                            });

                            // HEADER ROW
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyleHeader).AlignCenter().Text("No.");
                                header.Cell().Element(CellStyleHeader).AlignCenter().Text("Date");
                                header.Cell().Element(CellStyleHeader).Text("Description");
                                header.Cell().Element(CellStyleHeader).AlignRight().Text("Debit");
                                header.Cell().Element(CellStyleHeader).AlignRight().Text("Credit");
                                header.Cell().Element(CellStyleHeader).AlignCenter().Text("Freq");
                                header.Cell().Element(CellStyleHeader).AlignCenter().Text("Next Due");
                            });

                            // TABLE BODY
                            for (int i = 0; i < filtered.Count; i++)
                            {
                                var t = filtered[i];
                                // Alternate row styles
                                var rowStyle = (i % 2 == 0) ? EvenRowStyle : OddRowStyle;

                                // Column 1: No.
                                table.Cell().Element(rowStyle)
                                    .AlignCenter()
                                    .Text((i + 1).ToString());

                                // Column 2: Date
                                table.Cell().Element(rowStyle)
                                    .AlignCenter()
                                    .Text(t.Date.ToString("yyyy-MM-dd"));

                                // Column 3: Description
                                table.Cell().Element(rowStyle)
                                    .Text(t.Description);

                                // Column 4: Debit
                                // Column 5: Credit
                                if (t.TransactionType == "e")
                                {
                                    // Expense => put amount in Debit (red)
                                    table.Cell().Element(rowStyle)
                                        .AlignRight()
                                        .Text(t.Amount.ToString("C"))
                                        .FontColor("#c62828");
                                    // Credit => dash
                                    table.Cell().Element(rowStyle)
                                        .AlignRight()
                                        .Text("-");
                                }
                                else
                                {
                                    // Not expense => Debit => dash
                                    table.Cell().Element(rowStyle)
                                        .AlignRight()
                                        .Text("-");
                                    // Credit => green
                                    table.Cell().Element(rowStyle)
                                        .AlignRight()
                                        .Text(t.Amount.ToString("C"))
                                        .FontColor("#2e7d32");
                                }

                                // Column 6: Freq 
                                string freq = "-";
                                if (!string.IsNullOrEmpty(t.Frequency))
                                {
                                    var freqLower = t.Frequency.ToLower();
                                    if (freqLower == "monthly") freq = "Monthly";
                                    else if (freqLower == "yearly") freq = "Yearly";
                                    else freq = t.Frequency; // some other text
                                }
                                table.Cell().Element(rowStyle)
                                    .AlignCenter()
                                    .Text(freq);

                                // Column 7: Next Due
                                var nextDue = "-";
                                if (freq == "Monthly")
                                    nextDue = t.Date.AddMonths(1).ToString("yyyy-MM-dd");
                                else if (freq == "Yearly")
                                    nextDue = t.Date.AddYears(1).ToString("yyyy-MM-dd");

                                table.Cell().Element(rowStyle)
                                    .AlignCenter()
                                    .Text(nextDue);
                            }
                        });
                    }
                });

                // FOOTER
                page.Footer().PaddingVertical(10).Column(col =>
                {
                    col.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten1).PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("Confidential • Jamper Financial ©")
                            .FontSize(8).FontColor(Colors.Grey.Medium);
                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Page ").FontSize(10);
                            text.CurrentPageNumber();
                            text.Span(" of ").FontSize(10);
                            text.TotalPages();
                        });
                    });
                });
            });
        });

        var pdfBytes = doc.GeneratePdf();
        var fileName = $"{reportName.Replace(" ", "_")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
        return Results.File(pdfBytes, "application/pdf", fileName);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error generating PDF: {ex.Message}");
    }
});
// --------------------------------------------------


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Jamper_Financial.Shared._Imports).Assembly);

app.Run();
