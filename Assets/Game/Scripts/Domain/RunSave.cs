using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace LanternDepths
{
    internal static class RunSave
    {
        private const int Version = 2;
        internal const int MaxBytes = 4 * 1024 * 1024;
        public static byte[] Write(RunRules rules, RunState state, int seed, uint randomState)
        {
            if (state == null) throw new InvalidOperationException("There is no run to save.");
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            {
                writer.Write("LanternDepths"); writer.Write(Version); writer.Write(RulesKey(rules));
                writer.Write(state.RunId);
                writer.Write(seed); writer.Write(randomState); writer.Write(state.FloorNumber); writer.Write(state.TurnNumber);
                writer.Write(state.IsVictory); Position(writer, state.Floor.Entrance); Position(writer, state.Floor.Stairs);
                for (int y = 0; y < rules.Height; y++) for (int x = 0; x < rules.Width; x++)
                {
                    var tile = state.Floor.Map.GetTile(new GridPosition(x, y));
                    writer.Write((byte)tile.Terrain); writer.Write(tile.RoomId);
                }
                Position(writer, state.Player.Position); writer.Write(state.Progression.Experience); writer.Write(state.Player.Hp);
                var enemies = state.Floor.Enemies.Where(e => e.IsAlive).ToArray();
                writer.Write(enemies.Length);
                foreach (var enemy in enemies)
                {
                    writer.Write(enemy.Id); writer.Write(enemy.Name); Position(writer, enemy.Position);
                    writer.Write(enemy.MaxHp); writer.Write(enemy.Hp); writer.Write(enemy.BaseAttack);
                    writer.Write(enemy.BaseDefense); writer.Write(enemy.ExperienceReward);
                    writer.Write((byte)enemy.Role); writer.Write(enemy.Charged);
                }
                writer.Write(state.Inventory.Items.Count);
                foreach (var item in state.Inventory.Items) Item(writer, item);
                writer.Write(state.Equipment.WeaponId); writer.Write(state.Equipment.ArmorId);
                var ground = state.Floor.Items.ToArray(); writer.Write(ground.Length);
                foreach (var pair in ground) { Position(writer, pair.Key); Item(writer, pair.Value); }
            }
            byte[] payload = stream.ToArray();
            using var hash = SHA256.Create(); byte[] digest = hash.ComputeHash(payload);
            stream.Write(digest, 0, digest.Length);
            return stream.ToArray();
        }
        public static RunState Read(byte[] data, RunRules rules, out int seed, out uint randomState)
        {
            if (data == null || data.Length < 32 || data.Length > MaxBytes) throw new InvalidDataException("Invalid save size.");
            using var hash = SHA256.Create();
            if (!hash.ComputeHash(data, 0, data.Length - 32).SequenceEqual(data.Skip(data.Length - 32)))
                throw new InvalidDataException("The save is damaged (checksum mismatch).");
            using var stream = new MemoryStream(data, 0, data.Length - 32, false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadString() != "LanternDepths") throw new InvalidDataException("Invalid save header.");
            int version = reader.ReadInt32();
            if ((version != 1 && version != Version) || reader.ReadString() != RulesKey(rules, version == 1))
                throw new InvalidDataException("This save belongs to a different game version or rules configuration.");
            string runId = version >= 2 ? reader.ReadString() : new Guid(hash.ComputeHash(data).Take(16).ToArray()).ToString("N");
            if (!Guid.TryParseExact(runId, "N", out _)) throw new InvalidDataException("Invalid run identity.");
            seed = reader.ReadInt32(); randomState = reader.ReadUInt32();
            if (randomState == 0) throw new InvalidDataException("Invalid random state.");
            int floorNumber = Number(reader, 1, rules.FinalFloor), turnNumber = Number(reader, 0, int.MaxValue);
            bool victory = reader.ReadBoolean(); var entrance = Position(reader); var stairs = Position(reader);
            var map = new DungeonMap(rules.Width, rules.Height); int stairsCount = 0;
            for (int y = 0; y < rules.Height; y++) for (int x = 0; x < rules.Width; x++)
            {
                byte terrain = reader.ReadByte(); int room = Number(reader, -1, rules.Rooms - 1);
                if (terrain > (byte)Terrain.Stairs) throw new InvalidDataException("Invalid terrain.");
                if (terrain == (byte)Terrain.Stairs) stairsCount++;
                map.SetTile(new GridPosition(x, y), new TileData((Terrain)terrain, room));
            }
            if (stairsCount != 1 || map.GetTile(stairs).Terrain != Terrain.Stairs || entrance == stairs)
                throw new InvalidDataException("Invalid stairs.");
            var floor = new FloorState(map, entrance, stairs);
            var player = new CharacterState(0, "Wayfarer", Position(reader), rules.PlayerHp, rules.PlayerAttack, rules.PlayerDefense);
            var progression = new PlayerProgression(rules.Progression);
            progression.AddExperience(player, Number(reader, 0, int.MaxValue), new ActionResult());
            player.TakeDamage(player.MaxHp - Number(reader, 0, player.MaxHp));
            int count = Number(reader, 0, rules.EnemyCount);
            for (int i = 0; i < count; i++)
            {
                int id = Number(reader, 1, rules.EnemyCount); string name = reader.ReadString(); var position = Position(reader);
                if (name.Length > 100) throw new InvalidDataException("Invalid enemy name.");
                int maxHp = Number(reader, 1, 1000000), hp = Number(reader, 1, maxHp);
                var enemy = new CharacterState(id, name, position, maxHp, Number(reader, 0, 1000000), Number(reader, 0, 1000000), Number(reader, 0, 1000000));
                if (version >= 2)
                {
                    byte role = reader.ReadByte(); bool charged = reader.ReadBoolean();
                    if (role > (byte)EnemyRole.Guardian || (charged && role != (byte)EnemyRole.Guardian)) throw new InvalidDataException("Invalid enemy behavior.");
                    enemy = new CharacterState(id, name, position, maxHp, enemy.BaseAttack, enemy.BaseDefense, enemy.ExperienceReward, (EnemyRole)role) { Charged = charged };
                }
                enemy.TakeDamage(maxHp - hp); floor.RestoreEnemy(enemy);
            }
            var state = new RunState(player, floor, progression) { RunId = runId, FloorNumber = floorNumber, TurnNumber = turnNumber, IsVictory = victory };
            if (victory && (!player.IsAlive || floorNumber != rules.FinalFloor || player.Position != stairs))
                throw new InvalidDataException("Invalid victory state.");
            var ids = new HashSet<int>();
            count = Number(reader, 0, state.Inventory.Capacity);
            for (int i = 0; i < count; i++) state.Inventory.TryAdd(Item(reader, rules, ids));
            Equip(reader, state, ItemKind.Weapon); Equip(reader, state, ItemKind.Armor);
            count = Number(reader, 0, rules.Width * rules.Height);
            for (int i = 0; i < count; i++) { var position = Position(reader); floor.PlaceItem(position, Item(reader, rules, ids)); }
            if (stream.Position != stream.Length) throw new InvalidDataException("Unexpected save data.");
            return state;
        }
        private static void Equip(BinaryReader reader, RunState state, ItemKind kind)
        {
            int id = reader.ReadInt32(); if (id == -1) return;
            var item = state.Inventory.Find(id);
            if (item == null || item.Definition.Kind != kind) throw new InvalidDataException("Invalid saved equipment.");
            state.Equipment.Equip(item);
        }
        private static void Position(BinaryWriter writer, GridPosition p) { writer.Write(p.X); writer.Write(p.Y); }
        private static GridPosition Position(BinaryReader reader) => new GridPosition(reader.ReadInt32(), reader.ReadInt32());
        private static int Number(BinaryReader reader, int min, int max)
        {
            int value = reader.ReadInt32();
            if (value < min || value > max) throw new InvalidDataException("Invalid saved value.");
            return value;
        }
        private static void Item(BinaryWriter writer, ItemInstance item) { writer.Write(item.Id); writer.Write(item.Definition.Id); }
        private static ItemInstance Item(BinaryReader reader, RunRules rules, HashSet<int> ids)
        {
            int id = Number(reader, 1, int.MaxValue); string key = reader.ReadString();
            var definition = rules.Items.FirstOrDefault(item => item.Id == key);
            if (!ids.Add(id) || definition == null) throw new InvalidDataException("Invalid saved item.");
            return new ItemInstance(id, definition);
        }
        private static string RulesKey(RunRules rules, bool legacy = false)
        {
            using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
            foreach (int number in new[] { rules.Width, rules.Height, rules.Rooms, rules.EnemyCount, rules.PlayerHp,
                rules.PlayerAttack, rules.PlayerDefense, rules.FinalFloor, rules.Progression.HpGain, rules.Progression.AttackGain }) writer.Write(number);
            writer.Write(rules.Progression.Thresholds.Count); foreach (int value in rules.Progression.Thresholds) writer.Write(value);
            var items = legacy ? rules.Items.Where(i => i.Id == "ember-tonic" || i.Id == "copper-edge" || i.Id == "woven-guard").ToArray() : rules.Items.ToArray();
            writer.Write(items.Length);
            foreach (var item in items) { writer.Write(item.Id); writer.Write((int)item.Kind); writer.Write(item.Power); if (!legacy) writer.Write(item.Penalty); }
            writer.Flush(); using var hash = SHA256.Create(); return Convert.ToBase64String(hash.ComputeHash(stream.ToArray()));
        }
    }
}
