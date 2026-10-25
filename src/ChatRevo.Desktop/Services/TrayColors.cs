using System.Drawing;
using System.Windows.Forms;

namespace ChatRevo.Desktop.Services;

internal sealed class TrayColors : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => Color.FromArgb(24,24,23);
    public override Color ImageMarginGradientBegin => ToolStripDropDownBackground;
    public override Color ImageMarginGradientMiddle => ToolStripDropDownBackground;
    public override Color ImageMarginGradientEnd => ToolStripDropDownBackground;
    public override Color MenuItemSelected => Color.FromArgb(52,34,21);
    public override Color MenuItemBorder => Color.FromArgb(112,69,37);
    public override Color MenuBorder => Color.FromArgb(55,51,45);
}
