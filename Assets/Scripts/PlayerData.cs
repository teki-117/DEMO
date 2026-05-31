using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public string playerName = "Á¼ÖÎ"; // Ä¬ÈÏÃû×Ö

    public static PlayerData Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}