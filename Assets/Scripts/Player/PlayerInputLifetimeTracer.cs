using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputLifetimeTracer : MonoBehaviour
{
    private PlayerInput pi;
    private void Awake()
    {
        pi = GetComponent<PlayerInput>();
        Dump("Awake");
    }

    private void OnEnable() => Dump("OnEnable");
    private void Start() => Dump("Start");
    private void OnDisable() => Dump("OnDisable");
    private void OnDestroy() => Dump("OnDestroy");

    private void Dump(string where)
    {
        var rt = Time.realtimeSinceStartup;
        var playing = Application.isPlaying;

        string piId = pi ? pi.GetInstanceID().ToString() : "null";
        string actionsId = (pi && pi.actions) ? pi.actions.GetInstanceID().ToString() : "null";
        string mapName = (pi && pi.currentActionMap != null) ? pi.currentActionMap.name : "null";

        Debug.Log($"[LifetimeTracer] {where} rt={rt:F3} playing={playing} PI={piId} Actions={actionsId} CurrentMap={mapName}");
    }
}
