using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LanternDepths.Presentation
{
    public sealed class GridPresenter
    {
        private readonly VisualElement root;
        private readonly Dictionary<int, Label> actors = new Dictionary<int, Label>();
        private readonly List<Label> items = new List<Label>();
        private float cell;
        private int height;
        private readonly System.Action<int> inspect;
        public GridPresenter(VisualElement root, System.Action<int> inspect = null) { this.root = root; this.inspect = inspect; }
        public Vector2 GridToWorld(GridPosition position) => new Vector2(position.X * cell, (height - 1 - position.Y) * cell);
        private void Place(VisualElement element, GridPosition position)
        {
            var p = GridToWorld(position); element.style.left = p.x; element.style.top = p.y;
            element.style.width = cell; element.style.height = cell;
        }
        public void Build(RunState state)
        {
            root.Clear(); actors.Clear(); items.Clear(); height = state.Floor.Map.Height;
            cell = Mathf.Min(800f / state.Floor.Map.Width, 460f / height);
            root.style.width = cell * state.Floor.Map.Width; root.style.height = cell * height;
            for (int y = 0; y < height; y++) for (int x = 0; x < state.Floor.Map.Width; x++)
            {
                var p = new GridPosition(x, y); var tile = state.Floor.Map.GetTile(p);
                var element = new VisualElement(); element.AddToClassList("tile");
                element.AddToClassList(tile.Terrain == Terrain.Wall ? "wall" : tile.Terrain == Terrain.Corridor ? "corridor" : (x + y) % 2 == 0 ? "floor-even" : "floor-odd");
                Place(element, p); root.Add(element);
            }
            var stairs = Glyph(">", "stairs"); Place(stairs, state.Floor.Stairs); root.Add(stairs);
            foreach (var enemy in state.Floor.Enemies) if (enemy.IsAlive) AddActor(enemy, enemy.Role == EnemyRole.Guardian ? "B" : enemy.Role == EnemyRole.Archer ? "r" : "m", "enemy");
            AddActor(state.Player, "@", "player"); RefreshItems(state);
        }
        private Label Glyph(string text, string role)
        {
            var label = new Label(text); label.AddToClassList("glyph"); label.AddToClassList(role); label.style.fontSize = cell * 0.8f; return label;
        }
        private void AddActor(CharacterState actor, string glyph, string role)
        {
            var label = Glyph(glyph, role); Place(label, actor.Position); root.Add(label); actors.Add(actor.Id, label);
            if (actor.Id != 0) label.RegisterCallback<ClickEvent>(_ => inspect?.Invoke(actor.Id));
        }
        private void RefreshItems(RunState state)
        {
            foreach (var item in items) item.RemoveFromHierarchy(); items.Clear();
            foreach (var pair in state.Floor.Items)
            {
                string glyph = pair.Value.Definition.Kind == ItemKind.Healing ? "!" : pair.Value.Definition.Kind == ItemKind.Weapon ? "/" : pair.Value.Definition.Kind == ItemKind.Armor ? "]" : pair.Value.Definition.Kind == ItemKind.Blink ? "?" : "*";
                var label = Glyph(glyph, "item"); Place(label, pair.Key); root.Add(label); items.Add(label);
            }
            foreach (var actor in actors.Values) actor.BringToFront();
        }
        public void Sync(RunState state)
        {
            foreach (var enemy in state.Floor.Enemies)
                if (actors.TryGetValue(enemy.Id, out var label)) { label.RemoveFromClassList("hit"); label.style.display = enemy.IsAlive ? DisplayStyle.Flex : DisplayStyle.None; Place(label, enemy.Position); }
            if (actors.TryGetValue(state.Player.Id, out var player)) { player.RemoveFromClassList("hit"); Place(player, state.Player.Position); }
            RefreshItems(state);
        }
        public IEnumerator Play(GameEvent e, bool animate)
        {
            if (!actors.TryGetValue(e.ActorId, out var actor)) yield break;
            if (e.Kind == EventKind.Moved)
            {
                Vector2 from = GridToWorld(e.From), to = GridToWorld(e.To);
                float duration = animate ? 0.07f : 0;
                for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
                {
                    var p = Vector2.Lerp(from, to, time / duration); actor.style.left = p.x; actor.style.top = p.y; yield return null;
                }
                Place(actor, e.To);
            }
            else if (e.Kind == EventKind.Damaged && animate)
            {
                actor.AddToClassList("hit"); yield return new WaitForSecondsRealtime(0.10f); actor.RemoveFromClassList("hit");
            }
            else if (e.Kind == EventKind.Died) actor.style.display = DisplayStyle.None;
        }
    }
}
