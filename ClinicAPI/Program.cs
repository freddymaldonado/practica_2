using PatientManager;
using Serilog;
using System;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(builder.Configuration["Logging:File:Path"])
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var patientFilePath = builder.Configuration["PatientFilePath"] ?? throw new InvalidOperationException("Patient file path is not configured. Please specify the path in appsettings.json under the PatientFilePath key");
builder.Services.AddSingleton<PatientService>(serviceProvider => new PatientService(patientFilePath));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {
        var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (contextFeature != null)
        {
            var exception = contextFeature.Error;
            bool isDevelopment = app.Environment.IsDevelopment();
            string message = exception switch
            {
                NotFoundException _ => exception.Message,
                ValidationException _ => exception.Message,
                _ => "An internal server error occurred."
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = exception switch
            {
                NotFoundException _ => StatusCodes.Status404NotFound,
                ValidationException _ => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = message
            };

            Log.Error($"Unhandled exception: {exception.Message} | StackTrace: {exception.StackTrace}");

            await context.Response.WriteAsJsonAsync(response);
        }
    });
});

app.MapPost("/patients", (PatientService service, Patient patient) => {
    try {
    service.CreatePatient(patient);
    return Results.Ok(patient);
    } catch (ValidationException ex) {
        Log.Information(ex.Message);
        return Results.BadRequest(ex.Message);
    }catch (Exception ex) {
        Log.Error(ex, "Failed to create patient.");
        return Results.Problem(ex.Message);
    }
}).WithName("CreatePatient");

app.MapPut("/patients/{ci}", (PatientService service, string ci, PatientUpdateModel update) => {
    try {
        service.UpdatePatient(ci, update.Name, update.LastName);
        return Results.Ok();
    } catch (NotFoundException ex) {
        Log.Information(ex.Message);
        return Results.NotFound(ex.Message);
    } catch (ValidationException ex) {
        Log.Information(ex.Message);
        return Results.BadRequest(ex.Message);
    } catch (Exception ex) {
        Log.Error(ex, "Failed to update patient.");
        return Results.Problem(ex.Message);
    }
}).WithName("UpdatePatient");

app.MapDelete("/patients/{ci}", (PatientService service, string ci) => {
    try {
        service.DeletePatient(ci);
        return Results.Ok("Patient deleted successfully");
    } catch (NotFoundException ex) {
        Log.Information(ex.Message);
        return Results.NotFound(ex.Message);
    } catch (Exception ex) {
        Log.Error(ex, "Failed to delete patient.");
        return Results.Problem(ex.Message);
    }
}).WithName("DeletePatient");

app.MapGet("/patients", (PatientService service) => {
    try {
        var patients = service.GetAllPatients();
        return Results.Ok(patients);
    } catch (EmptyPatientListException ex) {
        Log.Error(ex, "There are not patients in the list.");
        return Results.Ok(ex.Message);
    }
    catch (Exception ex) {
        Log.Error(ex, "Failed to retrieve patients.");
        return Results.Problem(ex.Message);
    }
}).WithName("GetPatients");

app.MapGet("/patients/{ci}", (PatientService service, string ci) => {
    try {
        var patient = service.GetPatientByCI(ci);
        if (patient != null) {
            return Results.Ok(patient);
        } else {
            throw new NotFoundException("Patient not found");
        }
    } catch (NotFoundException ex) {
        Log.Information(ex.Message);
        return Results.NotFound(ex.Message);
    } catch (Exception ex) {
        Log.Error(ex, "Failed to retrieve patient by CI.");
        return Results.Problem(ex.Message);
    }
}).WithName("GetPatientByCI");

app.Run();