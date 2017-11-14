﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Ship;

namespace Ship
{
    namespace Firespray31
    {
        public class KrassisTrelix : Firespray31
        {
            public KrassisTrelix() : base()
            {
                PilotName = "Krassis Trelix";
                ImageUrl = "https://raw.githubusercontent.com/guidokessels/xwing-data/master/images/pilots/Galactic%20Empire/Firespray-31/krassis-trelix.png";
                PilotSkill = 5;
                Cost = 36;

                IsUnique = true;

                PilotAbilities.Add(new PilotAbilitiesNamespace.KrassisTrelixAbility());

                faction = Faction.Imperial;

                SkinName = "Krassis Trelix";
            }
        }
    }
}

namespace PilotAbilitiesNamespace
{
    public class KrassisTrelixAbility : GenericPilotAbility
    {
        public override void Initialize(GenericShip host)
        {
            base.Initialize(host);

            Host.AfterGenerateAvailableActionEffectsList += KrassisTrelixPilotAbility;
        }

        public void KrassisTrelixPilotAbility(GenericShip ship)
        {
            ship.AddAvailableActionEffect(new KrassisTrelixAction());
        }

        private class KrassisTrelixAction : ActionsList.GenericAction
        {
            public KrassisTrelixAction()
            {
                Name = EffectName = "Krassis Trelix's ability";
                IsReroll = true;
            }

            public override void ActionEffect(System.Action callBack)
            {
                DiceRerollManager diceRerollManager = new DiceRerollManager
                {
                    NumberOfDiceCanBeRerolled = 1,
                    CallBack = callBack
                };
                diceRerollManager.Start();
            }

            public override bool IsActionEffectAvailable()
            {
                bool result = false;
                if (Combat.AttackStep == CombatStep.Attack && (Combat.ChosenWeapon as Upgrade.GenericSecondaryWeapon) != null)
                {
                    result = true;
                }
                return result;
            }

            public override int GetActionEffectPriority()
            {
                int result = 0;

                if (Combat.AttackStep == CombatStep.Attack && (Combat.ChosenWeapon as Upgrade.GenericSecondaryWeapon) != null)
                {
                    if (Combat.DiceRollAttack.Blanks > 0)
                    {
                        result = 90;
                    }
                    else if (Combat.DiceRollAttack.Focuses > 0 && Combat.Attacker.GetAvailableActionEffectsList().Count(n => n.IsTurnsAllFocusIntoSuccess) == 0)
                    {
                        result = 90;
                    }
                    else if (Combat.DiceRollAttack.Focuses > 0)
                    {
                        result = 30;
                    }
                }

                return result;
            }

        }
    }
}