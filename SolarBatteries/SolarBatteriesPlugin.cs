using System;
using System.Collections.Generic;
using System.Collections;
using BepInEx;
using CustomBatteries.API;
using UnityEngine;

namespace SolarBatteries
{
    /// <summary>
    /// Registers two solar-fuelled items through the CustomBatteries library, which
    /// keeps them fully stock-compatible: same Battery component, same charger
    /// behaviour, same TechType roles, and the vanilla green charge indicator.
    ///
    /// This Nautilus build ships no BasePlugin type, so the entry point is a plain
    /// BepInEx BaseUnityPlugin with Awake(), matching how CustomBatteries itself is
    /// written.
    /// </summary>
    [BepInPlugin("com.pichau.solarbatteries", "Solar Batteries", "1.2.0")]
    [BepInDependency("com.snmodding.nautilus")]
    [BepInDependency("com.mrpurple6411.CustomBatteries")]
    [BepInProcess("Subnautica.exe")]
    public class SolarBatteriesPlugin : BaseUnityPlugin
    {
        /// <summary>Full empty-to-full recharge, in real seconds of daylight.</summary>
        private const float FullRechargeSeconds = 120f;

        private void Awake()
        {
            PatchSolarBattery();
            PatchSolarPowerCell();

            StartCoroutine(BootstrapManager());
        }

        /// <summary>
        /// Spins up the single persistent component that actually performs the
        /// charging. Deferred until the game object exists so the manager can live
        /// on a normal scene object and survive scene changes.
        /// </summary>
        private IEnumerator BootstrapManager()
        {
            yield return new WaitUntil(() => DayNightCycle.main != null);

            var host = new GameObject("SolarChargeManager");
            DontDestroyOnLoad(host);
            host.AddComponent<SolarChargeManager>();
        }

        private void PatchSolarBattery()
        {
            var item = new CbBattery
            {
                ID = "SolarBattery",
                Name = "Solar Battery",
                FlavorText = "A rechargeable cell that soaks up sunlight wherever it is carried.",
                EnergyCapacity = 100,
                CraftingMaterials = new List<TechType>
                {
                    TechType.Lithium,
                    TechType.Lithium,
                    TechType.Gold,
                },
                UnlocksWith = TechType.SolarPanel,
                CustomIcon = SolarIcon.Load("solarbattery.png", Logger),
                EnhanceGameObject = Enhance,
            };

            item.Patch();
            SolarChargeManager.SolarTechTypes.Add(item.TechType);
        }

        private void PatchSolarPowerCell()
        {
            var item = new CbPowerCell
            {
                ID = "SolarPowerCell",
                Name = "Solar Power Cell",
                FlavorText = "A high-capacity solar cell that recharges in any daylight.",
                EnergyCapacity = 200,
                CraftingMaterials = new List<TechType>
                {
                    TechType.Lithium,
                    TechType.Lithium,
                    TechType.Lithium,
                    TechType.Gold,
                    TechType.Gold,
                },
                UnlocksWith = TechType.SolarPanel,
                CustomIcon = SolarIcon.Load("solarpowercell.png", Logger),
                EnhanceGameObject = Enhance,
            };

            item.Patch();
            SolarChargeManager.SolarTechTypes.Add(item.TechType);
        }

        /// <summary>
        /// Runs on every instantiation of the prefab. CustomBatteries registers this
        /// through Nautilus's CustomPrefab.SetGameObject, so it fires each time CraftData
        /// builds an instance. This attaches the per-item settings and the yellow
        /// texture; the actual charging is done globally by SolarChargeManager,
        /// because prefab instantiation does not cover save-restored items or
        /// batteries living in tool and vehicle slots.
        /// </summary>
        private static void Enhance(GameObject obj)
        {
            var charge = obj.AddComponent<SolarCharge>();
            charge.FullRechargeSeconds = FullRechargeSeconds;

            SolarTint.Apply(obj);
        }
    }
}
