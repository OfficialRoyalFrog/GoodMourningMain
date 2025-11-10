using UnityEngine;

public interface IHoldProgressProvider
{
    /// <summary>
    /// Return a 0..1 fraction representing how much of the total hold has already been completed
    /// for the current target, across all partial holds.
    /// The HoldToInteract ring uses this to resume visual progress.
    /// </summary>
    float GetHoldProgress01();
}