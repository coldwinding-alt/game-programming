// Centralized single-player naming and story copy for Moon Lantern Park.

namespace rimrush
{
    public enum rimrushSinglePlayerNarrativeMode
    {
        Adventure,
        Tournament
    }

    public sealed class rimrushStoryPanelDefinition
    {
        public readonly string Caption;
        public readonly string ArtDirection;
        public readonly string ImageKey;

        public rimrushStoryPanelDefinition(string caption, string artDirection)
            : this(caption, artDirection, null)
        {
        }

        public rimrushStoryPanelDefinition(string caption, string artDirection, string imageKey)
        {
            Caption = caption;
            ArtDirection = artDirection;
            ImageKey = imageKey;
        }
    }

    public sealed class rimrushSinglePlayerModeDefinition
    {
        public readonly rimrushSinglePlayerNarrativeMode Mode;
        public readonly string ModeName;
        public readonly string MenuTitle;
        public readonly string Subtitle;
        public readonly string Objective;
        public readonly string Tone;
        public readonly string GameplayWrapper;
        public readonly string WorldRole;
        public readonly rimrushStoryPanelDefinition[] OpeningComic;

        public rimrushSinglePlayerModeDefinition(
            rimrushSinglePlayerNarrativeMode mode,
            string modeName,
            string menuTitle,
            string subtitle,
            string objective,
            string tone,
            string gameplayWrapper,
            string worldRole,
            rimrushStoryPanelDefinition[] openingComic)
        {
            Mode = mode;
            ModeName = modeName;
            MenuTitle = menuTitle;
            Subtitle = subtitle;
            Objective = objective;
            Tone = tone;
            GameplayWrapper = gameplayWrapper;
            WorldRole = worldRole;
            OpeningComic = openingComic ?? new rimrushStoryPanelDefinition[0];
        }
    }

    public static class rimrushSinglePlayerNarrative
    {
        public const string ParkName = "MOON LANTERN PARK";
        public const string PumpkinHeartLantern = "PUMPKIN HEART LANTERN";
        public const string LanternSigil = "LANTERN SIGIL";
        public const string LanternSigils = "LANTERN SIGILS";
        public const string Warden = "WARDEN";
        public const string Wardens = "WARDENS";
        public const string MidnightLockdownProtocol = "MIDNIGHT LOCKDOWN PROTOCOL";
        public const string LanternChampion = "LANTERN CHAMPION";

        public const string AdventureMenuTitle = "ESCAPE MOON LANTERN";
        public const string AdventureSubtitle = "Collect every Lantern Sigil before dawn.";
        public const string TournamentMenuTitle = "MOON LANTERN CUP";
        public const string TournamentSubtitle = "Win the season and become the Lantern Champion.";
        public const string TournamentResultSubtitle = "MOON LANTERN RESULT";
        public const string TournamentSeasonCompleteTitle = "SEASON COMPLETE";
        public const string TournamentFormatLine = "FORMAT: 8 PLAYERS / 2 DIVISIONS";
        public const string AdventurePreviewStatus = "PARK MAP PLAYABLE";
        public const string TournamentPreviewStatus = "FULL SEASON PLAYABLE";
        public const string TournamentSeasonBanner = "PUBLIC CHAMPIONSHIP SEASON";
        public const string TournamentSeasonHook = "A restored park turns its hidden ritual into an annual Halloween cup.";
        public const string ComicReplayButton = "READ COMIC";
        public const string AdventureLinkToCup = "The public Cup later turns this lockdown ritual into a season.";
        public const string CupLinkToAdventure = "The season quietly honors the night the park gates locked.";

        public static readonly rimrushSinglePlayerModeDefinition Adventure =
            new rimrushSinglePlayerModeDefinition(
                rimrushSinglePlayerNarrativeMode.Adventure,
                "ADVENTURE MODE",
                AdventureMenuTitle,
                AdventureSubtitle,
                "Win 1v1 duels, reclaim every Lantern Sigil, and reopen the park gates.",
                "Tense, adventurous, mysterious, but never grim.",
                "A park map links one Warden duel to the next.",
                "The night Moon Lantern Park locked its gates.",
                new[]
                {
                    new rimrushStoryPanelDefinition(
                        "Moon Lantern Park opens on Halloween night.",
                        "Fullscreen comic page: neon gates, pumpkin lights, and a court flaring to life.",
                        rimrushAssets.Images.Story.AdventureComicPage01),
                    new rimrushStoryPanelDefinition(
                        "The Heart Lantern fails and the park locks itself shut.",
                        "Fullscreen comic page: the dome flickers, gates chain shut, and Sigils scatter.",
                        rimrushAssets.Images.Story.AdventureComicPage02),
                    new rimrushStoryPanelDefinition(
                        "Win the duel, take the Sigil, and keep moving.",
                        "Fullscreen comic page: the first Warden duel begins and the escape route lights up.",
                        rimrushAssets.Images.Story.AdventureComicPage03)
                });

