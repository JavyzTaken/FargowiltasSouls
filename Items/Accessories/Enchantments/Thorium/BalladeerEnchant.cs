using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Linq;
using ThoriumMod;

namespace FargowiltasSouls.Items.Accessories.Enchantments.Thorium
{
    public class BalladeerEnchant : ModItem
    {
        private readonly Mod thorium = ModLoader.GetMod("ThoriumMod");

        public override bool Autoload(ref string name)
        {
            return false;// ModLoader.GetLoadedMods().Contains("ThoriumMod");
        }

        public override string Texture => "FargowiltasSouls/Items/Placeholder";
        
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Balladeer Enchantment");
            Tooltip.SetDefault(
@"'Echoes of the cosmic ballad dance in your head'
Each unique empowerment you have increases:
    symphonic damage by 6%
    movement speed by 3%
    your inspiration regeneration by 2%
    playing speed by 1%.");
        }

        public override void SetDefaults()
        {
            item.width = 20;
            item.height = 20;
            item.accessory = true;
            ItemID.Sets.ItemNoGravity[item.type] = true;
            item.rare = 10;
            item.value = 250000;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!Fargowiltas.Instance.ThoriumLoaded) return;

            ThoriumPlayer thoriumPlayer = player.GetModPlayer<ThoriumPlayer>(thorium);
            //dmg, regen
            thoriumPlayer.BalladeerSet = true;
            //move speed, play speed
            thoriumPlayer.headphones = true;
        }
        
        private readonly string[] items =
        {
            "BalladeerHat",
            "BalladeerShirt",
            "BalladeerBoots",
            "BalladeersTurboTuba",
            "Headset",
            "Acoustic",
            "SunflareGuitar",
            "StrawberryHeart",
            "BlackOtamatone",
            "RockstarsDoubleBassBlastGuitar" 
        };

        public override void AddRecipes()
        {
            if (!Fargowiltas.Instance.ThoriumLoaded) return;
            
            ModRecipe recipe = new ModRecipe(mod);
            
            foreach (string i in items) recipe.AddIngredient(thorium.ItemType(i));

            recipe.AddTile(TileID.LunarCraftingStation);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
