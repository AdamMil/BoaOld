using System;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Boa.IDE
{

// TODO: improve support for variable-width fonts

[Flags]
public enum TextEditorScrollBars
{ None=0, Vertical=1, Horizontal=2, Both=Vertical|Horizontal, Forced=4,
  ForcedVertical=Vertical|Forced, ForcedHorizontal=Horizontal|Forced, ForcedBoth=Both|Forced
}

public class TextEditor : Control
{ public TextEditor()
  { ForeColor=SystemColors.WindowText; BackColor=SystemColors.Window;
    vbar.Dock = DockStyle.Right;
    hbar.Dock = DockStyle.Bottom;

    ptab.LineCountChanged += new EventHandler(ptab_LineCountChanged);
  }

  public bool AcceptsTab
  { get { return acceptsTab; }
    set
    { if(value!=acceptsTab)
      { acceptsTab=value;
        OnAcceptsTabChanged(EventArgs.Empty);
      }
    }
  }

  public BorderStyle BorderStyle
  { get { return borderStyle; }
    set
    { if(value!=borderStyle)
      { borderStyle=value;
        OnBorderStyleChanged(EventArgs.Empty);
        Invalidate(false);
      }
    }
  }

  public bool CanRedo
  { get { throw new NotImplementedException(); }
  }

  public bool CanUndo
  { get { throw new NotImplementedException(); }
  }

  public CharacterCasing CharacterCasing
  { get { return casing; }
    set { casing=value; }
  }

  public bool CreateUndos
  { get { return createUndos; }
    set { createUndos=value; }
  }

  public bool HideSelection
  { get { return hideSelection; }
    set
    { if(hideSelection!=value)
      { hideSelection=value;
        OnHandleCreated(EventArgs.Empty);
      }
    }
  }

  public string[] Lines
  { get { return ptab.Lines; }
    set { ptab.Lines=value; }
  }

  public int MaxLength
  { get { return maxLength; }
    set
    { if(maxLength<0) throw new ArgumentException("Value cannot be negative.", "MaxLength");
      maxLength=value;
    }
  }

  public bool Modified
  { get { return modified; }
    set
    { if(modified!=value)
      { modified=value;
        OnModifiedChanged(EventArgs.Empty);
      }
    }
  }

  public bool ReadOnly
  { get { return readOnly; }
    set
    { if(readOnly!=value)
      { readOnly=value;
        OnReadOnlyChanged(EventArgs.Empty);
      }
    }
  }

  public TextEditorScrollBars ScrollBars
  { get { return scrollBars; }
    set { throw new NotImplementedException(); }
  }

  public string SelectedText
  { get
    { if(selLength==0) return "";
      return Text.Substring(selStart, selLength);
    }
    set
    { ptab.Replace(selStart, selLength, value);
      selLength = value.Length;
    }
  }

  public Color SelectionColor
  { get { throw new NotImplementedException(); }
    set { throw new NotImplementedException(); }
  }

  public Font SelectionFont
  { get { throw new NotImplementedException(); }
    set { throw new NotImplementedException(); }
  }

  public int SelectionLength
  { get { return selLength; }
    set { Select(selStart, value); }
  }
  public int SelectionStart
  { get { return selStart; }
    set { Select(value, selLength); }
  }

  public bool SelectionProtected
  { get { throw new NotImplementedException(); }
    set { throw new NotImplementedException(); }
  }

  public override string Text
  { get { return ptab.Text; }
    set
    { if(ptab.Text!=value)
      { ptab.Text = value;
        OnTextChanged(EventArgs.Empty);
      }
    }
  }

  public int TextLength { get { return ptab.TextLength; } }

  public TextRenderingHint TextRenderingHint
  { get { return textRender; }
    set
    { if(textRender!=value)
      { textRender=value;
        Invalidate(false);
      }
    }
  }

  public bool WordWrap
  { get { return wordWrap; }
    set
    { if(wordWrap!=value)
      { wordWrap=value;
        // TODO: recalculate scroll bars
        Invalidate(false);
      }
    }
  }

  public void AppendText(string text) { ptab.Append(text); }
  public void Clear() { ptab.Clear(); }

  public void ClearAllUndos() { throw new NotImplementedException(); }
  public void ClearUndo() { throw new NotImplementedException(); }

  public void Copy()
  { if(selLength==0)
    { throw new NotImplementedException("Selecting a whole line is not implemented.");
    }
    else Clipboard.SetDataObject(SelectedText, true);
  }

  public bool CreateUndo() { throw new NotImplementedException(); }

  public void Cut()
  { if(selLength==0)
    { throw new NotImplementedException("Cutting a whole line is not implemented.");
    }
    else
    { Clipboard.SetDataObject(SelectedText, true);
      SelectedText = "";
    }
  }

  public char GetCharFromPosition(Point point) { throw new NotImplementedException(); }
  public int GetCharIndexFromPosition(Point point) { throw new NotImplementedException(); }
  public int GetLineFromCharIndex(int index) { return ptab.GetLineFromCharIndex(index); }
  public int GetCharIndexFromLine(int line) { return ptab.GetCharIndexFromLine(line); }
  public Point GetPositionFromCharIndex(int index) { throw new NotImplementedException(); }

  public void InsertText(int position, string text) { ptab.Insert(position, text); }

  public void Paste()
  { IDataObject obj = Clipboard.GetDataObject();
    if(obj==null) return;
    string text = (string)obj.GetData(typeof(string));
    if(text==null) return;
    if(selLength==0) InsertText(SelectionStart, text);
    else SelectedText = text;
  }

  public void Redo() { throw new NotImplementedException(); }
  public void ScrollToCaret() { throw new NotImplementedException(); }

  public void Select(int position, int length)
  { if(position<0 || length<0) throw new ArgumentOutOfRangeException();
    selStart  = Math.Min(position, ptab.TextLength);
    selLength = Math.Min(length, ptab.TextLength-selStart);
  }

  public void SelectAll() { Select(0, ptab.TextLength); }

  public void Undo() { throw new NotImplementedException(); }

  public event EventHandler AcceptsTabChanged, BorderStyleChanged, HideSelectionChanged, ModifiedChanged,
                            ReadOnlyChanged;
  //public event SomeEventHandler Protected;

  protected override bool IsInputKey(Keys keyData)
  { switch(keyData)
    { case Keys.Insert: case Keys.Delete: case Keys.Home: case Keys.End: case Keys.PageUp: case Keys.PageDown:
      case Keys.Up: case Keys.Down: case Keys.Left: case Keys.Right: case Keys.Enter: case Keys.Escape:
        return true;
      case Keys.Back: return !readOnly;
      case Keys.Tab: return acceptsTab && !readOnly;
      default: return base.IsInputKey(keyData); 
    }
  }

  #region Event handlers
  void ptab_LineCountChanged(object sender, EventArgs e)
  { vbar.Maximum = ptab.LineCount;
    RecalcScrollbars();
  }
  #endregion

  #region Event overrides
  protected override void OnPaint(PaintEventArgs e)
  { base.OnPaint(e);
  }

  protected override void OnPaintBackground(PaintEventArgs e)
  { base.OnPaintBackground(e);
    if(borderStyle==BorderStyle.FixedSingle)
      ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
                              Enabled ? SystemColors.ActiveBorder : SystemColors.InactiveBorder,
                              ButtonBorderStyle.Solid);
    else if(borderStyle==BorderStyle.Fixed3D)
      ControlPaint.DrawBorder3D(e.Graphics, ClientRectangle, Border3DStyle.Sunken);
  }

  #endregion

  #region Event raisers
  protected virtual void OnAcceptsTabChanged(EventArgs e)
  { if(AcceptsTabChanged!=null) AcceptsTabChanged(this, e);
  }

  protected virtual void OnBorderStyleChanged(EventArgs e)
  { if(BorderStyleChanged!=null) BorderStyleChanged(this, e);
  }

  protected virtual void OnHideSelectionChanged(EventArgs e)
  { if(HideSelectionChanged!=null) HideSelectionChanged(this, e);
  }

  protected virtual void OnModifiedChanged(EventArgs e)
  { if(ModifiedChanged!=null) ModifiedChanged(this, e);
  }

  //protected virtual void OnProtected(SomeEventArgs e);

  protected virtual void OnReadOnlyChanged(EventArgs e)
  { if(ReadOnlyChanged!=null) ReadOnlyChanged(this, e);
  }
  #endregion

  int MaxVisibleLines { get { return ClientSize.Height/Font.Height; } }

  void RecalcScrollbars()
  { if((scrollBars&TextEditorScrollBars.Vertical)!=0)
      vbar.Visible = (scrollBars&TextEditorScrollBars.Forced)==0 ? vbar.Maximum>MaxVisibleLines : true;
    if((scrollBars&TextEditorScrollBars.Horizontal)!=0)
      hbar.Visible = (scrollBars&TextEditorScrollBars.Forced)==0 ? false : true;
  }

  PieceTable ptab=new PieceTable();
  ScrollBar hbar=new HScrollBar(), vbar=new VScrollBar();
  int selStart, selLength, maxLength;
  CharacterCasing casing=CharacterCasing.Normal;
  BorderStyle borderStyle=BorderStyle.Fixed3D;
  TextEditorScrollBars scrollBars=TextEditorScrollBars.Both;
  TextRenderingHint textRender;
  bool acceptsTab, createUndos=true, hideSelection=true, modified, readOnly, wordWrap=true;
}

} // namespace Boa.IDE
