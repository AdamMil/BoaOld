using System.Windows.Forms;

namespace Boa.IDE
{

public class AutoCompleteBox : ListBox
{ protected override void OnDoubleClick(System.EventArgs e)
  { if(SelectedIndex != -1)
    { BoaBox textbox = (BoaBox)Tag;
      textbox.SelectItem();
      Hide();
      textbox.Focus();
    }
    base.OnDoubleClick(e);
  }

  protected override void OnKeyDown(KeyEventArgs e)
  { ((Control)Tag).Focus();
    base.OnKeyDown(e);
  }

  protected override void OnSelectedIndexChanged(System.EventArgs e)
  { if(Tag!=null) ((Control)Tag).Focus();
    base.OnSelectedIndexChanged(e);
  }
}

} // namespace Boa.IDE
