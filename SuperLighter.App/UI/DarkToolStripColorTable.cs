namespace SuperLighter.App.UI;

internal sealed class DarkToolStripColorTable : ProfessionalColorTable
{
    private static readonly Color Background = Color.FromArgb(30, 34, 42);
    private static readonly Color Border = Color.FromArgb(54, 61, 74);
    private static readonly Color Selection = Color.FromArgb(48, 75, 112);

    public override Color ToolStripDropDownBackground => Background;
    public override Color ImageMarginGradientBegin => Background;
    public override Color ImageMarginGradientMiddle => Background;
    public override Color ImageMarginGradientEnd => Background;
    public override Color MenuBorder => Border;
    public override Color MenuItemBorder => Color.FromArgb(70, 115, 174);
    public override Color MenuItemSelected => Selection;
    public override Color MenuItemSelectedGradientBegin => Selection;
    public override Color MenuItemSelectedGradientEnd => Selection;
    public override Color MenuItemPressedGradientBegin => Selection;
    public override Color MenuItemPressedGradientMiddle => Selection;
    public override Color MenuItemPressedGradientEnd => Selection;
    public override Color SeparatorDark => Border;
    public override Color SeparatorLight => Border;
}
