﻿using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria;

namespace FargowiltasSouls.Common.Graphics.Particles
{
	public abstract class BaseExpandingParticle : FargoParticle
	{
		public readonly Vector2 StartScale;

		public readonly Vector2 EndScale;

		public readonly bool UseBloom;

		public override Texture2D MainTexture => CommonBloomTexture;

		/// <summary>
		/// The scale for a texture of size 100x100px.
		/// </summary>
		public virtual Vector2 DrawScale => Scale * 0.3f;

		public BaseExpandingParticle(Vector2 position, Vector2 velocity, Color drawColor, Vector2 startScale, Vector2 endScale, int lifetime, bool useExtraBloom = false, Color? extraBloomColor = null)
		{
			Position = position;
			Velocity = velocity;
			DrawColor = drawColor;
			Scale = StartScale = startScale;
			EndScale = endScale;
			MaxLifetime = lifetime;
			UseBloom = useExtraBloom;
			extraBloomColor ??= Color.White;
			BloomColor = extraBloomColor.Value;
		}

		public sealed override void Update()
		{
			Opacity = MathHelper.Lerp(1f, 0f, FargoSoulsUtil.SineInOut(LifetimeRatio));
			Scale = Vector2.Lerp(StartScale, EndScale, FargoSoulsUtil.SineInOut(LifetimeRatio));
		}

		public sealed override void Draw(SpriteBatch spriteBatch)
		{
			spriteBatch.Draw(MainTexture, Position - Main.screenPosition, null, DrawColor with { A = 0 } * Opacity, Rotation, MainTexture.Size() * 0.5f, DrawScale, SpriteEffects.None, 0f);

			if (UseBloom)
				spriteBatch.Draw(MainTexture, Position - Main.screenPosition, null, BloomColor with { A = 0 } * 0.4f * Opacity, Rotation, MainTexture.Size() * 0.5f, DrawScale * 0.66f, SpriteEffects.None, 0f);
		}
	}
}
