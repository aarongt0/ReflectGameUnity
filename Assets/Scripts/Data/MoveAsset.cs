using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Fighting/Move Asset")]
public class MoveAsset : ScriptableObject
{

    public int fps = 60;
    public AnimationClip anim;
    public string moveName;

    public HitboxEvent[] hitboxEvents;
    public SfxEvent[] sfxEvents;
    public HitData hitData;
    public string type = "Attack";

    public AudioClip successSfx;
    public float successVolume = 1.0f;
}


[Serializable]
public struct HitboxEvent
{
    public int frame;           
    public string hitboxId;
    public bool enabled;

    public Vector2 localPos;
    public Vector2 size;
}

[Serializable]
public struct SfxEvent
{
    public int frame;
    public AudioClip clip;
    public float volume;
}

[Serializable]
public struct HitData
{
    public float damage;
    public float angle; 
    public float power; 
    public int hitstunFrames;    
    public int hitstopFrames;   
    public bool causesLaunch;
}



