using System;
using System.Drawing;
using System.Windows.Forms;
using Boa.AST;
using Boa.Runtime;

namespace Boa.IDE
{

#region TextBox
public class TextBox : RichTextBox
{ public TextBox() { AcceptsTab=true; WordWrap=false; }

  protected AutoCompleteBox AcBox { get { return EditForm.AcBox; } }

  protected bool AtEndOfLine
  { get { return GetLineFromCharIndex(SelectionStart) != GetLineFromCharIndex(SelectionStart+1); }
  }
  
  protected Frame BoaFrame { get { return EditForm.boaFrame; } }

  protected EditForm EditForm
  { get
    { Control ctl = Parent;
      while(!(ctl is EditForm)) ctl = ctl.Parent;
      return (EditForm)ctl;
    }
  }

  protected ImmediateBox Immediate { get { return EditForm.immediate; } }

  protected internal void AppendLine(string format, params object[] args) { AppendLine(string.Format(format, args)); }
  protected internal void AppendLine(string line)
  { line += "\n";
    Text += Text.EndsWith("\n") ? line : "\n"+line;
    SelectionStart  = TextLength;
    SelectionLength = 0;
  }

  protected void InsertText(string text)
  { int start=SelectionStart, end=start+SelectionLength;
    Text = Text.Substring(0, start) + text + Text.Substring(end, TextLength-end);
    SelectionStart  = start + text.Length;
    SelectionLength = 0;
  }

  protected void MoveToNextLine()
  { int line = GetLineFromCharIndex(SelectionStart), len=0;
    string[] lines = Lines;
    if(line==lines.Length-1) { Text += "\n"; SelectionStart=TextLength; }
    else
    { for(int i=0; i<=line; i++) len += lines[i].Length+1;
      SelectionStart = Math.Min(len, TextLength);
    }
    SelectionLength = 0;
  }

  #region Event overrides
  protected override void OnKeyDown(KeyEventArgs e)
  { if(e.Handled) goto done;
    else if(e.KeyCode==Keys.Return && e.Alt && !e.Control && !e.Shift) // Alt-enter
    { string code = SelectedText;
      if(code=="")
      { string[] lines = Lines;
        if(lines.Length==0) goto done;
        code = lines[GetLineFromCharIndex(SelectionStart)].Trim();
        MoveToNextLine();
        if(code=="") goto done;
      }
      else if(code.Trim().Length==0) goto done;

      Options.Interactive = true;
      try
      { Statement stmt = Parser.FromString(code).Parse();
        stmt.PostProcessForCompile();
        SnippetMaker.Generate(stmt).Run(BoaFrame);
      }
      catch(Exception ex) { Immediate.AppendLine("Error {0}: {1}", ex.GetType().Name, ex.Message); }
    }
    else if(e.KeyData==Keys.OemPeriod)
    { if(!AcBox.Visible)
      { AcBox.Tag = this;
        PopulateBox();

        if(AcBox.Items.Count!=0)
        { Point cpt = GetPositionFromCharIndex(SelectionStart);
          int y = cpt.Y, xoff=0, yoff=0;
          Control ctl = this;
          while(ctl != AcBox.Parent) { xoff += ctl.Left; yoff += ctl.Top; ctl=ctl.Parent; }

          cpt.X += xoff+2;
          cpt.Y += yoff+(int)Math.Ceiling(Font.GetHeight())+2;
          if(cpt.Y+AcBox.Height > Parent.ClientSize.Height) cpt.Y = yoff + y - AcBox.Height - 2;

          AcBox.Location = cpt;
          AcBox.BringToFront();
          AcBox.Show();
        }
      }
      else
      { if(AcBox.SelectedIndex!=-1) SelectItem();
        PopulateBox();
        if(AcBox.Items.Count==0) AcBox.Hide();
      }
      typed = "";
    }
    else if(e.KeyCode==Keys.Back)
    { if(typed.Length!=0) typed = typed.Substring(0, typed.Length-1);
      int curPos = SelectionStart;
      if(curPos>0 && Text.Substring(curPos-1, 1)==".") AcBox.Hide();
    }
    else if(!AcBox.Visible) goto done;
    else if(e.KeyCode==Keys.Up)
    { if(AcBox.SelectedIndex>0) AcBox.SelectedIndex--;
      e.Handled = true;
    }
    else if(e.KeyCode==Keys.Down)
    { if(AcBox.SelectedIndex<AcBox.Items.Count-1) AcBox.SelectedIndex++;
      e.Handled = true;
    }
    else if(e.KeyCode==Keys.PageUp)
    { int items = AcBox.ClientSize.Height / AcBox.GetItemHeight(0);
      if(AcBox.SelectedIndex>0) AcBox.SelectedIndex = Math.Max(0, AcBox.SelectedIndex-items);
    }
    else if(e.KeyCode==Keys.PageDown)
    { int items = AcBox.ClientSize.Height / AcBox.GetItemHeight(0);
      if(AcBox.SelectedIndex!=AcBox.Items.Count-1)
        AcBox.SelectedIndex = Math.Min(AcBox.Items.Count-1, AcBox.SelectedIndex+items);
    }
    else if(e.KeyCode==Keys.Home)
    { AcBox.SelectedIndex=0;
      e.Handled = true;
    }
    else if(e.KeyCode==Keys.End)
    { AcBox.SelectedIndex = AcBox.Items.Count-1;
      e.Handled = true;
    }

    done: base.OnKeyDown(e);
  }

