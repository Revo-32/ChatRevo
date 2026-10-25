using System.Windows.Controls;
using System.Windows.Media;
using System.Text.RegularExpressions;

namespace ChatRevo.Desktop.Views;

public partial class ColorWindow : Window
{
    public string SelectedColor { get; private set; }
    public ColorWindow(string current)
    {
        SelectedColor=current;
        InitializeComponent();
        foreach(var color in new[]{"#000000","#FFFFFF","#FFD09A","#FFF090","#A7D88A","#A9C8FF"})
        {
            var swatch=new Button { Width=42,Height=32,Margin=new Thickness(0,0,8,0),Background=(Brush)new BrushConverter().ConvertFromString(color)!,ToolTip=color,Padding=new Thickness(0) };
            swatch.Click+=(_,_)=>Hex.Text=color; Swatches.Children.Add(swatch);
        }
        Hex.Text=current;
    }
    private void HexChanged(object sender,TextChangedEventArgs e)
    {
        if (Apply is null) return;
        var valid=Regex.IsMatch(Hex.Text,"^#[0-9a-fA-F]{6}$");
        Apply.IsEnabled=valid; Validation.Text=valid?"":"#RRGGBB 형식으로 입력하세요.";
        if(valid) { SelectedColor=Hex.Text.ToUpperInvariant(); ColorPreview.Background=(Brush)new BrushConverter().ConvertFromString(SelectedColor)!; }
    }
    private void ApplyClick(object sender,RoutedEventArgs e) => DialogResult=true;
    private void CancelClick(object sender,RoutedEventArgs e) => DialogResult=false;
}
