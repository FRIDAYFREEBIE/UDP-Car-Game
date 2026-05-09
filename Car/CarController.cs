using UnityEngine;
using UnityEngine.InputSystem;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class CarController : MonoBehaviour
{
    // 게임
    public string playerName = "Player1";
    public static bool gameFinished = false;

    // 자동차
    float speed = 0;
    Vector2 startPos;
    GameObject myCar;
    GameObject otherCar;
    GameObject flag;

    // 서버
    UdpClient client;
    IPEndPoint serverEP;
    float targetOtherX = 0; // 상대
    bool hasSwiped = false;
    bool resultSent = false;
    float sendTimer = 0;

    void Start()
    {
        Application.targetFrameRate = 60;
        Application.runInBackground = true;

        if (playerName == "Player1")
        {
            myCar = GameObject.Find("Player1Car");
            otherCar = GameObject.Find("Player2Car");
        }
        else
        {
            myCar = GameObject.Find("Player2Car");
            otherCar = GameObject.Find("Player1Car");
        }

        flag = GameObject.Find("flag");

        // 상대 자동차 반투명 처리
        Color c = otherCar.GetComponent<SpriteRenderer>().color;
        c.a = 0.5f;
        otherCar.GetComponent<SpriteRenderer>().color = c;

        // UDP 준비
        client = new UdpClient();
        serverEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345);

        // 초기 상대 위치 저장
        targetOtherX = otherCar.transform.position.x;
    }

    void Update()
    {
        var mouse = Mouse.current;

        // 한 번만 스와이프 가능
        if (!hasSwiped && mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                startPos = mouse.position.ReadValue();
            }

            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                Vector2 endPos = mouse.position.ReadValue();

                float swipeLength = endPos.x - startPos.x;

                speed = swipeLength / 500.0f;

                hasSwiped = true;

                AudioSource audio = GetComponent<AudioSource>();
                if (audio != null)
                {
                    audio.Play();
                }
            }
        }

        myCar.transform.Translate(speed, 0, 0);
        speed *= 0.98f;

        // 0.1초마다 위치 전송
        sendTimer += Time.deltaTime;

        if (sendTimer >= 0.1f)
        {
            sendTimer = 0;

            string msg = playerName + "|" + myCar.transform.position.x.ToString("F2");

            byte[] data = Encoding.UTF8.GetBytes(msg);

            client.Send(data, data.Length, serverEP);
        }

        // 정지하면 최종 결과 전송
        if (!resultSent && hasSwiped && speed < 0.0001f)
        {
            float distance = flag.transform.position.x - myCar.transform.position.x;

            string msg = playerName + "|" + distance.ToString("F2") + "|END";

            byte[] data = Encoding.UTF8.GetBytes(msg);

            client.Send(data, data.Length, serverEP);

            resultSent = true;
            gameFinished = true;
        }

        // 서버 수신
        if (client.Available > 0)
        {
            byte[] recv = client.Receive(ref serverEP);

            string txt = Encoding.UTF8.GetString(recv);

            // 랭킹 데이터 수신
            if (txt.StartsWith("TOP5"))
            {
                RankingUI.rankingData = txt;
            }
            else
            {
                // 상대 위치 데이터 수신
                string[] parts = txt.Split('|');

                if (parts.Length == 2)
                {
                    string recvPlayer = parts[0];

                    float otherX = float.Parse(parts[1]);

                    if (recvPlayer != playerName)
                    {
                        targetOtherX = otherX;
                    }
                }
            }
        }

        // 상대 위치 적용
        otherCar.transform.position = new Vector3(
            targetOtherX,
            otherCar.transform.position.y,
            otherCar.transform.position.z);
    }
}