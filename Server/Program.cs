using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MySqlConnector;

namespace CKGameServer
{
    internal class Program
    {
        // 플레이어 접속 정보
        static EndPoint player1EP = null;
        static EndPoint player2EP = null;

        // 각 플레이어 거리
        static float player1Distance = -1;
        static float player2Distance = -1;

        // 게임 종료 여부
        static bool player1Done = false;
        static bool player2Done = false;

        // DB 저장 중복 방지
        static bool resultSaved = false;

        static void Main(string[] args)
        {
            Thread serverThread = new Thread(ServerFunc);
            serverThread.IsBackground = true;
            serverThread.Start();

            Console.WriteLine("[Server] Game Server Working...");
            Console.ReadLine();
        }

        static void ServerFunc(object obj)
        {
            // UDP
            Socket srvSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            srvSocket.Bind(new IPEndPoint(IPAddress.Any, 12345));

            byte[] recvBytes = new byte[1024];

            while (true)
            {
                EndPoint clientEP = new IPEndPoint(IPAddress.Any, 0);

                // 데이터 수신
                int nRecv = srvSocket.ReceiveFrom(recvBytes, ref clientEP);

                string txt = Encoding.UTF8.GetString(recvBytes, 0, nRecv);

                Console.WriteLine("[Recv] " + txt);

                string[] parts = txt.Split('|');

                // 위치 동기화 처리
                if (parts.Length == 2)
                {
                    string player = parts[0];

                    if (player == "Player1")
                        player1EP = clientEP;

                    if (player == "Player2")
                        player2EP = clientEP;

                    byte[] sendBytes = Encoding.UTF8.GetBytes(txt);

                    if (player == "Player1" && player2EP != null)
                        srvSocket.SendTo(sendBytes, player2EP);

                    if (player == "Player2" && player1EP != null)
                        srvSocket.SendTo(sendBytes, player1EP);
                }

                // 게임 종료
                if (parts.Length == 3 && parts[2] == "END")
                {
                    string player = parts[0];
                    float distance = float.Parse(parts[1]);

                    if (player == "Player1")
                    {
                        player1Distance = distance;
                        player1Done = true;
                    }

                    if (player == "Player2")
                    {
                        player2Distance = distance;
                        player2Done = true;
                    }

                    // 승패 판정
                    if (player1Done && player2Done && !resultSaved)
                    {
                        string result1;
                        string result2;

                        if (player1Distance < player2Distance)
                        {
                            result1 = "Win";
                            result2 = "Lose";
                        }
                        else if (player2Distance < player1Distance)
                        {
                            result1 = "Lose";
                            result2 = "Win";
                        }
                        else
                        {
                            result1 = "Draw";
                            result2 = "Draw";
                        }

                        // DB 저장
                        SaveResult("Player1", player1Distance, result1);
                        SaveResult("Player2", player2Distance, result2);

                        // Top5 전송
                        string ranking = LoadTop5();

                        byte[] rankBytes = Encoding.UTF8.GetBytes(ranking);

                        if (player1EP != null)
                            srvSocket.SendTo(rankBytes, player1EP);

                        if (player2EP != null)
                            srvSocket.SendTo(rankBytes, player2EP);

                        resultSaved = true;
                    }
                }
            }
        }

        // 결과 저장 함수
        static void SaveResult(string player, float dist, string result)
        {
            string connStr =
                "Server=127.0.0.1;Port=3307;Database=ckgame;Uid=root;Pwd=000000;SslMode=None;";

            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                conn.Open();

                string sql =
                    $"INSERT INTO Ranking VALUES('{player}', {dist}, '{result}')";

                MySqlCommand cmd = new MySqlCommand(sql, conn);

                cmd.ExecuteNonQuery();
            }
        }

        // Top5 함수
        static string LoadTop5()
        {
            string result = "TOP5";

            string connStr =
                "Server=127.0.0.1;Port=3307;Database=ckgame;Uid=root;Pwd=000000;SslMode=None;";

            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                conn.Open();

                string sql =
                    "SELECT PlayerName, Distance FROM Ranking ORDER BY Distance ASC LIMIT 5";

                MySqlCommand cmd = new MySqlCommand(sql, conn);

                MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    result += "|" +
                              rdr.GetString(0) +
                              ":" +
                              rdr.GetFloat(1).ToString("F2");
                }
            }

            return result;
        }
    }
}