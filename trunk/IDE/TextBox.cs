using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Boa.AST;
using Boa.Runtime;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;

namespace Boa.IDE
{

#region BoaBox
public class BoaBox : TextEditorControl
{ public BoaBox()
  { ConvertTabsToSpaces = true;
    EnableFolding = ShowEOLMarkers = ShowInvalidLines = ShowLineNumbers = ShowSpaces = ShowTabs = ShowVRuler = false;
    TabIndent = 2;

    ActiveTextAreaControl.TextArea.DoProcessDialogKey += new DialogKeyProcessor(TextArea_DialogKey);
    ActiveTextAreaControl.TextArea.KeyEventHandler += new ICSharpCode.TextEditor.KeyEventHandler(TextArea_KeyEventHandler);

    Document.HighlightingStrategy =
      ICSharpCode.TextEditor.Document.HighlightingStrategyFactory.CreateHighlightingStrategy("Boa");
  }

  public void AppendLine(string format, params object[] args) { AppendLine(string.Format(format, args)); }
  public void AppendLine(string text)
  { int end = Document.TextLength;
    if(end!=0 && Document.GetCharAt(end-1) != '\n') Document.Insert(end++, "\n");
    ActiveTextAreaControl.TextArea.Document.Insert(end, text+"\n");
  }

  public void InsertLine(string format, params object[] args) { InsertLine(string.Format(format, args)); }
  public void InsertLine(string text)
  { Caret caret = ActiveTextAreaControl.Caret;
    int line = caret.Line+1;
    if(line==Document.TotalNumberOfLines) Document.Insert(Document.TextLength, "\n");
    caret.Position = new Point(0, line);
    ActiveTextAreaControl.TextArea.Document.Insert(caret.Offset, text+"\n");
  }

  protected AutoCompleteBox AcBox { get { return EditForm.acbox; } }
  protected Frame BoaFrame { get { return EditForm.boaFrame; } }
  protected EditForm EditForm { get { return (EditForm)ParentForm; } }
  protected ImmediateBox Immediate { get { return EditForm.immediate; } }

