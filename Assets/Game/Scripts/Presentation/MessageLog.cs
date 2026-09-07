using System.Collections.Generic;

namespace LanternDepths.Presentation
{
    public sealed class MessageLog
    {
        private readonly Queue<string> lines = new Queue<string>();
        public void Append(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            lines.Enqueue(message); while (lines.Count > 200) lines.Dequeue();
        }
        public string FullText => string.Join("\n", lines);
        public override string ToString() => string.Join("\n", System.Linq.Enumerable.Skip(lines, System.Math.Max(0, lines.Count - 5)));
        public void Clear() => lines.Clear();
    }
}
