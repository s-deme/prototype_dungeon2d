using UnityEngine;

namespace LanternDepths.Content
{
    [CreateAssetMenu(menuName = "Lantern Depths/Item")]
    public sealed class ItemDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private ItemKind kind;
        [SerializeField, Min(1)] private int power = 1;
        [SerializeField, Min(0)] private int penalty;
        public ItemDefinition ToDefinition() => new ItemDefinition(itemId, displayName, description, kind, power, penalty);
    }
}
