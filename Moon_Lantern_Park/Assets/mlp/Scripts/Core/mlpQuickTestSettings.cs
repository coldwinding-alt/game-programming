using UnityEngine;

namespace mlp
{
    public static class mlpQuickTestSettings
    {
        private const string PlayerPrefsKey = "mlp.quickTestMode";

        public const float QuickMatchTime = 15f;

        /// <summary>
        /// 获取或设置快速测试模式是否开启。该值通过 PlayerPrefs 持久化存储。
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
        /// 返回比赛时长（秒）。当快速测试模式开启且为常规时间时使用快速比赛时长，否则使用标准时长或加时时长。
        /// </summary>
        /// <param name="regularTime">true 表示常规时间，false 表示加时赛。</param>
        /// <returns>比赛时长（秒）。</returns>
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
