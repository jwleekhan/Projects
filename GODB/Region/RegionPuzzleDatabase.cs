using Game.Puzzle;
using Game.Hint;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Region
{
    [Serializable]
    public class RegionPuzzleEntry
    {
        public string regionId;
        public List<PuzzleDefinition> puzzles = new();
        public List<HintData> hints = new();
    }

    [Serializable]
    public class SymbolSpriteData
    {
        [Serializable]
        public class StateSpriteEntry
        {
            public StateType state;
            public Sprite sprite;
        }
        public string name;
        public List<StateSpriteEntry> stateSprites;
    }

    [CreateAssetMenu(fileName = "RegionPuzzleDatabase", menuName = "Game/Region Puzzle Database")]
    public sealed class RegionPuzzleDatabase : ScriptableObject
    {
        [SerializeField] private List<RegionPuzzleEntry> entries = new();
        [SerializeField] private List<SymbolSpriteData> symbolSpriteTable = new();
        
        private Dictionary<string, RegionPuzzleEntry> entryDict = new();
        private Dictionary<(string, StateType), Sprite> symbolSpriteDict = new();

        public List<RegionPuzzleEntry> Entries => entries;

        private void OnEnable()
        {
            BuildDictionary();
        }

        public void BuildDictionary()
        {
            // entries dict
            entryDict.Clear();
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.regionId)) continue;
                if (!entryDict.ContainsKey(entry.regionId)) entryDict.Add(entry.regionId, entry);
            }

            // symbol dict
            symbolSpriteDict.Clear();
            foreach (var data in symbolSpriteTable)
            {
                if (string.IsNullOrEmpty(data.name)) continue;
                foreach (var stateSprite in data.stateSprites)
                {
                    if (!symbolSpriteDict.ContainsKey((data.name, stateSprite.state))) symbolSpriteDict.Add((data.name, stateSprite.state), stateSprite.sprite);
                }
            }
        }

        public void RegisterSymbol(string symbolName, bool includeAshed = false)
        {
            if (string.IsNullOrEmpty(symbolName)) return;
            var data = symbolSpriteTable.FirstOrDefault(s => s.name == symbolName);
            if (data == null)
            {
                data = new SymbolSpriteData { name = symbolName, stateSprites = new List<SymbolSpriteData.StateSpriteEntry>() };
                symbolSpriteTable.Add(data);
            }

            if (data.stateSprites == null) data.stateSprites = new();

            if (!data.stateSprites.Any(s => s.state == StateType.None))
                data.stateSprites.Add(new SymbolSpriteData.StateSpriteEntry { state = StateType.None });

            if (includeAshed && !data.stateSprites.Any(s => s.state == StateType.Ashed))
                data.stateSprites.Add(new SymbolSpriteData.StateSpriteEntry { state = StateType.Ashed });

            BuildDictionary();
        }

        public bool TryGetSprite(string name, StateType state, out Sprite sprite)
        {
            if (symbolSpriteDict.Count == 0 && symbolSpriteTable.Count > 0) BuildDictionary();

            return symbolSpriteDict.TryGetValue((name, state), out sprite);
        }

        public bool TryGetPuzzles(string regionId, out PuzzleDefinition[] puzzles)
        {
            if (entryDict.Count == 0 && entries.Count > 0) BuildDictionary();

            if (entryDict.TryGetValue(regionId, out var entry))
            {
                puzzles = entry.puzzles.ToArray();
                return true;
            }
            puzzles = null;
            return false;
        }

        public bool TryGetHints(string regionId, int puzzleIndex, out string[] hints)
        {
            if (entryDict.Count == 0 && entries.Count > 0) BuildDictionary();

            if (entryDict.TryGetValue(regionId, out var entry))
            {
                if (puzzleIndex >= 0 && puzzleIndex < entry.hints.Count && entry.hints[puzzleIndex] != null)
                {
                    hints = entry.hints[puzzleIndex].hints;
                    return true;
                }
            }
            hints = null;
            return false;
        }
    }
}
