using System.Drawing;
using System.Windows.Forms;

namespace TruckRemoteServer.UI.Controls
{
    //Colors of drop-down menus in the theme of the window
    public sealed class ThemedMenuRenderer : ToolStripProfessionalRenderer
    {
        public ThemedMenuRenderer(Color background, Color border, Color highlight)
            : base(new Colors(background, border, highlight))
        {
            RoundedEdges = false;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.ForeColor;
            base.OnRenderItemText(e);
        }

        private sealed class Colors : ProfessionalColorTable
        {
            private readonly Color background;
            private readonly Color border;
            private readonly Color highlight;

            public Colors(Color background, Color border, Color highlight)
            {
                this.background = background;
                this.border = border;
                this.highlight = highlight;
                UseSystemColors = false;
            }

            public override Color ToolStripDropDownBackground => background;
            public override Color ImageMarginGradientBegin => background;
            public override Color ImageMarginGradientMiddle => background;
            public override Color ImageMarginGradientEnd => background;
            public override Color MenuBorder => border;
            public override Color MenuItemBorder => highlight;
            public override Color MenuItemSelected => highlight;
            public override Color MenuItemSelectedGradientBegin => highlight;
            public override Color MenuItemSelectedGradientEnd => highlight;
            public override Color CheckBackground => highlight;
            public override Color CheckSelectedBackground => highlight;
            public override Color ButtonCheckedHighlight => highlight;
        }
    }
}
