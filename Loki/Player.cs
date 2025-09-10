﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using JetBrains.Annotations;

namespace Loki
{
    public class Player: INotifyPropertyChanged
    {
        private const int Version = 29;
        private const int InventoryVersion = 106;
        private const int SkillVersion = 2;

        private float _maxHealth;
        private float _curHealth;
        private float _maxStamina;
        public bool LegacyFirstSpawn { get; private set; }
        private float _timeSinceDeath;
        private string _guardianPower;
        private float _guardianPowerCooldown;
        private List<Item> _inventory = new List<Item>();
        private readonly List<string> _knownRecipes = new List<string>();
        private readonly List<(string, int)> _knownStations = new List<(string, int)>();
        private readonly List<string> _knownMaterials = new List<string>();
        private List<string> _shownTutorials = new List<string>();
        private readonly List<string> _uniques = new List<string>();
        private readonly List<string> _trophies = new List<string>();
        private readonly List<Biome> _knownBiomes = new List<Biome>();
        private readonly List<(string, string)> _knownTexts = new List<(string, string)>();
        private string _beard;
        private string _hair;
        private Vector3 _skinColour;
        private Vector3 _hairColour;
        private float _stamina;
        private float _maxEitr;
        private float _eitr;

        private int _modelIndex;
        private readonly List<Food> _food = new List<Food>();
        private readonly List<(string, string)> customPlayerData = new List<(string, string)>();
        public ObservableCollection<Skill> Skills { get; } = new ObservableCollection<Skill>();

        public Inventory Inventory { get; } = new Inventory(8, 4);

        public Beard Beard
        {
            get => Beard.FromCode(_beard);
            set => _beard = value.Code;
        }

        public Hair Hair
        {
            get => Hair.FromCode(_hair);
            set => _hair = value.Code;
        }

        public Model Model
        {
            get => Model.FromIndex(_modelIndex);
            set => _modelIndex = value.Index;
        }

        private Color ColorFromVector3(Vector3 vec)
        {
            // .NET 7 bug with `FromScRGB` breaks ToString() on Color class... need to use `FromRGB` method to avoid it.
            byte red = (byte)(vec.X * 255);
            byte green = (byte)(vec.Y * 255);
            byte blue = (byte)(vec.Z * 255);
            return Color.FromRgb(red, green, blue);
        }

        private Vector3 Vector3FromColor(Color c)
        {
            return new Vector3(c.R / 255f, c.G / 255f, c.B / 255f);
        }

        public Color HairColour
        {
            get => ColorFromVector3(_hairColour);
            set
            {
                if (value.Equals(HairColour)) return;
                _hairColour = Vector3FromColor(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(RawHairRgb));
            }
        }

        public Color SkinColour
        {
            get => ColorFromVector3(_skinColour);
            set
            {
                if (value.Equals(SkinColour)) return;
                _skinColour = Vector3FromColor(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SkinGlow));
                OnPropertyChanged(nameof(RawSkinRgb));
            }
        }

