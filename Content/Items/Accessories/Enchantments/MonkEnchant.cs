﻿using FargowiltasSouls.Content.Buffs.Souls;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.Projectiles.Souls;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls.Core.Systems;
using FargowiltasSouls.Core.Toggler.Content;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargowiltasSouls.Content.Items.Accessories.Enchantments
{
    public class MonkEnchant : BaseEnchant
    {

        public override Color nameColor => new(146, 5, 32);

        public override void SetDefaults()
        {
            base.SetDefaults();

            Item.rare = ItemRarityID.LightPurple;
            Item.value = 150000;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            AddEffects(player, Item);
        }
        public static void AddEffects(Player player, Item item)
        {
            
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            modPlayer.MonkEnchantActive = true;
            player.AddEffect<MonkDashEffect>(item);
        }

        public override void AddRecipes()
        {
            CreateRecipe()

            .AddIngredient(ItemID.MonkBrows)
            .AddIngredient(ItemID.MonkShirt)
            .AddIngredient(ItemID.MonkPants)
            //.AddIngredient(ItemID.MonkBelt);
            .AddIngredient(ItemID.DD2LightningAuraT2Popper)
            //meatball
            //blue moon
            //valor
            .AddIngredient(ItemID.DaoofPow)
            .AddIngredient(ItemID.MonkStaffT2)

            .AddTile(TileID.CrystalBall)
            .Register();
        }
    }
    public class MonkDashEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<ShadowHeader>();
        public override int ToggleItemType => ModContent.ItemType<MonkEnchant>();
        public static void AddDash(Player player)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            modPlayer.FargoDash = DashManager.DashType.Monk;
            modPlayer.HasDash = true;
            
            if (player.dashDelay == 1)
            {
                //dust
                double spread = 2 * Math.PI / 36;
                for (int i = 0; i < 36; i++)
                {
                    Vector2 velocity = new Vector2(2, 2).RotatedBy(spread * i);

                    int index2 = Dust.NewDust(player.Center, 0, 0, DustID.GoldCoin, velocity.X, velocity.Y, 100);
                    Main.dust[index2].noGravity = true;
                    Main.dust[index2].noLight = true;
                }
            }
            if (player.dashDelay < 0)
            {
                if (player.HasEffect<ShinobiThroughWalls>() && modPlayer.CanShinobiTeleport) //go through walls
                {
                    
                    var teleportPos = player.position;
                    float direction = Math.Sign(player.velocity.X);

                    const int length = 32;

                    teleportPos.X += length * direction;

                    if (!Collision.CanHitLine(player.Center, player.width, player.height, teleportPos, player.width, player.height))
                    {
                        modPlayer.CanShinobiTeleport = false;
                        while (Collision.SolidCollision(teleportPos, player.width, player.height))
                        {
                            if (direction == 1)
                            {
                                teleportPos.X++;
                            }
                            else
                            {
                                teleportPos.X--;
                            }
                        }
                        if (teleportPos.X > 50 && teleportPos.X < (double)(Main.maxTilesX * 16 - 50) && teleportPos.Y > 50 && teleportPos.Y < (double)(Main.maxTilesY * 16 - 50))
                        {
                            FargoSoulsUtil.GrossVanillaDodgeDust(player);
                            player.Teleport(teleportPos, 1);
                            FargoSoulsUtil.GrossVanillaDodgeDust(player);
                            NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI, teleportPos.X, teleportPos.Y, 1);

                        }
                    }

                }
            }
        }
        public static void MonkDash(Player player, int direction)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();

            modPlayer.CanShinobiTeleport = true; //allow 1 teleport per dash

            float speed = player.HasEffect<ShinobiDashEffect>() ? 20 : 16;
            player.velocity.X = speed * direction;

            player.immune = true;
            int invul = 20;
            modPlayer.MonkDashing = invul;
            player.immuneTime = Math.Max(player.immuneTime, invul);
            player.hurtCooldowns[0] = Math.Max(player.hurtCooldowns[0], invul);
            player.hurtCooldowns[1] = Math.Max(player.hurtCooldowns[1], invul);
            bool monkForce = modPlayer.ShinobiEnchantActive || modPlayer.ForceEffect<MonkEnchant>();
            bool shinobiForce = modPlayer.ShinobiEnchantActive && modPlayer.ForceEffect<ShinobiEnchant>();

            Vector2 pos = player.Center;

            int damage = monkForce ? (shinobiForce ? 1500 : 1000) : 500;

            Projectile.NewProjectile(player.GetSource_FromThis(), pos, Vector2.Zero, ModContent.ProjectileType<MonkDashDamage>(), damage, 0);

            float cd = modPlayer.ShinobiEnchantActive ? 90 : 120;
            modPlayer.DashCD = 100;
            player.dashDelay = 100;
            if (player.FargoSouls().IsDashingTimer < 20)
                player.FargoSouls().IsDashingTimer = 20;

            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.PlayerControls, number: player.whoAmI);

            //dash dust n stuff
            for (int num17 = 0; num17 < 20; num17++)
            {
                int num18 = Dust.NewDust(new Vector2(player.position.X, player.position.Y), player.width, player.height, DustID.Smoke, 0f, 0f, 100, default, 2f);
                Dust expr_CDB_cp_0 = Main.dust[num18];
                expr_CDB_cp_0.position.X += Main.rand.Next(-5, 6);
                Dust expr_D02_cp_0 = Main.dust[num18];
                expr_D02_cp_0.position.Y += Main.rand.Next(-5, 6);
                Main.dust[num18].velocity *= 0.2f;
                Main.dust[num18].scale *= 1f + Main.rand.Next(20) * 0.01f;
                //Main.dust[num18].shader = GameShaders.Armor.GetSecondaryShader(player.cShoe, this);
            }
            int num19 = Gore.NewGore(player.GetSource_FromThis(), new Vector2(player.position.X + player.width / 2 - 24f, player.position.Y + player.height / 2 - 34f), default, Main.rand.Next(61, 64), 1f);
            Main.gore[num19].velocity.X = Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num19].velocity.Y = Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num19].velocity *= 0.4f;
            num19 = Gore.NewGore(player.GetSource_FromThis(), new Vector2(player.position.X + player.width / 2 - 24f, player.position.Y + player.height / 2 - 14f), default, Main.rand.Next(61, 64), 1f);
            Main.gore[num19].velocity.X = Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num19].velocity.Y = Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num19].velocity *= 0.4f;
        }
    }
}
