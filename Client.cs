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
    public virtual char Symbol { get; set; }
    public ConsoleColor Color { get; set; }
    public int X { get; set; }
    public int Y { get; set; }

    public BlockType Type { get; set; }


    public enum BlockType
    {
        None,
        Wall,
        Player
    }
}


class User : Block
{
    public string ID;


    public override char Symbol 
    { 
        get => base.Symbol; 
        set => base.Symbol = (value == ' ' ? '@' : value); 
    }
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
                    map.Add
                        (
                        new Block() 
                        {
                            X = j,
                            Y = i, Symbol = '#',
                            Color = ConsoleColor.Red,
                            Type = Block.BlockType.Wall
                        }
                        );
                }
                else
                {
                    map.Add
                        (
                        new Block()
                        { 
                            X = j,
                            Y = i,
                            Symbol = ' ',
                            Color = ConsoleColor.Black,
                            Type = Block.BlockType.None
                        }
                        );
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


    public static async Task Receive(TcpClient tcp_client, User user)
    {
        var stream = tcp_client.GetStream();

        string str = Newtonsoft.Json.JsonConvert.SerializeObject(user);
        str += '\0';

        byte[] bytes = Encoding.UTF8.GetBytes(str);

        await stream.WriteAsync(bytes);
    }


    public static async Task SendMessage(TcpClient tcp_client, User user)
    {
        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.W:

                    if (user.Y > 0) user.Y--;
                    //if (Map.map.Find(x => x.X == user.X && x.Y == user.Y).Type != Block.BlockType.None)
                    //{
                    //    user.Y++;
                    //}


                    break;

                case ConsoleKey.A:

                    if (user.X > 0) user.X--;
                    //if (Map.map.Find(x => x.X == user.X && x.Y == user.Y).Type != Block.BlockType.None)
                    //{
                    //    user.X++;
                    //}

                    break;

                case ConsoleKey.S:

                    user.Y++;
                    //if (Map.map.Find(x => x.X == user.X && x.Y == user.Y).Type != Block.BlockType.None)
                    //{
                    //    user.Y--;
                    //}

                    break;

                case ConsoleKey.D:

                    user.X++;
                    //if (Map.map.Find(x => x.X == user.X && x.Y == user.Y).Type != Block.BlockType.None)
                    //{
                    //    user.X--;
                    //}

                    break;
            }


            await Receive(tcp_client, user);
        }
    }


    public static async Task Main(string[] args)
    {
        await Console.Out.WriteAsync("you are: ");
        string symbol = Console.ReadLine();

        await Console.Out.WriteAsync("your color is: ");
        int num = Convert.ToInt32(Console.ReadLine());

        ConsoleColor color = (num > 0 && num < 16 ? (ConsoleColor)num : ConsoleColor.Green);



        Map.Fill(20, 20);
        Map.Output();

        User user = new User()
        {
            X = 10,
            Y = 10,
            Color = color,
            Symbol = symbol[0],
            ID = Guid.NewGuid().ToString(),
            Type = Block.BlockType.Player
        };

        Users.Add(user);


        TcpClient tcp_client = new TcpClient();

        await tcp_client.ConnectAsync(IPAddress.Parse("26.86.16.106"), 9010);
        await Receive(tcp_client, user);
        await Console.Out.WriteAsync("\nConnected\n");


        _ = Task.Run(async () => await GetMessage(tcp_client));

        await Task.Run(async () => await SendMessage(tcp_client, Users.First(x => x.ID == user.ID)));
    }
}
