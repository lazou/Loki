using System.Collections.Generic;
using System.Linq;

namespace Loki
{
    public readonly struct Beard
    {
        private static readonly IEnumerable<Beard> SensibleBeards = 
            ItemDb.AllItems
                .Where(i => 
                    i.ItemType == ItemType.Customization 
                    && i.ItemName.ToLower().Contains("beard"))
                .Select(i => 
                    new Beard(i.DisplayName, i.ItemName));

        private static readonly IEnumerable<Beard> SillyBeards = 
            ItemDb.AllItems.Where(i => i.ItemType == ItemType.Trophy)
            .Select(i => new Beard(i.DisplayName, i.ItemName));

        public static Beard[] AvailableBeards = SensibleBeards.Concat(SillyBeards).ToArray();

        public string DisplayName { get; }
        public string Code { get; }

        public static Beard Default = new Beard(Loki.Properties.Resources.B_No_beard, "BeardNone");

        public Beard(string name, string code)
        {
            DisplayName = name;
            Code = code;
        }

        public static Beard FromCode(string beardCode)
        {
            Beard beard = AvailableBeards.FirstOrDefault(b => b.Code == beardCode);
            if (string.IsNullOrEmpty(beard.Code)) 
                beard = Default;
            return beard;
        }
    }
}
