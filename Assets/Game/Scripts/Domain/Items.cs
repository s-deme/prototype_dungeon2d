using System;
using System.Collections.Generic;

namespace LanternDepths
{
    public enum ItemKind { Healing, Weapon, Armor, Blink, Blast }
    public sealed class ItemDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public ItemKind Kind { get; }
        public int Power { get; }
        public int Penalty { get; }
        public bool IsEquipment => Kind == ItemKind.Weapon || Kind == ItemKind.Armor;
        public ItemDefinition(string id, string name, string description, ItemKind kind, int power, int penalty = 0)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || power < 1 || !Enum.IsDefined(typeof(ItemKind), kind)) throw new ArgumentException("Invalid item definition.");
            Id = id; Name = name; Description = description ?? ""; Kind = kind; Power = power;
            if (penalty < 0 || (penalty > 0 && !IsEquipment)) throw new ArgumentOutOfRangeException(nameof(penalty));
            Penalty = penalty;
        }
    }
    public sealed class ItemInstance
    {
        public int Id { get; }
        public ItemDefinition Definition { get; }
        public ItemInstance(int id, ItemDefinition definition)
        {
            if (id < 1) throw new ArgumentOutOfRangeException(nameof(id));
            Id = id; Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }
    }
    public sealed class Inventory
    {
        private readonly List<ItemInstance> items = new List<ItemInstance>();
        public IReadOnlyList<ItemInstance> Items { get; }
        public int Capacity { get; }
        public Inventory(int capacity = 20)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity)); Capacity = capacity;
            Items = items.AsReadOnly();
        }
        public ItemInstance Find(int id) => items.Find(item => item.Id == id);
        public bool TryAdd(ItemInstance item)
        {
            if (item == null || items.Count >= Capacity || Find(item.Id) != null) return false;
            items.Add(item); return true;
        }
        internal bool Remove(ItemInstance item) => items.Remove(item);
    }
    public sealed class Equipment
    {
        public int WeaponId { get; private set; } = -1;
        public int ArmorId { get; private set; } = -1;
        public bool IsEquipped(int id) => id == WeaponId || id == ArmorId;
        internal bool Equip(ItemInstance item)
        {
            if (IsEquipped(item.Id)) return false;
            if (item.Definition.Kind == ItemKind.Weapon) { WeaponId = item.Id; return true; }
            if (item.Definition.Kind == ItemKind.Armor) { ArmorId = item.Id; return true; }
            return false;
        }
        internal bool Unequip(int id)
        {
            if (id == WeaponId && id != -1) { WeaponId = -1; return true; }
            if (id == ArmorId && id != -1) { ArmorId = -1; return true; }
            return false;
        }
    }
    public static class StatCalculator
    {
        public static int Attack(RunState state, CharacterState actor) => Math.Max(0, actor.BaseAttack +
            (actor == state.Player ? (state.Inventory.Find(state.Equipment.WeaponId)?.Definition.Power ?? 0) - (state.Inventory.Find(state.Equipment.ArmorId)?.Definition.Penalty ?? 0) : 0));
        public static int Defense(RunState state, CharacterState actor) => Math.Max(0, actor.BaseDefense +
            (actor == state.Player ? (state.Inventory.Find(state.Equipment.ArmorId)?.Definition.Power ?? 0) - (state.Inventory.Find(state.Equipment.WeaponId)?.Definition.Penalty ?? 0) : 0));
    }
}
