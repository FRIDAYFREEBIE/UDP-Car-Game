using UnityEngine;
using TMPro;

public class RankingUI : MonoBehaviour
{
    public static string rankingData = "";

    // UI
    GameObject rankingText;

    void Start()
    {
        rankingText = GameObject.Find("RankingText");
    }

    void Update()
    {
        if (rankingData != "")
        {
            string[] parts = rankingData.Split('|');

            string txt = "[TOP 5]\n";

            // 1?œ„ë¶??„° ?ˆœ?„œ???ë¡? ì¶œë ¥
            for (int i = 1; i < parts.Length; i++)
            {
                txt += i + ". " + parts[i] + "\n";
            }

            rankingText.GetComponent<TextMeshProUGUI>().text = txt;
        }
    }
}