using TelemetryDevice.BuilderBlock;
using TelemetryDevice.Icd;
using TelemetryDevice.KafkaBlock;
using TelemetryDevice.ListenerBlock;
using TelemetryDevice.MongoBlock;
using TelemetryDevice.ParserBlock;
using TelemetryDevice.PipelineBlock;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

string icdPath = Path.Combine(builder.Environment.ContentRootPath, "Icd", "MissionMapTable.json");
IcdDocument icdDocument = IcdDocument.Load(File.ReadAllText(icdPath));

builder.Services.AddSingleton(icdDocument);
builder.Services.AddSingleton<NetworkCaptureService>();
builder.Services.AddSingleton<Parser>();
builder.Services.AddSingleton<Builder>();
builder.Services.AddSingleton<Listener>();
builder.Services.AddSingleton<PipelineService>();
builder.Services.AddSingleton<Kafka>();
builder.Services.AddSingleton<Mongo>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.Services.GetRequiredService<PipelineService>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
