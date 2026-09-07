using UnityEngine;

namespace LanternDepths.Content
{
    [CreateAssetMenu(menuName = "Lantern Depths/Game Rules")]
    public sealed class GameRulesAsset : ScriptableObject
    {
        [SerializeField] private int mapWidth = 40;
        [SerializeField] private int mapHeight = 28;
        [SerializeField] private int roomCount = 8;
        [SerializeField] private int enemyCount = 6;
        [SerializeField] private int finalFloor = 5;
        [SerializeField] private int playerHp = 30;
        [SerializeField] private int playerAttack = 6;
        [SerializeField] private int playerDefense = 1;
        [SerializeField] private int[] experienceThresholds = { 12, 30, 60, 100, 160, 240, 340, 460, 600 };
        [SerializeField] private int hpPerLevel = 5;
        [SerializeField] private int attackPerLevel = 2;
        [SerializeField] private ItemDefinitionAsset[] items;
        public RunRules ToRules()
        {
            if (items == null || items.Length < 3) throw new System.InvalidOperationException("Assign healing, weapon and armor definitions.");
            var definitions = new ItemDefinition[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null) throw new System.InvalidOperationException("Missing item definition.");
                definitions[i] = items[i].ToDefinition();
            }
            return new RunRules(mapWidth, mapHeight, roomCount, enemyCount, playerHp, playerAttack, playerDefense,
                new ProgressionRules(experienceThresholds, hpPerLevel, attackPerLevel), definitions, finalFloor);
        }
    }
}
