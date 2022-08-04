using System;
using UnityEngine;
using HarmonyLib;

namespace TownOfUs.Roles
{
    public class Exploder : Role

    {
        public KillButton _explodeButton;
        public PlayerControl ClosestPlayer;
        public DateTime LastExploded;
        public PlayerControl ExplodedPlayer;
        public float TimeRemaining;
        public bool Enabled = false;

        public Exploder(PlayerControl player) : base(player)
        {
            Name = "Exploder";
            ImpostorText = () => "explode crewmates";
            TaskText = () => "explode the crewmates but be carefull not to get caught";
            Color = Palette.ImpostorRed;
            LastExploded = DateTime.UtcNow;
            RoleType = RoleEnum.Exploder;
            AddToRoleHistory(RoleType);
            Faction = Faction.Impostors;
            ExplodedPlayer = null;
        }
        public KillButton ExplodeButton
        {
            get => _explodeButton;
            set
            {
                _explodeButton = value;
                ExtraButtons.Clear();
                ExtraButtons.Add(value);
            }
        }
        public bool Exploded => TimeRemaining > 0f;
        public void Explode()
        {
            Enabled = true;
            TimeRemaining -= Time.deltaTime;
            if (MeetingHud.Instance)
            {
                TimeRemaining = 0;
            }
            if (TimeRemaining <= 0)
            {
                ExplodeKill();
            }
        }
        public void ExplodeKill()
        {
            if (!ExplodedPlayer.Is(RoleEnum.Pestilence))
            {
                Utils.RpcMurderPlayer(Player, ExplodedPlayer);
                if (!ExplodedPlayer.Data.IsDead) SoundManager.Instance.PlaySound(PlayerControl.LocalPlayer.KillSfx, false, 0.5f);
            }
            ExplodedPlayer = null;
            Enabled = false;
            LastExploded = DateTime.UtcNow;
        }
        public float ExplodeTimer()
        {
            var utcNow = DateTime.UtcNow;
            var timeSpan = utcNow - LastExploded;
            var num = CustomGameOptions.ExplodeCd * 1000f;
            var flag2 = num - (float)timeSpan.TotalMilliseconds < 0f;
            if (flag2) return 0;
            return (num - (float)timeSpan.TotalMilliseconds) / 1000f;
        }
    }
}
