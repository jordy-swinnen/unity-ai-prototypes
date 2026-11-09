using System.Collections.Generic;
using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>Every sound of the game. Each has a file in Audio/ named in snake case (SwingPawn is swing_pawn.wav).</summary>
    public enum SoundId
    {
        Footstep,
        KnightLeap,
        RookSlide,
        SwingPawn,
        SwingKnight,
        SwingBishop,
        SwingRook,
        SwingQueen,
        SwingKing,
        Impact,
        Collapse,
        Drum,
        SelectClink,
        IllegalKnock,
        HoverTick,
        CheckHorn,
        MateFanfare,
        DrawBell,
        PromotionAnvil,
        UiCreak,
        ResignHorn,
        GameStart,
    }

    public enum EffectKind
    {
        None,
        Dust,
        Embers,
        Sparks,
    }

    public static class SoundIds
    {
        /// <summary>The file name without its extension: SwingPawn gives swing_pawn.</summary>
        public static string FileName(SoundId id)
        {
            var name = id.ToString();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i])) sb.Append('_');
                sb.Append(char.ToLowerInvariant(name[i]));
            }
            return sb.ToString();
        }

        public static SoundId Swing(PieceType attacker)
        {
            switch (attacker)
            {
                case PieceType.Pawn: return SoundId.SwingPawn;
                case PieceType.Knight: return SoundId.SwingKnight;
                case PieceType.Bishop: return SoundId.SwingBishop;
                case PieceType.Rook: return SoundId.SwingRook;
                case PieceType.Queen: return SoundId.SwingQueen;
                default: return SoundId.SwingKing;
            }
        }
    }

    /// <summary>
    /// A sound and an effect tied to a moment of a step: a footfall's thud and dust, a swing, the clank of a blow, the
    /// embers of a collapse. The time is in the step's own time. Actor says which of the step's pieces it happens at.
    /// </summary>
    public struct FeelCue
    {
        public float Time;
        public SoundId Sound;
        public bool HasSound;
        public EffectKind Effect;
        public int Actor;
    }

    /// <summary>Decides what a step sounds like. It is a plain function of the step, so a test can read it.</summary>
    public static class SoundCues
    {
        public static List<FeelCue> For(IActionPlan plan, FeelSettings feel)
        {
            var cues = new List<FeelCue>();

            if (plan is FightPlan fight)
            {
                float blow = fight.BlowTime;
                cues.Add(Sound(Mathf.Max(0f, blow - feel.swingLead), SoundIds.Swing(fight.AttackerType)));
                cues.Add(new FeelCue { Time = blow, Sound = SoundId.Impact, HasSound = true, Effect = EffectKind.Sparks, Actor = 1 });
                cues.Add(Sound(blow, SoundId.Drum));
                cues.Add(new FeelCue { Time = blow + feel.collapseDelay, Sound = SoundId.Collapse, HasSound = true, Effect = EffectKind.Embers, Actor = 1 });
            }
            else
            {
                foreach (float f in plan.Footfalls)
                    cues.Add(new FeelCue { Time = f, Sound = SoundId.Footstep, HasSound = true, Effect = EffectKind.Dust, Actor = 0 });

                switch (plan.Name)
                {
                    case "Knight leaps": cues.Add(Sound(0f, SoundId.KnightLeap)); break;
                    case "Rook slides": cues.Add(Sound(0f, SoundId.RookSlide)); break;
                    case "King resigns": cues.Add(Sound(0f, SoundId.ResignHorn)); break;
                    case "King falls": cues.Add(new FeelCue { Time = plan.Duration * 0.55f, Sound = SoundId.Collapse, HasSound = true, Effect = EffectKind.Embers, Actor = 0 }); break;
                }
                if (plan is MovePlan move && !float.IsNaN(move.SwapTime))
                    cues.Add(new FeelCue { Time = move.SwapTime, Sound = SoundId.PromotionAnvil, HasSound = true, Effect = EffectKind.Sparks, Actor = 0 });
            }
            cues.Sort((a, b) => a.Time.CompareTo(b.Time));
            return cues;
        }

        static FeelCue Sound(float time, SoundId id) => new FeelCue { Time = time, Sound = id, HasSound = true, Actor = 0 };

        /// <summary>The cues that fall after <paramref name="from"/> and up to and including <paramref name="to"/>.</summary>
        public static void Between(IReadOnlyList<FeelCue> cues, float from, float to, List<FeelCue> into)
        {
            foreach (var cue in cues)
                if (cue.Time > from && cue.Time <= to) into.Add(cue);
        }
    }
}
