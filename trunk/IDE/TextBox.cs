using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Boa.AST;
using Boa.Runtime;

namespace Boa.IDE
{

#region BoaBox
public class BoaBox : RichTextBox
{ public BoaBox() { AcceptsTab=true; DetectUrls=false; WordWrap=false; }

  public void PerformSyntaxHighlighting()
  { if((DateTime.Now-lastUserChange).TotalSeconds < 5) return;

    if(!redoHighlight) return;
    redoHighlight=false;

    bool old=myChange; myChange=true;
    int start=Math.Max(SelectionStart, 0), len=Math.Max(SelectionLength, 0);

    for(Match m=highlightre.Match(Text); m.Success; m=m.NextMatch())
    { Highlight type;
      string value = m.Value;
      char c = value[0];
      if(char.IsLetter(c) || c=='_') type=Highlight.Identifier;
      else if(char.IsDigit(c)) type=Highlight.Number;
      else if(c=='#' || value.StartsWith("/*")) type = Highlight.Comment;
      else type = Highlight.Operator;

      Color newColor = HighlightColor(Classify(value, type));
      SelectionStart  = m.Index;
      SelectionLength = m.Length;
      if(SelectionColor!=newColor) SelectionColor = newColor;
    }

    SelectionLength = 0;
    SelectionColor  = Color.Black;
    SelectionStart  = start;
    SelectionLength = len;
    
    myChange=old;
  }

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

  protected internal void AppendLine(string format, params object[] args) { AppendLine(string.Format(format, args)); }
  protected internal void AppendLine(string line)
  { line += "\n";
    string text = Text + (Text.EndsWith("\n") ? line : "\n"+line);
    SetText(text, text.Length);
  }

  protected void InsertText(string text)
  { int start=SelectionStart, end=start+SelectionLength;
    SetText(Text=Text.Substring(0, start) + text + Text.Substring(end, TextLength-end), start+text.Length);
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
      bool nextline;
      if(code=="")
      { string[] lines = Lines;
        if(lines.Length==0) goto done;
        code = lines[GetLineFromCharIndex(SelectionStart)].Trim();
        nextline = true;
        if(code=="") goto move;
      }
      else nextline = false;
      EditForm.Run(code, true);

      move:
      if(nextline) MoveToNextLine();
      else
      { SelectionStart  = SelectionStart+SelectionLength;
        SelectionLength = 0;
      }
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

          if(cpt.Y+AcBox.Height > Parent.ClientSize.Height)
          { y = yoff+y-AcBox.Height+2;
            if(y>=0) cpt.Y=y;
          }

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
      if(curPos>0 && Text[curPos-1]=='.') AcBox.Hide();
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

    if(!AcBox.Visible || e.KeyChar=='.' || e.KeyChar=='\b') goto done;
    else if(e.KeyChar<127 && !char.IsLetterOrDigit(e.KeyChar) && e.KeyChar!='_')
    { if(e.KeyChar=='(' || e.KeyChar=='[' || e.KeyChar=='\n' || e.KeyChar=='\r' || e.KeyChar=='\t' || e.KeyChar==' ')
      { if(AcBox.SelectedIndex!=-1) SelectItem();
        if((int)e.KeyChar<32) e.Handled = true;
      }
      AcBox.Hide();
      typed = "";
    }
    else if(e.KeyChar>32 && e.KeyChar<127)
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

  protected override void OnTextChanged(EventArgs e)
  { base.OnTextChanged(e);
    if(myChange) return;

    Modified = true;
    lastUserChange = DateTime.Now;

    int pos=SelectionStart, end;
    if(pos<=0) { redoHighlight=true; return; }

    char c = Text[--pos];
    if(char.IsLetterOrDigit(c)) return;

    string text=Text;

    while(pos>=0 && char.IsWhiteSpace(text[pos])) pos--;
    if(pos<0) return;
    end=pos+1;

    Highlight type;
    if(char.IsLetter(c=text[pos]) || c=='_')
    { while(--pos>=0 && (char.IsLetter(c=text[pos]) || c=='_'));
      type = Highlight.Identifier;
    }
    else if(char.IsNumber(text[pos])) // TODO: improve number recognition
    { while(--pos>=0 && (char.IsDigit(c=text[pos]) || c=='.'));
      type = Highlight.Number;
    }
    else if(char.IsPunctuation(c=text[pos]) || char.IsSymbol(c)) // TODO: improve symbol recognition
    { while(--pos>=0 && (char.IsPunctuation(c=text[pos]) || char.IsSymbol(c)));
      type = Highlight.Operator;
    }
    else return;

    pos++;
    string word = end<=0 ? "" : text.Substring(pos, end-pos);
    if(word=="") return;

    Color newColor = HighlightColor(Classify(word, type));

    int start=SelectionStart, len=SelectionLength;
    SelectionStart  = pos;
    SelectionLength = end-pos;
    if(SelectionColor!=newColor) SelectionColor = newColor;

    SelectionStart  = start;
    SelectionLength = 0;
    SelectionColor  = Color.Black;
    SelectionLength = len;
  }
  #endregion

  enum Highlight
  { Identifier, Keyword, CallKeyword, DeclareKeyword, Operator, Punctuation, Number, Comment
  };

  Highlight Classify(string word, Highlight type)
  { if(type==Highlight.Identifier)
      switch(word)
      { case "while": case "return": case "from": case "for": case "if": case "elif": case "else": case "break":
        case "continue": case "in": case "try": case "except": case "finally": case "is":
        case "not": case "and": case "or":
          type=Highlight.Keyword; break;

        case "import": case "pass": case "global": case "raise": case "assert": case "del": case "yield":
        case "exec":
          type=Highlight.CallKeyword; break;

        case "def": case "lambda": case "class":
          type=Highlight.DeclareKeyword; break;
      }
    else if(type==Highlight.Operator)
      switch(word[0])
      { case '(': case ')': case '[': case ']': case '{': case '}': case ':': case ',':
          type=Highlight.Punctuation; break;
      }
    return type;
  }

  Color HighlightColor(Highlight type)
  { switch(type)
    { case Highlight.Operator: return Color.OrangeRed;
      case Highlight.CallKeyword: case Highlight.Keyword: return Color.Blue;
      case Highlight.Number: return Color.DarkRed;
      case Highlight.Comment: return Color.Green;
      case Highlight.DeclareKeyword: return Color.DarkCyan;
      default: return Color.Black;
    }
  }

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
    SetText(Text.Substring(0, prefix) + item + Text.Substring(suffix, TextLength-suffix), prefix+item.Length);
  }
  
  void SetText(string text, int newCaretPos)
  { bool old=myChange; myChange=true;
    Text = text;
    SelectAll();
    SelectionColor  = Color.Black;
    SelectionLength = 0;
    SelectionStart  = newCaretPos;
  }

  string typed="";
  DateTime lastUserChange;
  bool redoHighlight=true, myChange;
  
  static Regex highlightre = new Regex(@"/\*.*?\*/|#.*?(?:\n|$)|\w+|\d+(?:\.\d+)?|<>|(?:&&|\|\||[~^&*-=+<>/?|])=?",
                                       RegexOptions.Compiled|RegexOptions.Singleline);
}
#endregion

public class ImmediateBox : BoaBox
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