        public float SkinGlow
        {
            get => _skinColour.Length();
            set
            {
                if (value.Equals(SkinGlow)) return;
                _skinColour.Normalise(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SkinColour));
                OnPropertyChanged(nameof(RawSkinRgb));
            }
        }

        public string RawSkinRgb => $"RGB ({_skinColour.X:F3}, {_skinColour.Y:F3}, {_skinColour.Z:F3})";
        public string RawHairRgb => $"RGB ({_hairColour.X:F3}, {_hairColour.Y:F3}, {_hairColour.Z:F3})";

        private Player()
        {
        }

        public static Player Read(Stream input, bool leaveOpen = false)
        {
            using var reader = new BinaryReader(input, Encoding.UTF8, leaveOpen);

            int expectedPlayerDataLength = reader.ReadInt32();
            var playerDataStartPos = input.Position;

            var version = reader.ReadInt32();

            var maxHealth = version >= 7 ? reader.ReadSingle() : 25f;  // ToDo: Verify default still are correct
            var curHealth = reader.ReadSingle();
            var maxStamina = version >= 10 ? reader.ReadSingle() : 100f; // ToDo: Verify default still are correct           

            // As of Player version 28 and Profile version 40 FirstSpawn is within profile part of save, it seems
            var legacyFirstSpawn = false;
            if (version >= 8 && version < 28)
            {
                legacyFirstSpawn = reader.ReadBoolean(); // no skip, pass on to profile (but do not use in player)
            }

            var timeSinceDeath = version >= 20 ? reader.ReadSingle() : 999999f;
            var guardianPower = version >= 23 ? reader.ReadString() : string.Empty;
            var guardianPowerCooldown = version >= 24 ? reader.ReadSingle() : default;

            var player = new Player
            {
                _maxHealth = maxHealth,
                _curHealth = curHealth,
                _maxStamina = maxStamina,            
                LegacyFirstSpawn = legacyFirstSpawn,
                _timeSinceDeath = timeSinceDeath,
                _guardianPower = guardianPower,
                _guardianPowerCooldown = guardianPowerCooldown,
            };

            // Skip over 'ZDOID', long + uint 
            if (version == 2)
            {
                input.Position += 12; 
            }

            player._inventory = ReadInventory(input, true);
            player.UpdateInventorySlots();

            player._knownRecipes.AddRange(reader.ReadStrings(reader.ReadInt32()));

            if (version < 15)
            {
                int count = reader.ReadInt32();
                while (count-- > 0) reader.ReadString();
            }
            else
            {
                int count = reader.ReadInt32();
                while (count-- > 0)
                    player._knownStations.Add((reader.ReadString(), reader.ReadInt32()));
            }

            player._knownMaterials.AddRange(reader.ReadStrings(reader.ReadInt32()));

            if (version < 19 || version >= 21)
                player._shownTutorials.AddRange(reader.ReadStrings(reader.ReadInt32()));

            if (version >= 6)
                player._uniques.AddRange(reader.ReadStrings(reader.ReadInt32()));

            if (version >= 9)
                player._trophies.AddRange(reader.ReadStrings(reader.ReadInt32()));

            if (version >= 18) 
                player._knownBiomes.AddRange(reader.ReadInt32s(reader.ReadInt32()).Cast<Biome>());

            if (version >= 22)
            {
                int count = reader.ReadInt32();
                while(count -- >0)
                    player._knownTexts.Add((reader.ReadString(), reader.ReadString()));
            }

            if (version >= 4)
            {
                player._beard = reader.ReadString();
                player._hair = reader.ReadString();
            }

            if (version >= 5)
            {
                player._skinColour = reader.ReadVector3();
                player._hairColour = reader.ReadVector3();
            }

            player._modelIndex = version >= 11 ? reader.ReadInt32() : default;

            if (version >= 12)
            {
                int foodCount = reader.ReadInt32();
                while (foodCount-- > 0)
                {
                    if (version >= 14)
                    {
                        string name = reader.ReadString();
                        float time = default;
                        float health = default;
                        float stamina = default;
                        if(version >= 25)
                        {
                            time = reader.ReadSingle();
                        }
                        else
                        {
                            health = reader.ReadSingle();
                            stamina = version >= 16 ? reader.ReadSingle() : default;
                        }
                        player._food.Add(new Food(name, time, health, stamina));
                    }
                    else
                    {
                        // Skip legacy data.
                        reader.ReadString();
                        input.Position += version >= 13 ? 24 : 28;
                    }
                }
            }

            if (version >= 17)
            {
                int skillVersion = reader.ReadInt32();
                int skillCount = reader.ReadInt32();
                while (skillCount-- > 0)
                {
                    SkillType type = (SkillType) reader.ReadInt32();
                    float level = reader.ReadSingle();
                    float accumulator = skillVersion >= 2 ? reader.ReadSingle() : default;
                    player.Skills.Add(new Skill(type, level, accumulator));
                }
            }

            if (version >= 26)
            {
                var customDataCount = reader.ReadInt32();
                for (int i = 0; i < customDataCount; i++)
                {
                    player.customPlayerData.Add((reader.ReadString(), reader.ReadString()));
                }

                player._stamina = reader.ReadSingle();
                player._maxEitr = reader.ReadSingle();
                player._eitr = reader.ReadSingle();
            }

            if (version < 27)
            {
                if (player._knownMaterials.Contains("$item_flametal"))
                {
                    player._knownMaterials.Remove("$item_flametal");
                    player._knownMaterials.Add("$item_flametal_old");
                }
                if (player._knownMaterials.Contains("$item_flametalore"))
                {
                    player._knownMaterials.Remove("$item_flametalore");
                    player._knownMaterials.Add("$item_flametalore_old");
                }
            }

            // Sanity check - compare with player data length provided.
            long amountRead = input.Position - playerDataStartPos;
            if (amountRead != expectedPlayerDataLength)
            {
                var message =
                    "Amount of data read for player field did not match length header. " +
                    $"Data read = {amountRead}B, Length in header = {expectedPlayerDataLength}B";

                var result = MessageBox.Show(
                    $"{message}" + Environment.NewLine + Environment.NewLine +
                    "Do you want to continue loading? (Not Recommended)",
                    $"Error reading {nameof(Player)}",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    throw new InvalidDataException(message);
                }
            }

            return player;
        }

        private void UpdateInventorySlots()
        {
            foreach (var item in _inventory)
            {
                var slot = Inventory.GetSlotAt(item.Pos);
                if (slot != null)
                    slot.Item = item;
            }
        }

        public void Write(Stream output, bool leaveOpen = false)
        {
            using var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen);
            
            writer.Write(Version);
            writer.Write(_maxHealth);
            writer.Write(_curHealth);
            writer.Write(_maxStamina);           
            writer.Write(_timeSinceDeath);
            writer.Write(_guardianPower);
            writer.Write(_guardianPowerCooldown);

            writer.Write(InventoryVersion);
            // Rebuild Inventory (TODO: Review)
            _inventory = Inventory.Slots.Select(slot => slot.Item).Where(item => item != null).ToList();
            writer.Write(_inventory.Count);
            foreach (var item in _inventory)
            {
                writer.Write(item.Name);
                writer.Write(item.Stack);
                writer.Write(item.Durability);
                writer.Write(item.Pos);
                writer.Write(item.Equiped);
                writer.Write(item.Quality);
                writer.Write(item.Variant);
                writer.Write(item.CrafterId);
                writer.Write(item.CrafterName);
                writer.Write(item.ItemData.Count);
                foreach (var customData in item.ItemData)
                {
                    var (key, value) = customData;
                    writer.Write(key);
                    writer.Write(value);
                }
                writer.Write(item.WorldLevel);
                writer.Write(item.PickedUp);
            }

            writer.WriteCountItems(_knownRecipes);
            writer.Write(_knownStations.Count);
            foreach ((string name, int num) in _knownStations)
            {
                writer.Write(name);
                writer.Write(num);
            }
            writer.WriteCountItems(_knownMaterials);
            writer.WriteCountItems(_shownTutorials);
            writer.WriteCountItems(_uniques);
            writer.WriteCountItems(_trophies);
            writer.WriteCountItems(_knownBiomes.Cast<int>().ToList());
            writer.Write(_knownTexts.Count);
            foreach ((string key, string value) in _knownTexts)
            {
                writer.Write(key.Replace("\u0016", ""));
                writer.Write(value.Replace("\u0016", ""));
            }

            writer.Write(_beard);
            writer.Write(_hair);
            writer.Write(_skinColour);
            writer.Write(_hairColour);
            writer.Write(_modelIndex);
            writer.Write(_food.Count);
            foreach (var food in _food)
            {
                writer.Write(food.Name);
                writer.Write(food.Time);
            }

            writer.Write(SkillVersion);
            writer.Write(Skills.Count);
            foreach (var skill in Skills)
            {
                writer.Write((int)skill.Type);
                writer.Write(skill.Level);
                writer.Write(skill.Accumulator);
            }

            writer.Write(customPlayerData.Count);
            foreach (var customData in customPlayerData)
            {
                var (key, value) = customData;
                writer.Write(key);
                writer.Write(value);
            }

            writer.Write(_stamina);
            writer.Write(_maxEitr);
            writer.Write(_eitr);
        }

        private static List<Item> ReadInventory(Stream input, bool leaveOpen = false)
        {
            using var reader = new BinaryReader(input, Encoding.UTF8, leaveOpen);
            var version = reader.ReadInt32();
            var itemCount = reader.ReadInt32();
            var items = new List<Item>(itemCount);
            if (version == 106) // ToDo: check if changed from <= in 0.221.4 (Call To Arms) update or wrong aldready?
            {
                for (int i = 0; i < itemCount; i++)
                {
                    string name = reader.ReadString();
                    int stack = reader.ReadInt32();
                    float durability = reader.ReadSingle();
                    Vector2i pos = reader.ReadVector2i();
                    bool equipped = reader.ReadBoolean();
                    int quality = reader.ReadInt32();
                    int variant = reader.ReadInt32();
                    long crafterId = reader.ReadInt64();
                    string crafterName = reader.ReadString();
                    var itemData = new List<(string, string)>();
                    var itemDataCount = reader.ReadInt32();
                    for (int j = 0; j < itemDataCount; j++)
                    {
                        itemData.Add((reader.ReadString(), reader.ReadString()));
                    }
                    int worldLevel = reader.ReadInt32();
                    bool pickedUp = reader.ReadBoolean();
                    Debug.WriteLine($"ReadInv: Item={name}, Position={pos}");
                    if (name != "") items.Add(new Item(name, stack, durability, pos, equipped, quality, variant, crafterId, crafterName, itemData, worldLevel, pickedUp));
                }
            }
            else
            {
                for (int i = 0; i < itemCount; i++)
                {
                    string name = reader.ReadString();
                    int stack = reader.ReadInt32();
                    float durability = reader.ReadSingle();
                    Vector2i pos = reader.ReadVector2i();
                    bool equipped = reader.ReadBoolean();
                    int quality = version >= 101 ? reader.ReadInt32() : 1;
                    int variant = version >= 102 ? reader.ReadInt32() : 0;
                    (long crafterId, string crafterName) =
                        version >= 103 ? (reader.ReadInt64(), reader.ReadString()) : (0, string.Empty);
                    var itemData = new List<(string, string)>();
                    if (version >= 104)
                    {
                        var itemDataCount = reader.ReadInt32();
                        for (int j = 0; j < itemDataCount; j++)
                        {
                            itemData.Add((reader.ReadString(), reader.ReadString()));
                        }
                    }
                    Debug.WriteLine($"ReadInv: Item={name}, Position={pos}");
                    int worldLevel = version >= 105 ? reader.ReadInt32() : 0;
                    bool pickedUp = version >= 106 ? reader.ReadBoolean() : false;
                    if (name != "") items.Add(new Item(name, stack, durability, pos, equipped, quality, variant, crafterId, crafterName, itemData, worldLevel, pickedUp));
                }
            }
            return items;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
