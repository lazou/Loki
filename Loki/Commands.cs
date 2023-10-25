using System.Windows.Input;

namespace Loki
{
    public static class Commands
    {
        public static RoutedUICommand RevertCharacter =
            new RoutedUICommand("Revert", nameof(RevertCharacter), typeof(Commands));

        public static RoutedUICommand SaveCharacter =
            new RoutedUICommand("Save", nameof(RevertCharacter), typeof(Commands));

        public static RoutedUICommand RestoreCharacter =
            new RoutedUICommand("Restore", nameof(RestoreCharacter), typeof(Commands));

        public static RoutedUICommand RepairInventoryItems = 
            new RoutedUICommand("RepairItems", nameof(RepairInventoryItems), typeof(Commands));

        public static RoutedUICommand FillInventoryStacks =
            new RoutedUICommand("FillStacks", nameof(RepairInventoryItems), typeof(Commands));
    }
}
