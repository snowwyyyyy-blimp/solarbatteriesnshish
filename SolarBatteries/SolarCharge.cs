using UnityEngine;

namespace SolarBatteries
{
    /// <summary>
    /// Attached to every solar battery / solar power cell prefab instance.
    ///
    /// This component is now purely a passive data holder. It deliberately has NO
    /// Update() of its own, because that approach could never work for the cases
    /// that matter most:
    ///
    ///   * A battery restored from a save file is deserialised, not rebuilt through
    ///     CraftData, so this component is never attached to it at all.
    ///   * A battery sitting in a tool or vehicle battery slot lives on a GameObject
    ///     that is kept inactive (it only needs to render inside the tool model),
    ///     so its Update() would never tick.
    ///
    /// Instead, <see cref="SolarChargeManager"/> discovers every solar battery in
    /// the loaded world and charges them centrally, regardless of whether their
    /// GameObject is active or whether this component exists on them.
    ///
    /// A stock Battery is a pure data holder (_charge / _capacity) with no Update,
    /// so there is no vanilla charging behaviour to conflict with. Vanilla code
    /// (tool HUD, vehicle power meter, and the vanilla Charger's own
    /// `value2.charge += amount`) simply reads/writes Battery.charge, so the normal
    /// green charge indicator and chargers work untouched.
    /// </summary>
    public class SolarCharge : MonoBehaviour
    {
        /// <summary>Seconds of daylight needed to refill from empty to full.</summary>
        public float FullRechargeSeconds = 120f;

        /// <summary>Charge per real second, given this battery's capacity.</summary>
        public float PerSecond(float capacity)
        {
            return capacity / Mathf.Max(1f, FullRechargeSeconds);
        }
    }
}
