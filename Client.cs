using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;


class Block
{
    public char Symbol { get; set; }
    public ConsoleColor Color { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}


class User: Block
{
    public int ID;
}

static class Map
{
    public static List<Block> map = new List<Block>();

    public static void Fill(int h, int l)
    {
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < l; j++)
            {
                if (i == 0 || i == h - 1 || j == 0 || j == l - 1)
                {
                    map.Add(new Block() { X = j, Y = i, Symbol = '#', Color = ConsoleColor.Red });
                }
                else
                {
                    map.Add(new Block() { X = j, Y = i, Symbol = ' ', Color = ConsoleColor.Black });
                }          
            }
        }
    }

    public static void Output()
    {
        for (int i = 0; i < map.Count; i++)
        {
            Console.SetCursorPosition(map[i].X, map[i].Y);

            Console.ForegroundColor = map[i].Color;

            Console.Write(map[i].Symbol);
        }
    }
}

class Client
{
    public static List<User> Users = new List<User>();


    public static void DeletePreviousPosition(User previous)
    {
        Console.SetCursorPosition(previous.X, previous.Y);
        Console.ForegroundColor = ConsoleColor.Black;
        Console.Write(" ");
    }

    public static void SetCurrentPosition(User current)
    {
        Console.SetCursorPosition(current.X, current.Y);
        Console.ForegroundColor = current.Color;
        Console.Write(current.Symbol);
    }

    public static async Task GetMessage(TcpClient client)
    {
        var stream = client.GetStream();
        List<byte> bytes = new List<byte>();

        while (true)
        {
            int bytes_read = 0;

            while ((bytes_read = stream.ReadByte()) != '\0')
            {
                bytes.Add((byte)bytes_read);
            }

            string str = Encoding.UTF8.GetString(bytes.ToArray());
            bool isContain = false;
            User user = Newtonsoft.Json.JsonConvert.DeserializeObject<User>(str);

            for (int i = 0; i < Users.Count; i++)
            {
                if (Users[i].ID == user.ID)
                {
                    DeletePreviousPosition(Users[i]);

                    Users[i] = user;

                    SetCurrentPosition(Users[i]);

                    isContain = true;
                }
            }

            if (!isContain)
            {
                Users.Add(user);

                SetCurrentPosition(user);
            }

            bytes.Clear();
        }
    }


    public static async Task SendMessage(TcpClient tcp_client, User user)
    {
        var stream = tcp_client.GetStream();

        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(true);

            switch(key.Key)
            {
                case ConsoleKey.W:

                    user.Y--;

                    break;

                case ConsoleKey.A:

                    user.X--;

                    break;

                case ConsoleKey.S:

                    user.Y++;

                    break;

                case ConsoleKey.D:

                    user.X++;

                    break;
            }


            string str = Newtonsoft.Json.JsonConvert.SerializeObject(user);
            str += '\0';

            byte[] bytes = Encoding.UTF8.GetBytes(str);

            await stream.WriteAsync(bytes);
        }
    }


    public static async Task Main(string[] args)
    { 
        Map.Fill(20, 20);
        Map.Output();

        int id = Convert.ToInt32(Console.ReadLine());
        User user = new User() { X = 10, Y = 10, Color = ConsoleColor.Green, Symbol = '@', ID = id };
        Users.Add(user);


        TcpClient tcp_client = new TcpClient();
        await tcp_client.ConnectAsync(IPAddress.Parse("127.0.0.1"), 12345);
        await Console.Out.WriteAsync("Connected\n");



        _ = Task.Run(async () => await GetMessage(tcp_client));

        await Task.Run(async () => await SendMessage(tcp_client, Users.First(x => x.ID == id)));
    }
}