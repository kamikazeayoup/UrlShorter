using Microsoft.EntityFrameworkCore;
using UrlShorter.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var ConnStr = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApiDbContext>(options => options.UseSqlite(ConnStr));

var app = builder.Build();



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/shorturl", async (UrlDto url, ApiDbContext db, HttpContext ctx) =>
{
    //validate the input url
    if(!Uri.TryCreate(url.Url , UriKind.Absolute , out var InputUrl))
        return Results.BadRequest("your url is invalid");
    
    //create short version of provided url 
    var random = new Random();
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890@abcdefghijklmnopqrstuvwxyz";
    var randomStr = new string(Enumerable.Repeat(chars, 8)
        .Select(x => x[random.Next(x.Length)]).ToArray());

    //mapping the short url with long url 
    var Surl = new UrlManagement
    {
        Url = url.Url,
        ShortUrl = randomStr
    };
    //save the mapping into db
    db.Urls.Add(Surl);
    db.SaveChangesAsync();

    //construct url 
    var result = $"{ctx.Request.Scheme}://{ctx.Request.Host}/{Surl.ShortUrl}";

    return Results.Ok(new UrlShrotResponseDto
    {
        Url = result
    });
});

app.MapFallback(async (ApiDbContext db, HttpContext ctx) =>
{
    var path = ctx.Request.Path.ToUriComponent().Trim('/');
    var urlMatch = await db.Urls.FirstOrDefaultAsync(x=>x.ShortUrl.Trim() == path.Trim());
    if (urlMatch == null)
        return Results.BadRequest("Invalid request");

    return Results.Redirect(urlMatch.Url);
});

app.Run();

class ApiDbContext : DbContext
{
    public virtual DbSet<UrlManagement> Urls { get; set; }
    public ApiDbContext(DbContextOptions<ApiDbContext>options) : base(options)
    { }
    
}