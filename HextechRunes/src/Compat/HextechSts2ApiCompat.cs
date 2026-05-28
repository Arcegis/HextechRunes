#if STS2_104_OR_NEWER
global using HextechCombatState = MegaCrit.Sts2.Core.Combat.ICombatState;
#else
global using HextechCombatState = MegaCrit.Sts2.Core.Combat.CombatState;
#endif
global using PowerCmd = HextechRunes.HextechPowerCmdCompat;
