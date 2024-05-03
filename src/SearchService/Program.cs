using System.Net;
using Amazon.Runtime.Internal.Auth;
using MassTransit;
using MongoDB.Driver;
using MongoDB.Entities;
using Polly;
using Polly.Extensions.Http;
using SearchService;
using SearchService.Consumers;
using SearchService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Register all the available AutoMapper profiles defined within my project
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
// Register the http client to get the data from the Auction SVC 
builder.Services.AddHttpClient<AuctionSvcHttpClient>().AddPolicyHandler(GetPolicy());
// Register the RabbitMQ service
builder.Services.AddMassTransit(x =>
{
    // Scan the assembly containing the 'AuctionCreatedConsumer' class and register any consumer
    // classes found within the same namespace
    x.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();

    // Add the word search at the start of each Exchange
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false));

    // configures MassTransit to use RabbitMQ as the message transport
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ReceiveEndpoint("search-auction-created", e => {
            e.UseMessageRetry(r => r.Interval(5,5));

            e.ConfigureConsumer<AuctionCreatedConsumer>(context);
        });
        // configures MassTransit endpoints for RabbitMQ
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

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

static IAsyncPolicy<HttpResponseMessage> GetPolicy()
    => HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
        .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(3));
