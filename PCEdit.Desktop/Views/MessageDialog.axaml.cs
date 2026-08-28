using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;

namespace PCEdit.Desktop.Views;

public partial class MessageDialog : Window
{
    private readonly TextBlock _titleText;
    private readonly TextBlock _messageText;
    private readonly StackPanel _buttonBar;
    private int _result = -1;

    public MessageDialog()
    {
        AvaloniaXamlLoader.Load(this);
        _titleText = this.FindControl<TextBlock>("TitleText")!;
        _messageText = this.FindControl<TextBlock>("MessageText")!;
        _buttonBar = this.FindControl<StackPanel>("ButtonBar")!;
    }

    public MessageDialog(string title, string message, IReadOnlyList<string> buttons)
        : this()
    {
        Title = title;
        _titleText.Text = title;
        _messageText.Text = message;

        for (var i = 0; i < buttons.Count; i++)
        {
            var index = i;
            var isPrimary = i == buttons.Count - 1;
            var button = new Button
            {
                Content = buttons[i],
                MinWidth = 88,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                IsDefault = isPrimary,
                IsCancel = i == 0 && buttons.Count > 1,
            };
            if (isPrimary)
            {
                button.Classes.Add("accent");
            }

            button.Click += (_, _) =>
            {
                _result = index;
                Close();
            };
            _buttonBar.Children.Add(button);
        }
    }

    /// <summary>Shows the dialog modally and returns the index of the clicked button (-1 if dismissed).</summary>
    public async Task<int> ShowAsync(Window owner)
    {
        await ShowDialog(owner);
        return _result;
    }
}
