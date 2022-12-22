using System.Collections.Generic;
using System.Linq;

namespace Loki
{
    public readonly struct Hair
    {
        private static readonly IEnumerable<Hair> SensibleHairs =
            ItemDb.AllItems
                .Where(i =>
                    i.ItemType == ItemType.Customization
                    && i.ItemName.ToLower().Contains("hair"))
                .Select(i =>
                    new Hair(i.DisplayName, i.ItemName));

        private static readonly IEnumerable<Hair> SillyHairs =
            ItemDb.AllItems.Where(i => i.ItemType == ItemType.Trophy)
                .Select(i => new Hair(i.DisplayName, i.ItemName));

        public static readonly Hair[] AvailableHairs = SensibleHairs.Concat(SillyHairs).ToArray();

        public string DisplayName { get; }
        public string Code { get; }

        public static Hair Default = new Hair(Loki.Properties.Resources.No_hair, "HairNone");

        public Hair(string name, string code)
        {
            DisplayName = name;
            Code = code;
        }

        public static Hair FromCode(string hairCode)
        {
            Hair hair = AvailableHairs.FirstOrDefault(b => b.Code == hairCode);
            if (string.IsNullOrEmpty(hair.Code))
                hair = Default;
            return hair;
        }
    }
}
