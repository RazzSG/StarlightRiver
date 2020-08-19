﻿using Microsoft.Xna.Framework;
using Newtonsoft.Json.Serialization;
using StarlightRiver.GUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Odbc;
using System.Linq;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace StarlightRiver.Abilities
{
    public partial class AbilityHandler : ModPlayer
    {
        // The player's active ability.
        public Ability ActiveAbility
        {
            get => activeAbility; 
            internal set
            {
                if (value is null || Stamina > value.ActivationCost)
                {
                    if (activeAbility != null)
                        GetOrNull(activeAbility.GetType())?.OnEnd();
                    activeAbility = value;
                }
            }
        }

        // The player's stamina stats.
        public float StaminaMax => StaminaMaxDefault + StaminaMaxBonus;
        public float StaminaMaxDefault => 1 + Shards.Count / shardsPerVessel + unlockedAbilities.Count;

        public float StaminaMaxBonus
        {
            get => staminaMaxBonus;
            set
            {
                staminaMaxBonus = Math.Max(value, -StaminaMaxDefault); // Can't have less than 0 max hp...
                Stamina = stamina; // Update Stamina property setter safely
            }
        }

        public float Stamina
        {
            get => stamina;
            set => stamina = MathHelper.Clamp(value, 0, StaminaMax);
        }

        public float StaminaRegenRate { get; set; }
        public int InfusionLimit { get; set; } = 1;

        public int ShardCount => Shards.Count;
        public bool AnyUnlocked => unlockedAbilities.Count > 0;

        // Some constants.
        private const int infusionCount = 2;
        private const int shardsPerVessel = 3;

        public ShardSet Shards { get; private set; } = new ShardSet();

        // Internal-only information.
        private InfusionItem[] infusions = new InfusionItem[infusionCount];
        private Dictionary<Type, Ability> unlockedAbilities = new Dictionary<Type, Ability>();
        private int staminaRegenCD;
        private float stamina;
        private float staminaMaxBonus;
        private Ability activeAbility;

        private void Unlock(Type t, Ability ability)
        {
            unlockedAbilities[t] = ability;
            ability.User = this;
        }

        private InfusionItem GetOrNull(Type t) => infusions.FirstOrDefault(i => i?.AbilityType == t);

        /// <summary>
        /// Unlocks the ability type for the player.
        /// </summary>
        public void Unlock<T>() where T : Ability, new()
        {
            // Ensure we don't unlock the same thing twice
            if (!unlockedAbilities.ContainsKey(typeof(T))) Unlock(typeof(T), new T());
        }

        /// <summary>
        /// Tries to get an unlocked ability from the player.
        /// </summary>
        /// <typeparam name="T">The type of ability.</typeparam>
        /// <param name="value">The ability.</param>
        /// <returns>Success or not.</returns>
        public bool TryGetAbility<T>(out T value) where T : Ability
        {
            bool r = unlockedAbilities.TryGetValue(typeof(T), out var a);
            value = a as T;
            return r;
        }

        /// <summary>
        /// Checks if the given ability type is unlocked.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool Unlocked<T>() where T : Ability => TryGetAbility<T>(out _);

        /// <summary>
        /// Checks if the given ability type is unlocked.
        /// </summary>
        public bool Unlocked(Ability ability) => unlockedAbilities.ContainsKey(ability.GetType());

        /// <summary>
        /// Gets an unlocked ability from the player, or null if none exists.
        /// </summary>
        /// <typeparam name="T">The type of ability.</typeparam>
        /// <returns>The ability, if any.</returns>
        public T GetAbility<T>() where T : Ability => unlockedAbilities.TryGetValue(typeof(T), out var ret) ? (T)ret : null;

        /// <summary>
        /// Tries to add the matching infusion type.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns>If the add was successful.</returns>
        public bool SetInfusion(InfusionItem item, int slot)
        {
            if (unlockedAbilities.TryGetValue(item.AbilityType, out var t) &&
                !infusions.Any(i => i.AbilityType == item.AbilityType))
            {
                item.Ability = t;
                infusions[slot] = item;
                return true;
            }
            return false;
        }

        public InfusionItem GetInfusion(int slot) => slot < 0 || slot >= infusions.Length ? null : infusions[slot];

        public void SetStaminaRegenCD(int cooldownTicks) => staminaRegenCD = Math.Max(staminaRegenCD, cooldownTicks);

        public override TagCompound Save()
        {
            return new TagCompound
            {
                [nameof(Shards)] = Shards.ToList(),
                [nameof(unlockedAbilities)] = unlockedAbilities.Keys.Select(t => t.FullName).ToList(),
                [nameof(infusions)] = infusions.Where(t => t != null).Select(t => t.item).ToList(),
                [nameof(InfusionLimit)] = InfusionLimit
            };
        }

        public override void Load(TagCompound tag)
        {
            Shards = new ShardSet();
            unlockedAbilities = new Dictionary<Type, Ability>();
            infusions = new InfusionItem[infusionCount];
            InfusionLimit = 1;
            try
            {
                // Load shards
                var shardsTemp = tag.GetList<int>(nameof(Shards));
                foreach (var item in shardsTemp)
                {
                    Shards.Add(item);
                }

                // Load unlocked abilities and init them
                var abilitiesTemp = tag.GetList<string>(nameof(unlockedAbilities));
                foreach (var item in abilitiesTemp)
                {
                    var t = typeof(Ability).Assembly.GetType(item);
                    if (t != null)
                        Unlock(t, Activator.CreateInstance(t) as Ability);
                }

                // Load infusions
                var infusionsTemp = tag.GetList<Item>(nameof(infusions));
                for (int i = 0; i < infusionsTemp.Count; i++)
                {
                    infusions[i] = infusionsTemp[i].modItem as InfusionItem;
                }

                // Load max infusions
                InfusionLimit = tag.GetInt(nameof(InfusionLimit));
            }
            catch
            {

            }
        }

        public override void ResetEffects()
        {
            //Resets the player's stamina to prevent issues with gaining infinite stamina or stamina regeneration.
            staminaMaxBonus = 0;
            StaminaRegenRate = 1;

            if (ActiveAbility != null)
            {
                // The player cant use items while casting an ability.
                player.noItems = true;
                player.noBuilding = true;
            }
        }

        public override void UpdateDead()
        {
            ActiveAbility = null;
        }

        public override void OnRespawn(Player player)
        {
            Stamina = StaminaMax;
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            // Beyah
            foreach (var item in unlockedAbilities.Values)
            {
                if (item.Available && item.HotKeyMatch(triggersSet, StarlightRiver.Instance.AbilityKeys))
                {
                    item.Activate(this);
                    break;
                }
            }
        }

        public override void PreUpdate()
        {
            foreach (var ability in unlockedAbilities.Values)
            {
                ability.UpdateFixed();
            }

            foreach (var infusion in infusions)
            {
                if (infusion == null) continue;
                infusion.UpdateFixed();

                if (ActiveAbility?.GetType() == infusion.AbilityType) infusion.UpdateActive();
            }

            if (ActiveAbility != null)
            {
                // Update active ability
                ActiveAbility.UpdateActive();
                if (Main.netMode != NetmodeID.Server) ActiveAbility?.UpdateActiveEffects();
            }

            if (ActiveAbility != null)
            {
                player.velocity.Y += 0.01f; //Required to ensure that the game never thinks we hit the ground when using an ability. Thanks redcode!

                // Disable wings and rockets temporarily
                player.rocketRelease = true;
                player.canRocket = false;
                player.wings = -1;

                SetStaminaRegenCD(300);
            }
            else
            {
                if (staminaRegenCD > 0) staminaRegenCD--;
                Stamina += StaminaRegenRate / (staminaRegenCD + 1);
            }

            if (player.velocity != Vector2.Zero) SetStaminaRegenCD(90);
        }

        public override void ModifyDrawLayers(List<PlayerLayer> layers)
        {
            ActiveAbility?.ModifyDrawLayers(layers);
        }
    }
}