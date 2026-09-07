using System;

namespace LanternDepths
{
    public enum EnemyRole { Melee, Archer, Guardian }
    public sealed class CharacterState
    {
        public int Id { get; }
        public string Name { get; }
        public GridPosition Position { get; internal set; }
        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
        public int BaseAttack { get; private set; }
        public int BaseDefense { get; }
        public int Level { get; private set; } = 1;
        public int ExperienceReward { get; }
        public EnemyRole Role { get; }
        public bool Charged { get; internal set; }
        public bool IsAlive => Hp > 0;
        public CharacterState(int id, string name, GridPosition position, int hp, int attack, int defense, int experienceReward = 0, EnemyRole role = EnemyRole.Melee)
        {
            if (hp < 1 || attack < 0 || defense < 0 || experienceReward < 0) throw new ArgumentOutOfRangeException(nameof(hp));
            Id = id; Name = name; Position = position; Hp = MaxHp = hp;
            if (!Enum.IsDefined(typeof(EnemyRole), role)) throw new ArgumentOutOfRangeException(nameof(role));
            Role = role;
            BaseAttack = attack; BaseDefense = defense; ExperienceReward = experienceReward;
        }
        public int TakeDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            int actual = Math.Min(Hp, amount); Hp -= actual; return actual;
        }
        public int Heal(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!IsAlive) return 0;
            int actual = Math.Min(MaxHp - Hp, amount); Hp += actual; return actual;
        }
        internal void LevelUp(int hpGain, int attackGain)
        {
            Level++; MaxHp += hpGain; BaseAttack += attackGain; Hp = MaxHp;
        }
    }
}
