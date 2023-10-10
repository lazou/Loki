using System.Collections.Generic;

namespace Loki
{
    public class PlayerStats
    {
        // For convenience until Loki eventually is updated to support new stats format
        public int Kills { get => (int)Data[PlayerStatType.EnemyKills]; set => Data[PlayerStatType.EnemyKills] = value; }
        public int Deaths { get => (int)Data[PlayerStatType.Deaths]; set => Data[PlayerStatType.Deaths] = value; }
        public int Crafts { get => (int)Data[PlayerStatType.CraftsOrUpgrades]; set => Data[PlayerStatType.CraftsOrUpgrades] = value; }
        public int Builds { get => (int)Data[PlayerStatType.Builds]; set => Data[PlayerStatType.Builds] = value; }

        public float this[PlayerStatType type]
        {
            get
            {
                return this.Data[type];
            }
            set
            {
                this.Data[type] = value;
            }
        }

        public PlayerStats()
        {
            for (int i = 0; i < (int)PlayerStatType.Count; i++)
            {
                this.Data[(PlayerStatType)i] = 0f;
            }
        }

        public Dictionary<PlayerStatType, float> Data = new Dictionary<PlayerStatType, float>();
    }
}