  #region Event handlers
  bool TextArea_DialogKey(Keys key)
  { bool alt=(key&Keys.Alt)!=0, control=(key&Keys.Control)!=0, shift=(key&Keys.Shift)!=0;
    Keys code=key&Keys.KeyCode;

    if(key==Keys.OemPeriod)
    { if(!AcBox.Visible)
      { PopulateMembers();
        if(AcBox.Items.Count!=0) ShowCompletionBox();
      }
      else
      { if(AcBox.SelectedIndex!=-1) SelectItem();
        PopulateMembers();
        if(AcBox.Items.Count==0) HideCompletionBox();
        else typed="";
      }
      return false;
    }
    else if(code==Keys.Back)
    { if(typed.Length!=0) typed = typed.Substring(0, typed.Length-1);
      int curPos = ActiveTextAreaControl.Caret.Offset;
      if(curPos>0 && Document.GetCharAt(curPos-1)=='.') HideCompletionBox();
      return false;
    }
    else if(!AcBox.Visible)
    { if(code==Keys.I && control && !alt && !shift) // ctrl-I
      { string ident = ActiveTextAreaControl.SelectionManager.SelectedText.Trim();
        if(ident=="") ident = PreviousIdentifier(false);
        if(ident!="")
        { object obj;
          if(GetObject(ident, Get.RawSlot, out obj)) Immediate.Document.Insert(0, EmbedHelpers.HelpText(obj));
          else Immediate.Document.Insert(0, "No such object.\n");
        }
        return true;
      }
      else if(code==Keys.Return && alt && !control && !shift) // Alt-enter
      { TextAreaControl txt = ActiveTextAreaControl;
        Caret caret = txt.Caret;
        string source = txt.SelectionManager.SelectedText;
        int nextline;
        if(source=="")
        { source = Document.GetText(Document.GetLineSegmentForOffset(caret.Offset)).Trim();
          ICSharpCode.TextEditor.Document.LineSegment seg = Document.GetLineSegmentForOffset(caret.Offset);
          nextline = caret.Offset==seg.Offset+seg.TotalLength ? 2 : 1;
          if(source=="") goto move;
        }
        else nextline=0;
        EditForm.Run(source, true);

        move:
        if(nextline==2) return false;

        int line = 1 + (nextline==0 ? txt.SelectionManager.GetSelectionAt(caret.Offset).EndPosition.Y : caret.Line);
        if(line==Document.TotalNumberOfLines) Document.Insert(Document.TextLength, "\n");
        caret.Position = new Point(0, line);
        txt.SelectionManager.ClearSelection();
        return true;
      }
      else if(code==Keys.Space && control && !shift && !alt) // ctrl-space
      { typed = PopulatePartial();
        if(AcBox.Items.Count==1) { AcBox.SelectedIndex=0; SelectItem(); typed=""; }
        else if(AcBox.Items.Count!=0) { ShowCompletionBox(); AcBox.SelectedIndex=0; }
        else typed="";
        return true;
      }
      else if(code==Keys.OemCloseBrackets && control && !shift && !alt) // ctrl-]
      { int index=ActiveTextAreaControl.Caret.Offset;
        if(index!=Document.TextLength)
        { char c = Document.GetCharAt(index), other;
          if(c==')' || c==']' || c=='}')
          { other = c==')' ? '(' : c==']' ? '[' : '{';
            index = Document.FormattingStrategy.SearchBracketBackward(Document, index-1, other, c);
          }
          else if(c=='(' || c=='[' || c=='{')
          { other = c=='(' ? ')' : c=='[' ? ']' : '}';
            index = Document.FormattingStrategy.SearchBracketForward(Document, index+1, c, other);
          }
          if(index != -1) ActiveTextAreaControl.Caret.Position = Document.OffsetToPosition(index);
        }
        return true;
      }
      return false;
    }
    else if((char)code=='\t' || (char)code=='\r' || (char)code=='\n') return TextArea_KeyEventHandler((char)code);
    else if(code==Keys.Up)
    { if(AcBox.SelectedIndex>0) AcBox.SelectedIndex--;
    }
    else if(code==Keys.Down)
    { if(AcBox.SelectedIndex<AcBox.Items.Count-1) AcBox.SelectedIndex++;
    }
    else if(code==Keys.PageUp)
    { int items = AcBox.ClientSize.Height / AcBox.GetItemHeight(0);
      if(AcBox.SelectedIndex>0) AcBox.SelectedIndex = Math.Max(0, AcBox.SelectedIndex-items);
    }
    else if(code==Keys.PageDown)
    { int items = AcBox.ClientSize.Height / AcBox.GetItemHeight(0);
      if(AcBox.SelectedIndex!=AcBox.Items.Count-1)
        AcBox.SelectedIndex = Math.Min(AcBox.Items.Count-1, AcBox.SelectedIndex+items);
    }
    else if(code==Keys.Home)
    { AcBox.SelectedIndex=0;
    }
    else if(code==Keys.End)
    { AcBox.SelectedIndex = AcBox.Items.Count-1;
    }
    else if(code==Keys.Escape) HideCompletionBox();
    else return false;
    
    return true;
  }

  bool TextArea_KeyEventHandler(char ch)
  { if(!AcBox.Visible || ch=='.' || ch=='\b') return false;
    else if(ch<127 && !char.IsLetterOrDigit(ch) && ch!='_')
    { bool handled=false;
      if(AcBox.SelectedIndex!=-1) SelectItem();
      if(ch=='\n' || ch=='\r' || ch=='\t') handled=true;
      HideCompletionBox();
      return handled;
    }
    else if(ch>32 && ch<127)
    { typed += ch;
      int index = AcBox.FindString(typed);
      if(index!=ListBox.NoMatches) AcBox.SelectedIndex = index;
    }
    return false;
  }
  #endregion

