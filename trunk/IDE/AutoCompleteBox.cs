using System.Windows.Forms;

namespace Boa.IDE
{

public class AutoCompleteBox : ListBox
{ protected override void OnDoubleClick(System.EventArgs e)
  { if(SelectedIndex != -1)
    { TextBox textbox = (TextBox)Tag;
      textbox.SelectItem();
      Hide();
      textbox.Focus();
    }
    base.OnDoubleClick(e);
  }

  protected override void OnKeyDown(KeyEventArgs e)
  { ((TextBox)Tag).Focus();
    base.OnKeyDown(e);
  }

  protected override void OnSelectedIndexChanged(System.EventArgs e)
  { if(Tag!=null) ((TextBox)Tag).Focus();
    base.OnSelectedIndexChanged(e);
  }
}

} // namespace Boa.IDE
