using JetBrains.Annotations;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace Loki
{
    // Note: This could have been skipped and ObservableCollection<PlayerStatData> could have been used in
    // PlayerProfile (property Stats) in a similar way ObservableCollection<Skill> is used in Player (property Skills).
    //
    // (In the game, PlayerStats is a separate class with a local Dictionary. Works in Loki but not if stats
    // should be editable. This was my choice of compromise between keeping the read/write code close to original
    // but also being able to bind to UI with edit possibilies).
    public class PlayerStats : ObservableCollection<PlayerStatData>
    {
        // Indexer based on enum PlayerStatType and will get/set the Value property of PlayerStat class
        public float this[PlayerStatType type]
        {
            get
            {
                return this[(int)type].Value;
            }
            set
            {
                this[(int)type].Value = value;
            }
        }

        private static IEnumerable<PlayerStatData> Empty()
        {
            return Enumerable.Range(0, (int)PlayerStatType.Count).Select(i => new PlayerStatData((PlayerStatType)i, 0f));
        }

        public PlayerStats() : base(Empty())
        {
            // Empty constructor will populate with a full collection of PlayerStat's based on value of enum PlayerStatType
        }
    }   
}
