using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Api Persona",
        Description = "Administracion de datos personales",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "Example Contact",
            Url = new Uri("https://example.com/contact")
        },
        License = new OpenApiLicense
        {
            Name = "Example Licence",
            Url = new Uri("https://example.com/licence")

        }
    });
    //using system.reflection
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});
builder.Services.AddDbContext<PersonaDb>(opt => opt.UseInMemoryDatabase("UsuarioList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
var app = builder.Build();
//configure the http request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
};
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});
app.UseHttpsRedirection();

app.MapGet("/", () => Results.Redirect("swagger", true))
    .ExcludeFromDescription();

app.MapGet("/personas/listar", async (PersonaDb db) =>
    await db.Usuarios.ToListAsync());

app.MapGet("/personas/buscar/{id}", async (int id, PersonaDb db) =>
    await db.Usuarios.FindAsync(id)
    is Persona user
       ? Results.Ok(user)
       : Results.NotFound());

app.MapPost("/personas/agregar", async (Persona user, PersonaDb db) =>
{
    ParamCliente _pru = new ParamCliente();

    FluentValidation.Results.ValidationResult result = _pru.Validate(user);
    if (result.IsValid)
    {
        db.Usuarios.Add(user);
        await db.SaveChangesAsync();
        return Results.Created($"/personas/{user.CedulaIdentidad}", user);
    }
    else
    {
        return Results.BadRequest(new Error { TipoError = "Validaciones", Descripcion = result.Errors[0].ToString() });
    }

});

app.MapPut("/personas/modificar/{id}", async (int id, Persona inputUsuario, PersonaDb db) =>
{
    var user = await db.Usuarios.FindAsync(id);

    if (user is null) return Results.NotFound();

    user.Nombre = inputUsuario.Nombre;
    user.Apellido = inputUsuario.Apellido;
    user.Email = inputUsuario.Email;
    user.Telefono = inputUsuario.Telefono;
    user.FechaNacimiento = inputUsuario.FechaNacimiento;
    user.CedulaIdentidad = inputUsuario.CedulaIdentidad;

    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapDelete("/personas/eliminar/{id}", async (int id, PersonaDb db) =>
{
    if (await db.Usuarios.FindAsync(id) is Persona user)
    {
        db.Usuarios.Remove(user);
        await db.SaveChangesAsync();
        return Results.Ok(user);
    }
    return Results.NotFound();
});

app.Run();

public class Persona
{
    [Key]
    public int id { get; set; }
    public int CedulaIdentidad { get; set; }
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? FechaNacimiento { get; set; }

};

public class ParamCliente : AbstractValidator<Persona>
{
    public ParamCliente()
    {
        RuleFor(p => p.CedulaIdentidad)
            .NotNull()
            .NotEmpty().WithMessage("El campo CedulaIdentidad no debe estar vacio, favor verifique");
        RuleFor(p => p.Nombre)
            .NotNull()
            .NotEmpty().WithMessage("El campo NOmbre no debe estar vacio, favor verifique.");
    }
}

public class Error
{
    public string TipoError { get; set; }
    public string Descripcion { get; set; }

}

class PersonaDb : DbContext
{
    public PersonaDb(DbContextOptions<PersonaDb> options)
        : base(options) { }

    public DbSet<Persona> Usuarios => Set<Persona>();
}