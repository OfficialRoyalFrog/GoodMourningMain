using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Spirits/Action Set", fileName = "ActionSet_Spirits")]
public class SpiritActionSetSO : ScriptableObject
{
    [SerializeField] private List<SpiritActionDefSO> actions = new();
    public IReadOnlyList<SpiritActionDefSO> Actions => actions;
    // For designer convenience
    public List<SpiritActionDefSO> EditableActions => actions;
}