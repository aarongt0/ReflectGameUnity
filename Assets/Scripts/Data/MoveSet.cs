using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MoveSet", menuName = "Fighting/MoveSet")]
public class MoveSet : ScriptableObject
{
    public string type;

    public MoveAsset Side;
    public MoveAsset Neutral;
    public MoveAsset Down;

    [SerializeField]
    public List<MoveAsset> moveList = new();

    public IReadOnlyList<MoveAsset> Moves => moveList;

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildMoveList();
    }
#endif

    private void RebuildMoveList()
    {
        moveList.Clear();

        if (Side != null) moveList.Add(Side);
        if (Neutral != null) moveList.Add(Neutral);
        if (Down != null) moveList.Add(Down);
    }
}
