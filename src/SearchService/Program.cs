using System.Net;
using MassTransit;
using Polly;
using Polly.Extensions.Http;
using SearchService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());//AppDomain.CurrentDomain.GetAssemblies() => location of profile inherited class
builder.Services.AddHttpClient<AuctionSvcHttpClient>().AddPolicyHandler(GetPolicy());

builder.Services.AddMassTransit(x =>
{
    //where to find the consumers
    x.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false));
    x.UsingRabbitMq((context, config) =>
    {
        config.ConfigureEndpoints(context);
    });
});
var app = builder.Build();

app.UseAuthorization();

app.MapControllers();
app.Lifetime.ApplicationStarted.Register(async () =>
{
    try
    {
        await DbInitializer.InitDb(app);
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
});


app.Run();

// retry indefinitely if certain conditions are met
static IAsyncPolicy<HttpResponseMessage> GetPolicy()
    => HttpPolicyExtensions.HandleTransientHttpError()
    .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
    .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(3));