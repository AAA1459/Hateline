﻿using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Hateline
{
    [Tracked(true)]
    public class HatComponent : Sprite
    {
        public string crownSprite;

        public HatComponent(string hatSprite, int crownX, int crownY) : base(null, null)
        {
            CreateHat(hatSprite);
        }

        public override void Update()
        {
            base.Update();

            Visible = HatelineModule.Settings.Enabled;

            if (HatelineModule.Instance.SessionForcedHat != null)
            {
                UpdatePosition(HatelineModule.Session.mapsetX, HatelineModule.Session.mapsetY);
            }
            else if (HatelineModule.Settings.Enabled)
            {
                UpdatePosition(HatelineModule.Settings.CrownX, HatelineModule.Settings.CrownY);
            } else {
                return;
            }

            var hair = Entity.Get<PlayerHair>();
            var sprite = Entity.Get<PlayerSprite>();

            FlipX = hair.Facing == Facings.Left;
            FlipY = GravityHelperImports.IsActorInverted?.Invoke(Entity as Actor) ?? false;
            Visible = sprite.CurrentAnimationID != "dreamDashIn" && sprite.CurrentAnimationID != "dreamDashLoop";

        }

        public void UpdatePosition(float? x = null, float? y = null) {
            float px = x ?? HatelineModule.Settings.CrownX;
            float py = y ?? HatelineModule.Settings.CrownY;

            var hair = Entity.Get<PlayerHair>();
            var sprite = Entity.Get<PlayerSprite>();

            bool flipped = GravityHelperImports.IsActorInverted?.Invoke(Entity as Actor) ?? false;
            float scaleY = flipped ? -sprite.Scale.Y : sprite.Scale.Y;
            float heightOffsetY = flipped ? Height : 0;

            Position = new Vector2(px, py - heightOffsetY) + sprite.HairOffset * new Vector2((float)hair.Facing, 1) + new Vector2(0f, -2f);
            Position.Y *= scaleY;
        }

        public void CreateHat(string hatSprite)
        {
            hatSprite = HatelineModule.Instance.SessionForcedHat ?? hatSprite;
            try
            {
                GFX.SpriteBank.CreateOn(this, "hateline_" + hatSprite);
                crownSprite = hatSprite;
            } catch {
                GFX.SpriteBank.CreateOn(this, "hateline_none");
            }
            Position = new Vector2(0f, -13f);
        }

        public static void ReloadHat(string hat, bool inGame, int x, int y)
        {
            HatelineModule.Settings.SelectedHat = hat;
            HatelineModule.currentHat = hat;

            if (!inGame || !HatelineModule.Settings.Enabled)
                return;

            HatComponent playerHatComponent = Engine.Scene.Tracker.GetComponents<HatComponent>().FirstOrDefault(c => c.Entity is Player) as HatComponent;

            if (playerHatComponent == null)
                return;

            if (HatelineModule.Instance.SessionForcedHat != null)
            {
                hat = HatelineModule.Instance.SessionForcedHat;

                HatelineModule.Session.mapsetX = x;
                HatelineModule.Session.mapsetY = y;
            }
            else
            {
                x = HatelineModule.Settings.CrownX;
                y = HatelineModule.Settings.CrownY;
            }

            playerHatComponent.CreateHat(hat);
            playerHatComponent.UpdatePosition(x, y);
        }
    }
}
