using System.Net;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string host = Environment.GetEnvironmentVariable("DB_HOST") ?? "db";
string user = Environment.GetEnvironmentVariable("DB_USER") ?? "usuario";
string pass = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "clave123";
string db   = Environment.GetEnvironmentVariable("DB_NAME") ?? "holamundo";

string connStr = $"Server={host};Database={db};User ID={user};Password={pass}";

app.MapGet("/", async () =>
{
    MySqlConnection? conn = null;
    Exception? lastError = null;

    // Reintenta unos segundos por si MySQL aún está arrancando
    for (int i = 0; i < 10; i++)
    {
        try
        {
            conn = new MySqlConnection(connStr);
            await conn.OpenAsync();
            lastError = null;
            break;
        }
        catch (Exception ex)
        {
            lastError = ex;
            conn?.Dispose();
            conn = null;
            await Task.Delay(2000);
        }
    }

    if (conn == null)
    {
        var errorHtml = $"<h1>Error al conectar con MySQL</h1><p>{WebUtility.HtmlEncode(lastError?.Message ?? "Error desconocido")}</p>";
        return Results.Content(errorHtml, "text/html; charset=utf-8", statusCode: 500);
    }

    await using (conn)
    {
        // Crea la tabla y un registro de ejemplo si no existen
        await new MySqlCommand(
            @"CREATE TABLE IF NOT EXISTS mensajes (
                id INT AUTO_INCREMENT PRIMARY KEY,
                texto VARCHAR(100) NOT NULL
              )", conn).ExecuteNonQueryAsync();

        var total = Convert.ToInt32(
            await new MySqlCommand("SELECT COUNT(*) FROM mensajes", conn).ExecuteScalarAsync());

        if (total == 0)
        {
            await new MySqlCommand(
                "INSERT INTO mensajes (texto) VALUES ('¡Hola Mundo desde C# y MySQL!')", conn).ExecuteNonQueryAsync();
        }

        var texto = (string?)await new MySqlCommand(
            "SELECT texto FROM mensajes LIMIT 1", conn).ExecuteScalarAsync();

        var html = $@"<!DOCTYPE html>
<html lang=""es"">
<head>
  <meta charset=""UTF-8"">
  <title>Hola Mundo C# + MySQL</title>
</head>
<body>
  <h1>{WebUtility.HtmlEncode(texto ?? "")}</h1>
  <p>Conexión exitosa a la base de datos <strong>{WebUtility.HtmlEncode(db)}</strong> en el host <strong>{WebUtility.HtmlEncode(host)}</strong>.</p>
</body>
</html>";

        return Results.Content(html, "text/html; charset=utf-8");
    }
});

app.Run();