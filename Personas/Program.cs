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
builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
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

app.MapGet("/personas/listar", async (TodoDb db) =>
    await db.Todos.ToListAsync());

app.MapGet("/personas/buscar/{id}", async (int id, TodoDb db) =>
    await db.Todos.FindAsync(id)
    is Persona todo
       ? Results.Ok(todo)
       : Results.NotFound());

app.MapPost("/personas/agregar", async (Persona todo, TodoDb db) =>
{
    ParamCliente _pru = new ParamCliente();

    FluentValidation.Results.ValidationResult result = _pru.Validate(todo);
    if (result.IsValid)
    {
        db.Todos.Add(todo);
        await db.SaveChangesAsync();
        return Results.Created($"/personas/{todo.CedulaIdentidad}", todo);
    }
    else
    {
        return Results.BadRequest(new Error { TipoError = "Validaciones", Descripcion = result.Errors[0].ToString() });
    }

});

app.MapPut("/personas/modificar/{id}", async (int id, Persona inputTodo, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null) return Results.NotFound();

    todo.Nombre = inputTodo.Nombre;
    todo.Apellido = inputTodo.Apellido;
    todo.Email = inputTodo.Email;
    todo.Telefono = inputTodo.Telefono;
    todo.FechaNacimiento = inputTodo.FechaNacimiento;
    todo.CedulaIdentidad = inputTodo.CedulaIdentidad;

    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapDelete("/personas/eliminar/{id}", async (int id, TodoDb db) =>
{
    if (await db.Todos.FindAsync(id) is Persona todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return Results.Ok(todo);
    }
    return Results.NotFound();
});

app.Run();

public class Persona
{
    [Key]
    public int id { get; set; }
    public string? CedulaIdentidad { get; set; }
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

class TodoDb : DbContext
{
    public TodoDb(DbContextOptions<TodoDb> options)
        : base(options) { }

    public DbSet<Persona> Todos => Set<Persona>();
}