  enum Get { Normal, IgnoreLast, RawSlot }

  object GetObject(string ident) { return GetObject(ident, Get.Normal); }

  object GetObject(string ident, Get type)
  { object ret;
    return GetObject(ident, type, out ret) ? ret : null;
  }

  bool GetObject(string ident, Get type, out object ret)
  { ret=null;
    if(ident==null || ident=="") return false;
    string[] bits = ident.Split('.');
    try
    { object obj=BoaFrame.Module;
      for(int i=0,len=bits.Length-(type==Get.Normal ? 0 : 1); i<len; i++)
        if(!Ops.GetAttr(obj, bits[i], out obj) || obj==null) return false;
      if(obj!=null && type==Get.RawSlot)
      { obj=Ops.GetRawAttr(obj, bits[bits.Length-1]);
        if(obj==Ops.Missing) obj=null;
      }
      ret=obj;
    }
    catch { return false; }
    return true;
  }

  void HideCompletionBox()
  { AcBox.Hide();
    typed="";
  }

  void PopulateMembers()
  { AutoCompleteBox acbox = AcBox;
    acbox.Items.Clear();
    object obj = GetObject(PreviousIdentifier(true));
    if(obj!=null) foreach(string s in Modules.__builtin__.dir(obj)) acbox.Items.Add(new AutoCompleteItem(obj, s));
  }

  string PopulatePartial()
  { AutoCompleteBox acbox = AcBox;
    acbox.Items.Clear();
    string ident = PreviousIdentifier(false);
    object obj = GetObject(ident, Get.IgnoreLast);
    if(obj!=null)
    { int index=ident.LastIndexOf('.');
      if(index!=-1) ident = ident.Substring(index+1);
      foreach(string s in Modules.__builtin__.dir(obj))
        if(string.Compare(s, 0, ident, 0, ident.Length, true)==0) acbox.Items.Add(new AutoCompleteItem(obj, s));
    }
    return ident;
  }

  string PreviousIdentifier(bool trimEnd)
  { int pos=ActiveTextAreaControl.Caret.Offset, end=pos;
    char c;
    while(--pos>=0 && (char.IsLetterOrDigit(c=Document.GetCharAt(pos)) || c=='.' || c=='_'));
    string word = end<=0 ? "" : Document.GetText(pos+1, end-pos-1);
    if(trimEnd) word = word.TrimEnd('.');
    return word;
  }

  internal void SelectItem()
  { string item = AcBox.GetItemText(AcBox.SelectedItem);
    Caret caret = ActiveTextAreaControl.Caret;
    if(typed.Length==0) Document.Insert(caret.Offset, item);
    else Document.Replace(caret.Offset-typed.Length, typed.Length, item);
    caret.Column += item.Length-typed.Length;
  }

  void ShowCompletionBox()
  { AcBox.Tag = this;
    Point cpt = ActiveTextAreaControl.Caret.ScreenPosition;
    int y = cpt.Y, xoff=0, yoff=0;
    Control ctl=this, form=ParentForm;
    while(ctl != form) { xoff += ctl.Left; yoff += ctl.Top; ctl=ctl.Parent; }

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

  string typed="";
}
#endregion

public class ImmediateBox : BoaBox
{ public ImmediateBox()
  { displayhook = Ops.GenerateFunction("display", new Parameter[] { new Parameter("value") },
                                       new CallTargetN(display));
    invoke = new StringDelegate(InsertLine);
  }

  delegate void StringDelegate(string text);

  object display(params object[] values)
  { if(values[0]!=null)
    { Invoke(invoke, new object[] { "Result: "+Ops.Repr(values[0]) });
      Modules.__builtin__._ = values[0];
    }
    return null;
  }

  internal object displayhook;
  StringDelegate invoke;
}

}