﻿using PowerArgs.Cli.Physics;
using System;

namespace ConsoleGames
{
    public class TimedMineDropper : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Explosive;

        public override void FireInternal()
        {
            var mine = new TimedMine(TimeSpan.FromSeconds(2));
            SpaceTime.CurrentSpaceTime.Add(mine);
        }
    }
}
