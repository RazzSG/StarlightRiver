﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarlightRiver.Dusts;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;
using Void = StarlightRiver.Dusts.Void;

namespace StarlightRiver.Abilities.Content
{
    public class Wisp : Ability
    {
        public override string Texture => "StarlightRiver/Pickups/Faeflame";
        public override float ActivationCost => 0.5f;

        public float Speed { get; set; }

        private const float drainAmount = 1 / 60f;
        private const float diffTolerance = 5;

        private bool safe => User.Stamina > drainAmount;

        private static readonly int size = 10; // TODO make constant in release build

        public override void OnActivate()
        {
            Speed = 5;
            for (int k = 0; k <= 50; k++)
            {
                Dust.NewDust(Player.Center - new Vector2(Player.height / 2, Player.height / 2), Player.height, Player.height, DustType<Gold2>(), Main.rand.Next(-20, 20), Main.rand.Next(-20, 20), 0, default, 1.2f);
            }
        }

        public override void UpdateActive()
        {
            Player.gravity = 0;
            Player.maxFallSpeed = Speed;
            Player.frozen = true;

            // Local velocity update
            if (Player.whoAmI == Main.myPlayer)
            {
                Player.velocity = (Main.MouseScreen - Helper.ScreenSize / 2) / 20;
                
                if (Main.netMode != NetmodeID.SinglePlayer && (Player.position - Player.oldPosition).LengthSquared() > diffTolerance * diffTolerance)
                {
                    // TODO let's not send every single control every 5 pixels someday
                    NetMessage.SendData(MessageID.PlayerControls);
                }
            }
            if (Player.velocity.LengthSquared() > Speed * Speed)
            {
                Player.velocity = Vector2.Normalize(Player.velocity) * Speed;
            }

            // Set dimensions to size
            Player.position.X += Player.width - size;
            Player.position.Y += Player.height - size;
            Player.width = size;
            Player.height = size;

            Lighting.AddLight(Player.Center, 0.15f, 0.15f, 0f);

            UpdateEffects();

            bool control = StarlightRiver.Instance.AbilityKeys.Get<Wisp>().Current;

            // If it's safe and the player wants to continue, sure
            if (safe && control)
                User.Stamina -= 1 / 60f;

            // Ok abort
            if (!safe || !control)
                AttemptDeactivate();
        }

        protected virtual void UpdateEffects()
        {
            int type = safe ? DustType<Gold>() : DustType<Void>();
            for (int k = 0; k <= 2; k++)
            {
                Dust.NewDust(Player.Center - new Vector2(4, 4), 8, 8, type);
            }
        }

        private void AttemptDeactivate()
        {
            bool canExit = SafeExit(out Vector2 safeSpot);
            if (canExit)
            {
                Deactivate();
                Player.TopLeft = safeSpot;
            }
            else if (!safe) SquishDamage();
        }

        private void SquishDamage()
        {
            //if (timer-- <= 0)
            //{
            //    Player.lifeRegen = 0;
            //    Player.lifeRegenCount = 0;
            //    Player.statLife -= 5;
            //    if (Player.statLife <= 0)
            //    {
            //        Player.KillMe(Terraria.DataStructures.PlayerDeathReason.ByCustomReason(Player.name + " couldn't maintain their form"), 0, 0);
            //    }
            //    Main.PlaySound(SoundID.NPCHit13, Player.Center);
            //}
            // TODO make this a buff?
        }

        public override void ModifyDrawLayers(List<PlayerLayer> layers)
        {
            layers.ForEach(p => p.visible = false);
        }

        public override void OnExit()
        {
            if (Player.velocity.X != 0)
                Player.direction = Math.Sign(Player.velocity.X);

            for (int k = 0; k <= 30; k++)
            {
                Dust.NewDust(Player.Center - new Vector2(Player.height / 2, Player.height / 2), Player.height, Player.height, DustType<Gold2>(), Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 0, default, 1.2f);
            }
        }

        public bool SafeExit(out Vector2 topLeft)
        {
            topLeft = Player.position;
            if (!Collision.SolidCollision(Player.TopLeft, Player.defaultWidth, Player.defaultHeight))
                return true;
            for (var x = Player.Left.X - 16; x <= Player.Left.X + 16; x += 16)
            {
                for (var y = Player.Top.Y - 16; y <= Player.Top.Y + 16; y += 16)
                {
                    topLeft = new Vector2(x, y);
                    if (!Collision.SolidCollision(topLeft, Player.defaultWidth, Player.defaultHeight))
                        return true;
                }
            }
            return false;
        }

        public override bool HotKeyMatch(TriggersSet triggers, AbilityHotkeys abilityKeys)
        {
            return abilityKeys.Get<Wisp>().Current;
        }

        private class WispMount : ModMountData
        {
            
        }
    }
}