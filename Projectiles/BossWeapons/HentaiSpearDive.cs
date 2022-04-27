﻿namespace FargowiltasSouls.Projectiles.BossWeapons
{
    public class HentaiSpearDive : HentaiSpear
    {
        public override string Texture => "FargowiltasSouls/Projectiles/BossWeapons/HentaiSpear";

        public override void AI()
        {
            base.AI();
            Projectile.localAI[0]++;
        }

        public override bool? CanDamage()
        {
            if (Projectile.localAI[0] > 2)
                return true;
            return null;
        }
    }
}