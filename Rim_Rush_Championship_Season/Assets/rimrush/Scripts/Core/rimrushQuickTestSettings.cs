using UnityEngine;

namespace rimrush
{
    public static class rimrushQuickTestSettings
    {
        private const string PlayerPrefsKey = "rimrush.quickTestMode";

        public const float QuickMatchTime = 15f;

        /// <summary>
        /// Gets or sets whether quick-test mode is active.
        /// The value is persisted in PlayerPrefs.
        /// </summary>
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(PlayerPrefsKey, 0) == 1;
            set
            {
                if (Enabled == value)
                {
                    return;
                }

                PlayerPrefs.SetInt(PlayerPrefsKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Returns the match duration in seconds. Uses the quick-match
        /// time when quick-test mode is enabled and the match is regular time,
        /// otherwise falls back to the standard or overtime duration.
        /// </summary>
        /// <param name="regularTime">True for regular time, false for overtime.</param>
        /// <returns>The match duration in seconds.</returns>
        public static float GetMatchTime(bool regularTime)
        {
            if (!regularTime)
            {
                return rimrushConstants.OvertimeTime;
            }

            return Enabled ? QuickMatchTime : rimrushConstants.MatchTime;
        }
    }
}