  protected override void OnKeyPress(KeyPressEventArgs e)
  { if(e.Handled) goto done;

    if(e.KeyChar=='.' || e.KeyChar=='\b') goto done;
    else if(e.KeyChar<127 && !char.IsLetterOrDigit(e.KeyChar) && e.KeyChar!='_')
    { if(AcBox.Visible)
      { if(e.KeyChar=='(' || e.KeyChar=='[' || e.KeyChar=='\n' || e.KeyChar=='\r' || e.KeyChar=='\t' || e.KeyChar==' ')
        { if(AcBox.SelectedIndex!=-1) SelectItem();
          if((int)e.KeyChar<32) e.Handled = true;
        }
        AcBox.Hide();
        typed = "";
      }
    }
    else if(AcBox.Visible && e.KeyChar>32 && e.KeyChar<127)
    { typed += e.KeyChar;
      int index = AcBox.FindString(typed);
      if(index!=ListBox.NoMatches) AcBox.SelectedIndex = index;
    }

    done: base.OnKeyPress(e);
  }

  protected override void OnMouseDown(MouseEventArgs e)
  { AcBox.Hide();
    base.OnMouseDown(e);
  }
  #endregion

  void PopulateBox()
  { AcBox.Items.Clear();

    string word = PreviousIdentifier();
    if(word=="") return;

    string[] bits = word.Split('.');
    
    try
    { object obj=BoaFrame.Module;
      for(int i=0; i<bits.Length; i++) if(!Ops.GetAttr(obj, bits[i], out obj) || obj==null) return;
      foreach(string s in Modules.__builtin__.dir(obj)) AcBox.Items.Add(s);
    }
    catch { }
  }

  string PreviousIdentifier()
  { string text=Text;
    int pos=SelectionStart, end=pos;
    char c;
    while(--pos>=0 && (char.IsLetterOrDigit(c=text[pos]) || c=='.' || c=='_'));
    string word = end<=0 ? "" : text.Substring(pos+1, end-pos-1);
    word.TrimEnd('.');
    return word;
  }

  internal void SelectItem()
  { int start=SelectionStart, prefix=start-typed.Length, suffix=Math.Min(start+typed.Length, TextLength);
    string item = AcBox.GetItemText(AcBox.SelectedItem);
    Text = Text.Substring(0, prefix) + item + Text.Substring(suffix, TextLength-suffix);
    SelectionStart = prefix + item.Length;
  }

  string typed="";
}
#endregion

public class ImmediateBox : TextBox
{ public ImmediateBox()
  { displayhook = Ops.GenerateFunction("display", new Parameter[] { new Parameter("value") },
                                       new CallTargetN(display));
  }

  object display(params object[] values) // TODO: optimize this and use CallTarget1 or something
  { if(values[0]!=null)
    { AppendLine("Result: "+Ops.Repr(values[0]));
      Modules.__builtin__._ = values[0];
    }
    return null;
  }

  internal object displayhook;
}

}