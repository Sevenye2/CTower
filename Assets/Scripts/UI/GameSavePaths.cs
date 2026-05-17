using System.IO;
using UnityEngine;

namespace CardTower.UI
{
    /// <summary>
    /// 存档路径约定：与游戏内写入逻辑保持一致后，主菜单即可判断「继续游戏」是否显示。
    /// </summary>
    public static class GameSavePaths
    {
        public const string SaveFileName = "card_tower_save.json";

        public static string AbsolutePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static bool HasSave() => File.Exists(AbsolutePath);
    }
}