        public static readonly rimrushSinglePlayerModeDefinition Tournament =
            new rimrushSinglePlayerModeDefinition(
                rimrushSinglePlayerNarrativeMode.Tournament,
                "TOURNAMENT MODE",
                TournamentMenuTitle,
                TournamentSubtitle,
                "Survive the divisions, reach the finals, and claim the Moon Lantern Cup.",
                "Loud, competitive, ceremonial, and full of Halloween showmanship.",
                "Divisions lead into the final four, then the grand final.",
                "One year later, the park turns its secret ritual into a public championship.",
                new[]
                {
                    new rimrushStoryPanelDefinition(
                        "One year later, Moon Lantern Park reopens.",
                        "Fullscreen comic page: crowds return to a brighter Halloween basketball park.",
                        rimrushAssets.Images.Story.TournamentComicPage01),
                    new rimrushStoryPanelDefinition(
                        "The Wardens return as star players.",
                        "Fullscreen comic page: trophies, abstract brackets, and Warden athletes under spotlights.",
                        rimrushAssets.Images.Story.TournamentComicPage02),
                    new rimrushStoryPanelDefinition(
                        "Your season begins under the main dome.",
                        "Fullscreen comic page: the trophy, Heart Lantern, and first match ignite the dome.",
                        rimrushAssets.Images.Story.TournamentComicPage03)
                });

        public static rimrushSinglePlayerModeDefinition GetMode(rimrushSinglePlayerNarrativeMode mode)
        {
            return mode == rimrushSinglePlayerNarrativeMode.Adventure ? Adventure : Tournament;
        }

        public static string GetTournamentStageTitle(rimrushTournamentData tournament)
        {
            if (tournament == null)
            {
                return TournamentSeasonBanner;
            }

            if (tournament.Completed)
            {
                return "AWARDS PODIUM";
            }

            switch (tournament.CurrentStage)
            {
                case rimrushTournamentStage.RegularSeason:
                    return tournament.RegularSeasonCompleted ? "FINAL FOUR POSTER" : "DIVISION NIGHTS";
                case rimrushTournamentStage.SemiFinal:
                    return "FINAL FOUR CEREMONY";
                case rimrushTournamentStage.ThirdPlace:
                    return "BRONZE STAGE";
                case rimrushTournamentStage.Final:
                    return "GRAND DOME FINAL";
                default:
                    return TournamentSeasonBanner;
            }
        }

        public static string GetTournamentStageDescription(rimrushTournamentData tournament)
        {
            if (tournament == null)
            {
                return TournamentSeasonHook;
            }

            if (tournament.Completed)
            {
                return "The Cup renews the Heart Lantern, just as the first escape once did.";
            }

            switch (tournament.CurrentStage)
            {
                case rimrushTournamentStage.RegularSeason:
                    return tournament.RegularSeasonCompleted
                        ? "The top four step into the dome lights once reserved for Wardens."
                        : "Win division rounds and keep the Heart Lantern bright.";
                case rimrushTournamentStage.SemiFinal:
                    return "The former Wardens enter like star players under the restored dome.";
                case rimrushTournamentStage.ThirdPlace:
                    return "A final placement duel decides who leaves a name in the park.";
                case rimrushTournamentStage.Final:
                    return "The winner publicly guards the same Heart Lantern rescued on lockdown night.";
                default:
                    return TournamentSeasonHook;
            }
        }

        public static string GetTournamentPlacementEnding(int placement)
        {
            switch (placement)
            {
                case 1:
                    return "You become the new public guardian of the Pumpkin Heart Lantern.";
                case 2:
                    return "The main stage recognizes you, even without the champion crown.";
                case 3:
                    return "Your name stays on the park board for next year's challengers.";
                default:
                    return "The Cup ends with a clear invitation to return next season.";
            }
        }
    }
}
