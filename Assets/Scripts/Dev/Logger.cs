using UnityEngine;

public class Logger : MonoBehaviour
{
    [SerializeField] LogConfig config;
    string Team;

    private void Start()
    {
        Team = GetComponentInParent<PlayerCombat>().Team;
    }
    public void log(string message, int level = 1)
    {
        if (config._loggingEnabled && level >= config._loggingLevel)
        {
            string header = this.name.ToString();
            Debug.Log($"{Header(level)} ({Team}) {header}: {message}");
        }
    }

    private string Header(int level)
    {
        switch (level)
        {
            case 0:
                return "RAW";

            case 1:
                return "DEBUG";

            case 2:
                return "CRITICAL";

            case 3:
                return "EXTREME ERROR";

            default: return "";
        }
    }
}
