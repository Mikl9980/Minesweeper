using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

class Program
{
    private static HttpListener _listener;
    private static Dictionary<string, MinesweeperGame> _games = new Dictionary<string, MinesweeperGame>();

    static void Main(string[] args)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://localhost:8080/");
        _listener.Start();
        Console.WriteLine("Сервер запущен по адресу http://localhost:8080/");

        RunServerAsync().Wait();

        _listener.Close();
    }

    static async Task RunServerAsync()
    {
        while (_listener.IsListening)
        {
            var context = await _listener.GetContextAsync();
            var request = context.Request;
            var response = context.Response;

            try
            {
                if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/new")
                {
                    var game = await CreateNewGameAsync(request);
                    response.StatusCode = (int)HttpStatusCode.OK;
                    await WriteResponseAsync(response, game);
                }
                else if (request.HttpMethod == "POST" && request.Url.AbsolutePath.StartsWith("/move/"))
                {
                    var gameId = request.Url.Segments[2];
                    var move = await GetMoveAsync(request);
                    var game = _games[gameId];
                    game.MakeMove(move.Row, move.Col);
                    response.StatusCode = (int)HttpStatusCode.OK;
                    await WriteResponseAsync(response, game);
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                await WriteResponseAsync(response, new { error = ex.Message });
            }

            response.Close();
        }
    }

    static async Task<MinesweeperGame> CreateNewGameAsync(HttpListenerRequest request)
    {
        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
        {
            var json = await reader.ReadToEndAsync();
            var settings = JsonConvert.DeserializeObject<MinesweeperSettings>(json);
            var game = new MinesweeperGame(settings);
            _games[game.Id] = game;
            return game;
        }
    }

    static async Task<Move> GetMoveAsync(HttpListenerRequest request)
    {
        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
        {
            var json = await reader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<Move>(json);
        }
    }

    static async Task WriteResponseAsync(HttpListenerResponse response, object data)
    {
        var json = JsonConvert.SerializeObject(data);
        var buffer = Encoding.UTF8.GetBytes(json);

        response.ContentType = "application/json";
        response.ContentEncoding = Encoding.UTF8;
        response.ContentLength64 = buffer.Length;

        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
    }
}

public class MinesweeperSettings
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int MinesCount { get; set; }
}

public class Move
{
    public int Row { get; set; }
    public int Col { get; set; }
}

public class MinesweeperGame
{
    public string Id { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int MinesCount { get; private set; }
    public char[,] Field { get; private set; }
    public bool Completed { get; private set; }

    public MinesweeperGame(MinesweeperSettings settings)
    {
        if (settings.Width > 30 || settings.Height > 30 || settings.MinesCount > settings.Width * settings.Height - 1)
        {
            throw new ArgumentException("Недопустимые настройки игры");
        }

        Id = Guid.NewGuid().ToString();
        Width = settings.Width;
        Height = settings.Height;
        MinesCount = settings.MinesCount;
        Field = new char[Height, Width];
        Completed = false;

        // Инициализация поля пустыми пробелами
        for (int i = 0; i < Height; i++)
        {
            for (int j = 0; j < Width; j++)
            {
                Field[i, j] = ' ';
            }
        }

        // Размещение мин случайным образом
        var random = new Random();
        for (int i = 0; i < MinesCount; i++)
        {
            int row, col;
            do
            {
                row = random.Next(Height);
                col = random.Next(Width);
            } while (Field[row, col] == 'X');

            Field[row, col] = 'X';
        }
    }

    public void MakeMove(int row, int col)
    {
        if (Completed)
        {
            throw new InvalidOperationException("Игра уже завершена.");
        }

        if (row < 0 || row >= Height || col < 0 || col >= Width)
        {
            throw new ArgumentException("Недопустимые координаты хода.");
        }

        if (Field[row, col] == 'X')
        {
            Completed = true;
            throw new InvalidOperationException("Вы попали на мину!");
        }

        // Реализация логики открытия ячеек игрового поля
        // Это заглушка для реальной логики
        Field[row, col] = '0';
    }
}