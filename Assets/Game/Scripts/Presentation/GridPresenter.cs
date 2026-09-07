using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LanternDepths.Presentation
{
    public sealed class GridPresenter
    {
        private readonly VisualElement root;
        private readonly Dictionary<int, Pictogram> actors = new Dictionary<int, Pictogram>();
        private readonly List<Pictogram> items = new List<Pictogram>();
        private readonly Dictionary<GridPosition, ItemInstance> shownItems = new Dictionary<GridPosition, ItemInstance>();
        private float cell;
        private int height;
        private readonly System.Action<int> inspect;
        private readonly System.Action<GridPosition> inspectItem;
        public GridPresenter(VisualElement root, System.Action<int> inspect = null, System.Action<GridPosition> inspectItem = null)
        { this.root = root; this.inspect = inspect; this.inspectItem = inspectItem; }
        public Vector2 GridToWorld(GridPosition position) => new Vector2(position.X * cell, (height - 1 - position.Y) * cell);
        private void Place(VisualElement element, GridPosition position)
        {
            var p = GridToWorld(position); element.style.left = p.x; element.style.top = p.y;
            element.style.width = cell; element.style.height = cell;
        }
        public void Build(RunState state)
        {
            root.Clear(); actors.Clear(); items.Clear(); shownItems.Clear(); height = state.Floor.Map.Height;
            cell = Mathf.Min(800f / state.Floor.Map.Width, 460f / height);
            root.style.width = cell * state.Floor.Map.Width; root.style.height = cell * height;
            for (int y = 0; y < height; y++) for (int x = 0; x < state.Floor.Map.Width; x++)
            {
                var p = new GridPosition(x, y); var tile = state.Floor.Map.GetTile(p);
                var element = new VisualElement(); element.AddToClassList("tile");
                element.AddToClassList(tile.Terrain == Terrain.Wall ? "wall" : tile.Terrain == Terrain.Corridor ? "corridor" : (x + y) % 2 == 0 ? "floor-even" : "floor-odd");
                Place(element, p); root.Add(element);
            }
            var stairs = Glyph("stairs", "stairs"); Place(stairs, state.Floor.Stairs); root.Add(stairs);
            foreach (var enemy in state.Floor.Enemies) if (enemy.IsAlive) AddActor(enemy, enemy.Role.ToString(), "enemy");
            AddActor(state.Player, "lantern", "player"); Sync(state);
        }
        private Pictogram Glyph(string shape, string role)
        {
            var icon = new Pictogram(shape); icon.AddToClassList("glyph"); icon.AddToClassList(role); return icon;
        }
        private void Selectable(Pictogram icon, System.Action select)
        {
            icon.focusable = true;
            icon.RegisterCallback<ClickEvent>(_ => { icon.Focus(); select(); });
            icon.RegisterCallback<FocusInEvent>(_ => select());
        }
        private void AddActor(CharacterState actor, string glyph, string role)
        {
            var icon = Glyph(glyph, role); Place(icon, actor.Position); root.Add(icon); actors.Add(actor.Id, icon);
            icon.Charged = actor.Charged;
            if (actor.Id != 0) Selectable(icon, () => inspect?.Invoke(actor.Id));
            else icon.pickingMode = PickingMode.Ignore;
        }
        public void FacePlayer(GridPosition direction)
        {
            if (actors.TryGetValue(0, out var player)) player.Face(direction);
        }
        private void RefreshItems(RunState state)
        {
            bool unchanged = shownItems.Count == state.Floor.Items.Count();
            foreach (var pair in state.Floor.Items)
                unchanged &= shownItems.TryGetValue(pair.Key, out var previous) && previous == pair.Value;
            if (unchanged) return;
            foreach (var item in items) item.RemoveFromHierarchy(); items.Clear(); shownItems.Clear();
            foreach (var pair in state.Floor.Items)
            {
                shownItems.Add(pair.Key, pair.Value);
                var label = Glyph(pair.Value.Definition.Kind.ToString(), "item");
                var position = pair.Key;
                Selectable(label, () => inspectItem?.Invoke(position));
                Place(label, position); root.Add(label); items.Add(label);
            }
            foreach (var actor in actors.Values) actor.BringToFront();
        }
        public void Sync(RunState state)
        {
            foreach (var enemy in state.Floor.Enemies)
                if (actors.TryGetValue(enemy.Id, out var label)) { label.RemoveFromClassList("hit"); label.style.display = enemy.IsAlive ? DisplayStyle.Flex : DisplayStyle.None; Place(label, enemy.Position); label.Face(new GridPosition(state.Player.Position.X - enemy.Position.X, state.Player.Position.Y - enemy.Position.Y)); label.Charged = enemy.Charged; label.MarkDirtyRepaint(); }
            if (actors.TryGetValue(state.Player.Id, out var player)) { player.RemoveFromClassList("hit"); Place(player, state.Player.Position); }
            RefreshItems(state);
        }
        public IEnumerator Play(GameEvent e, bool animate)
        {
            if (!actors.TryGetValue(e.ActorId, out var actor)) yield break;
            if (e.Kind == EventKind.Moved)
            {
                if (e.ActorId != 0) actor.Face(new GridPosition(e.To.X - e.From.X, e.To.Y - e.From.Y));
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
        private sealed class Pictogram : VisualElement
        {
            private readonly string shape;
            public bool Charged;
            public Vector2 Facing { get; private set; } = Vector2.up;
            public Pictogram(string shape) { this.shape = shape; generateVisualContent += Draw; }
            public void Face(GridPosition direction)
            {
                if (direction.X == 0 && direction.Y == 0) return;
                Facing = new Vector2(direction.X, -direction.Y).normalized; MarkDirtyRepaint();
            }
            private void Draw(MeshGenerationContext context)
            {
                var p = context.painter2D;
                float size = Mathf.Min(contentRect.width, contentRect.height);
                if (size <= 0) return;
                Vector2 Point(float x, float y) => new Vector2(x * size, y * size);
                p.fillColor = resolvedStyle.color; p.strokeColor = resolvedStyle.color; p.lineWidth = Mathf.Max(1, size * .065f);
                void Path(bool fill, params float[] xy)
                {
                    p.BeginPath(); p.MoveTo(Point(xy[0], xy[1]));
                    for (int i = 2; i < xy.Length; i += 2) p.LineTo(Point(xy[i], xy[i + 1]));
                    if (fill) { p.ClosePath(); p.Fill(); } else p.Stroke();
                }
                void Circle(float x, float y, float radius, bool fill)
                {
                    p.BeginPath(); p.Arc(Point(x, y), radius * size, 0, 360); p.ClosePath();
                    if (fill) p.Fill(); else p.Stroke();
                }
                switch (shape)
                {
                    case "lantern":
                        Circle(.5f, .31f, .13f, false);
                        Path(true, .29f,.38f, .71f,.38f, .75f,.73f, .25f,.73f);
                        Path(false, .25f,.8f, .75f,.8f);
                        break;
                    case "Melee":
                        Circle(.5f,.57f,.25f,true);
                        Path(true, .25f,.46f, .25f,.22f, .44f,.35f);
                        Path(true, .56f,.35f, .75f,.22f, .75f,.46f);
                        break;
                    case "Archer":
                        Circle(.36f,.3f,.1f,true);
                        Path(false, .36f,.4f, .36f,.72f, .25f,.82f);
                        Path(false, .36f,.63f, .48f,.82f);
                        Path(false, .63f,.24f, .8f,.4f, .83f,.55f, .63f,.78f, .63f,.24f);
                        Path(false, .4f,.5f, .88f,.5f);
                        break;
                    case "Guardian":
                        Path(true, .5f,.18f, .8f,.47f, .5f,.83f, .2f,.47f);
                        break;
                    case "Armor":
                        Path(false, .22f,.25f, .78f,.25f, .73f,.62f, .5f,.83f, .27f,.62f, .22f,.25f);
                        Path(false, .5f,.29f, .5f,.7f); break;
                    case "Weapon":
                        Path(true, .7f,.17f, .79f,.2f, .75f,.34f, .43f,.67f, .34f,.58f);
                        Path(false, .24f,.5f, .52f,.78f); Path(false, .38f,.64f, .2f,.82f); break;
                    case "Healing": case "Blast":
                        Path(false, .4f,.23f, .6f,.23f, .6f,.4f, .74f,.52f, .74f,.8f, .26f,.8f, .26f,.52f, .4f,.4f, .4f,.23f);
                        if (shape == "Healing") { Path(false, .5f,.49f, .5f,.72f); Path(false, .38f,.6f, .62f,.6f); }
                        else { Path(false, .54f,.22f, .66f,.12f, .78f,.2f); Path(false, .8f,.08f, .8f,.3f); Path(false, .69f,.19f, .91f,.19f); }
                        break;
                    case "Blink":
                        Circle(.5f,.5f,.33f,false); Circle(.5f,.5f,.21f,false);
                        Path(false, .5f,.21f, .75f,.64f, .25f,.64f, .5f,.21f); break;
                    case "stairs":
                        Path(false, .18f,.28f, .4f,.28f, .4f,.45f, .59f,.45f, .59f,.63f, .8f,.63f, .8f,.8f, .18f,.8f); break;
                }
                if (shape == "lantern" || shape == "Melee" || shape == "Archer" || shape == "Guardian")
                {
                    var tip = new Vector2(.5f,.5f) + Facing * .48f;
                    var center = new Vector2(.5f,.5f) + Facing * .32f;
                    var side = new Vector2(-Facing.y, Facing.x) * .10f;
                    Path(true, tip.x,tip.y, center.x+side.x,center.y+side.y, center.x-side.x,center.y-side.y);
                }
                if (Charged) { p.strokeColor = new Color(1f,.3f,.27f); Circle(.5f,.5f,.45f,false); }
            }
        }
    }
}
