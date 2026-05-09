using UnityEngine;
using TMPro;

public class GameDirector : MonoBehaviour
{
    // 게임
    public string playerName = "Player1";

    // 자동차
    GameObject myCar;
    GameObject otherCar;
    GameObject flag;
    
    // UI
    GameObject distance;

    void Start()
    {
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
        distance = GameObject.Find("distance");
    }

    void Update()
    {
        // 나와 상대 거리 계산
        float myDistance = flag.transform.position.x - myCar.transform.position.x;
        float otherDistance = flag.transform.position.x - otherCar.transform.position.x;

        // 현재 거리 표시
        if (!CarController.gameFinished)
        {
            distance.GetComponent<TextMeshProUGUI>().text =
                "[Distance] " + myDistance.ToString("F2") + "m";
        }
        else
        {
            // 승패 판정
            string result;

            if (myDistance < otherDistance)
                result = "WIN";
            else if (myDistance > otherDistance)
                result = "LOSE";
            else
                result = "DRAW";

            // 승패와 자신의 기록 출력
            distance.GetComponent<TextMeshProUGUI>().text =
                "[" + result + "]\n" +
                "My Record : " + myDistance.ToString("F2") + "m";
        }
    }
}