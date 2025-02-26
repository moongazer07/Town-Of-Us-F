﻿using System;
using HarmonyLib;
using Hazel;
using TownOfUs.Roles;
using TownOfUs.CrewmateRoles.MedicMod;

namespace TownOfUs.NeutralRoles.WerewolfMod
{
    [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
    public class PerformKill
    {
        public static bool Prefix(KillButton __instance)
        {
            var flag = PlayerControl.LocalPlayer.Is(RoleEnum.Werewolf);
            if (!flag) return true;
            if (PlayerControl.LocalPlayer.Data.IsDead) return false;
            if (!PlayerControl.LocalPlayer.CanMove) return false;
            var role = Role.GetRole<Werewolf>(PlayerControl.LocalPlayer);
            if (role.Player.inVent) return false;

            if (__instance == role.RampageButton)
            {
                if (role.RampageTimer() != 0) return false;
                if (!__instance.isActiveAndEnabled || __instance.isCoolingDown) return false;

                role.TimeRemaining = CustomGameOptions.RampageDuration;
                role.Rampage();
                return false;
            }

            if (role.KillTimer() != 0) return false;
            if (!role.Rampaged) return false;
            if (__instance != DestroyableSingleton<HudManager>.Instance.KillButton) return true;
            if (!__instance.isActiveAndEnabled || __instance.isCoolingDown) return false;
            if (role.ClosestPlayer == null) return false;
            var distBetweenPlayers = Utils.GetDistBetweenPlayers(PlayerControl.LocalPlayer, role.ClosestPlayer);
            var flag3 = distBetweenPlayers <
                        GameOptionsData.KillDistances[PlayerControl.GameOptions.KillDistance];
            if (!flag3) return false;

            if (role.ClosestPlayer.Is(RoleEnum.Pestilence))
            {
                Utils.RpcMurderPlayer(role.ClosestPlayer, role.Player);
                return false;
            }
            if (role.ClosestPlayer.IsInfected() || PlayerControl.LocalPlayer.IsInfected())
            {
                foreach (var pb in Role.GetRoles(RoleEnum.Plaguebearer)) ((Plaguebearer)pb).RpcSpreadInfection(role.ClosestPlayer, role.Player);
            }
            if (role.ClosestPlayer.IsOnAlert())
            {
                if (role.ClosestPlayer.IsShielded())
                {
                    var medic = role.ClosestPlayer.GetMedic().Player.PlayerId;
                    var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                        (byte)CustomRPC.AttemptSound, SendOption.Reliable, -1);
                    writer.Write(medic);
                    writer.Write(role.ClosestPlayer.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);

                    if (CustomGameOptions.ShieldBreaks) role.LastKilled = DateTime.UtcNow;

                    StopKill.BreakShield(medic, role.ClosestPlayer.PlayerId,
                        CustomGameOptions.ShieldBreaks);
                    if (!role.Player.IsProtected())
                        Utils.RpcMurderPlayer(role.ClosestPlayer, role.Player);
                }
                else if (role.Player.IsShielded())
                {
                    var medic = role.Player.GetMedic().Player.PlayerId;
                    var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                        (byte)CustomRPC.AttemptSound, SendOption.Reliable, -1);
                    writer.Write(medic);
                    writer.Write(role.Player.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);

                    if (CustomGameOptions.ShieldBreaks) role.LastKilled = DateTime.UtcNow;

                    StopKill.BreakShield(medic, role.Player.PlayerId,
                        CustomGameOptions.ShieldBreaks);
                    if (CustomGameOptions.KilledOnAlert && !role.ClosestPlayer.IsProtected())
                    {
                        role.LastKilled = DateTime.UtcNow;
                        Utils.RpcMurderPlayer(role.Player, role.ClosestPlayer);
                    }
                }
                else if (role.ClosestPlayer.IsProtected())
                {
                    Utils.RpcMurderPlayer(role.ClosestPlayer, role.Player);
                }
                else if (CustomGameOptions.KilledOnAlert && role.Player.IsProtected())
                {
                    role.LastKilled = DateTime.UtcNow;
                    Utils.RpcMurderPlayer(role.Player, role.ClosestPlayer);
                }
                else if (!CustomGameOptions.KilledOnAlert && role.Player.IsProtected())
                {
                    role.LastKilled.AddSeconds(CustomGameOptions.ProtectKCReset);
                }
                else
                {
                    Utils.RpcMurderPlayer(role.ClosestPlayer, role.Player);
                    if (CustomGameOptions.KilledOnAlert)
                    {
                        role.LastKilled = DateTime.UtcNow;
                        Utils.RpcMurderPlayer(role.Player, role.ClosestPlayer);
                    }
                }
                return false;
            }
            else if (role.ClosestPlayer.IsShielded())
            {
                var medic = role.ClosestPlayer.GetMedic().Player.PlayerId;
                var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                    (byte)CustomRPC.AttemptSound, SendOption.Reliable, -1);
                writer.Write(medic);
                writer.Write(role.ClosestPlayer.PlayerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);

                if (CustomGameOptions.ShieldBreaks) role.LastKilled = DateTime.UtcNow;

                StopKill.BreakShield(medic, role.ClosestPlayer.PlayerId,
                    CustomGameOptions.ShieldBreaks);

                return false;
            }
            else if (role.ClosestPlayer.IsVesting())
            {
                role.LastKilled.AddSeconds(CustomGameOptions.VestKCReset);

                return false;
            }
            else if (role.ClosestPlayer.IsProtected())
            {
                role.LastKilled.AddSeconds(CustomGameOptions.ProtectKCReset);

                return false;
            }

            role.LastKilled = DateTime.UtcNow;
            Utils.RpcMurderPlayer(role.Player, role.ClosestPlayer);
            return false;
        }
    }
}
