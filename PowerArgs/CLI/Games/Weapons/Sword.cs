﻿using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Games
{
    public class Sword : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Primary;

        public int Range { get; set; } = 7;

        private List<Blade> activeBlades = new List<Blade>();

        public override void FireInternal(bool alt)
        {
            activeBlades.ForEach(b => { if (b.Lifetime.IsExpired == false) b.Lifetime.Dispose(); });
            activeBlades.Clear();
            for(var i = 1; i < 1+  Range.NormalizeQuantity(Holder.CalculateAngleToTarget()); i++)
            {
                var location = Holder.Center().MoveTowards(Holder.CalculateAngleToTarget(), i);
                var newBounds = Cli.Physics.RectangularF.Create(location.Left - .5f, location.Top - .5f, 1, 1);
                if (SpaceTime.CurrentSpaceTime.Bounds.Contains(newBounds))
                {
                    var blade = new Blade(this);
                    var holderLocation = Holder.TopLeft();
                    blade.MoveTo(newBounds.Left, newBounds.Top);
                    SpaceTime.CurrentSpaceTime.Add(blade);
                    activeBlades.Add(blade);
                    OnWeaponElementEmitted.Fire(blade);
                }
            }
        }
    }

    public class Blade : WeaponElement
    {
        public Character Holder { get; set; }

        private float dx;
        private float dy;

        public Blade(Sword w) : base(w)
        {
            dx = Holder.Left - this.Left;
            dy = Holder.Top - this.Top;

            this.Added.SubscribeOnce(async () =>
            {
                while (this.Lifetime.IsExpired == false)
                {
                    Evaluate();
                    await Time.CurrentTime.YieldAsync();
                }
            });
        }

        private void Evaluate()
        {
            if(this.CalculateAge().TotalSeconds >= .3)
            {
                this.Lifetime.Dispose();
            }

            this.MoveTo(Holder.Left + dx, Holder.Top + dy);

            DamageBroker.Instance.DamageableElements
                .Where(e => e != Holder && e.Touches(this))
                .ForEach(d => DamageBroker.Instance.ReportDamage(new DamageEventArgs()
                {
                    Damager = this,
                    Damagee = d
                }));
        }
    }

    [SpacialElementBinding(typeof(Blade))]
    public class BladeRenderer : SpacialElementRenderer
    {
        private ConsoleString DefaultStyle => new ConsoleString("=", ConsoleColor.Cyan);
        protected override void OnPaint(ConsoleBitmap context) => context.DrawString(DefaultStyle, 0, 0);
    }
}
