﻿using System;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Controllers;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.Appliances;
using Sims3.Gameplay.Objects.Miscellaneous;
using Sims3.SimIFace;
using Sims3.Store.Objects;
using static Sims3.Gameplay.Objects.Appliances.WashingMachine;

namespace Echoweaver.Sims3.LaundromatFix
{
    public class EWDoLaundryAtLaundromat : WashingMachine.DoLaundry
    {
        public new class Definition : InteractionDefinition<Sim, WashingMachine, EWDoLaundryAtLaundromat>
        {
            public bool mAllowStages;

            public bool AllowStages => mAllowStages;

            public Definition()
                : this(allowStages: true)
            {
            }

            public Definition(bool allowStages)
            {
                mAllowStages = allowStages;
            }

            public override string GetInteractionName(Sim actor, WashingMachine target,
                InteractionObjectPair iop)
            {
                return LocalizeString("DoLaundryAtLaundromat", target.Tuning.kCostToOperate);
            }

            public override bool Test(Sim a, WashingMachine target, bool isAutonomous,
                ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if (target.LotCurrent.IsResidentialLot)
                {
                    return false;
                }
                if (!target.CanDoLaundry())
                {
                    return false;
                }
                if (!target.IsServiceableBySim(a))
                {
                    return false;
                }
                if (target.mWashState == WashState.HasDirtyLaundry)
                {
                    return true;
                }
                if (isAutonomous && !target.CanDoLaundryAutonomously(a))
                {
                    return false;
                }
                if (a.IsSelectable && target.LotCurrent.IsCommunityLot && a.FamilyFunds
                    < target.Tuning.kCostToOperate)
                {
                    greyedOutTooltipCallback = InteractionInstance.CreateTooltipCallback(
                        LocalizeString("InsufficientFunds"));
                    return false;
                }
                return true;
            }

            public static string NoLaundryTooltip()
            {
                return LocalizeString("NoLaundryTooltip");
            }
        }
        public static new InteractionDefinition Singleton = new Definition();

        public static new InteractionDefinition SingletonNoStages = new Definition(allowStages: false);

        public override bool Run()
        {
            Main.DebugNote("Custom Do Laundry");
            bool success = base.Run();
            if (success && Target.LotCurrent.IsCommunityLot)
            {
                Main.DebugNote("Successful laundry in Laundromat");
                Hamper[] objects = Actor.LotHome.GetObjects<Hamper>();
                Main.DebugNote("Home hampers: " + objects.Length);
                foreach (Hamper hamper in objects)
                {
                    hamper.RemoveClothingPiles();
                }

                ClothingPileDry[] piles = Actor.LotHome.GetObjects<ClothingPileDry>();
                Main.DebugNote("Home clothing piles on floor: " + piles.Length);
                foreach (ClothingPileDry pile in piles)
                {
                    pile.Destroy();
                }
            }
            return true;
        }
    }

}