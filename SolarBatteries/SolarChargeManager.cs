using System.Collections.Generic;
using UnityEngine;

namespace SolarBatteries
{
    /// <summary>
    /// Single persistent component that charges every solar battery in the game.
    ///
    /// Why centralised instead of per-item: batteries in tool and vehicle slots sit
    /// on deactivated GameObjects, and batteries restored from a save are rebuilt
    /// without going through CraftData. Neither case can be handled by a MonoBehaviour
    /// living on the item itself, so one always-running component tracks them all.
    ///
    /// Scanning is deliberately coarse (a few times per second) and the per-frame work
    /// is a tight loop over a handful of batteries, so the cost is negligible.
    /// </summary>
    public class SolarChargeManager : MonoBehaviour
    {
        /// <summary>TechTypes registered as solar items by the plugin.</summary>
        public static readonly HashSet<TechType> SolarTechTypes = new HashSet<TechType>();

        private const float ScanInterval = 0.5f;
        private const float DefaultFullRechargeSeconds = 120f;

        private readonly List<Battery> _tracked = new List<Battery>();
        private float _scanTimer;

        private void Update()
        {
            _scanTimer += Time.deltaTime;
            if (_scanTimer >= ScanInterval)
            {
                _scanTimer = 0f;
                Scan();
            }

            var cycle = DayNightCycle.main;
            bool isDay = cycle != null && cycle.IsDay();

            // Time.deltaTime (real seconds) is deliberate. DayNightCycle.deltaTime is
            // scaled by the day/night speed setting, which would silently change the
            // recharge rate on time-lapse configurations.
            float dt = Time.deltaTime;

            for (int i = _tracked.Count - 1; i >= 0; i--)
            {
                var battery = _tracked[i];
                if (battery == null)
                {
                    _tracked.RemoveAt(i);
                    continue;
                }

                if (battery.charge >= battery.capacity)
                {
                    continue;
                }

                if (!isDay)
                {
                    continue;
                }

                var solar = battery.GetComponent<SolarCharge>();
                float perSecond = solar != null
                    ? solar.PerSecond(battery.capacity)
                    : battery.capacity / DefaultFullRechargeSeconds;

                battery.charge += perSecond * dt;
            }
        }

        /// <summary>
        /// Finds solar batteries anywhere in the loaded world, including inside tools
        /// and vehicles and including instances restored from a save file.
        ///
        /// Resources.FindObjectsOfTypeAll is used instead of FindObjectsOfType because
        /// the latter skips inactive GameObjects, which is exactly the tool/vehicle case.
        /// Results are filtered down to real, scene-resident item instances so that
        /// CraftData prefab templates are never charged.
        /// </summary>
        private void Scan()
        {
            var all = Resources.FindObjectsOfTypeAll<Battery>();
            for (int i = 0; i < all.Length; i++)
            {
                var battery = all[i];
                if (battery == null || _tracked.Contains(battery))
                {
                    continue;
                }

                var go = battery.gameObject;

                // Must be a real instance living in a loaded scene. This rejects
                // prefab assets and the CraftData prefab cache.
                if (!go.scene.IsValid() || !go.scene.isLoaded)
                {
                    continue;
                }

                if (!SolarTechTypes.Contains(CraftData.GetTechType(go)))
                {
                    continue;
                }

                _tracked.Add(battery);
            }
        }
    }
}
