// Quick test mode setup
// Provide a switch that shortens the game duration to 15 seconds when turned on, allowing developers to quickly test the game process without waiting for the complete game to end.

using UnityEngine;

namespace mlp
{
    /// <summary>
    /// Quick test mode setting: After turning it on, the game duration is greatly shortened, which facilitates development and debugging. Settings are persisted via PlayerPrefs.
    /// </summary>
    public static class mlpQuickTestSettings
    {
        private const string PlayerPrefsKey = "mlp.quickTestMode";

        public const float QuickMatchTime = 15f;

        /// <summary>
        /// Gets or sets whether quick test mode is enabled. This value is stored persistently via PlayerPrefs.
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
        /// Returns the match duration in seconds. When the quick test mode is on and it is regular time, the quick match duration is used, otherwise the standard duration or overtime duration is used.
        /// </summary>
        /// <param name="regularTime">true means regular time, false means overtime. </param>
        /// <returns>Game duration (seconds). </returns>
        public static float GetMatchTime(bool regularTime)
        {
            if (!regularTime)
            {
                return mlpConstants.OvertimeTime;
            }

            return Enabled ? QuickMatchTime : mlpConstants.MatchTime;
        }
    }
}
