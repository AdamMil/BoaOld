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
    ExamineFont();
    RecalcScrollbars();
  }
  ~TextEditor() { if(caretControl==this) caretControl=null; }

  public bool AcceptsTab
  { get { return acceptsTab; }
    set
    { if(value!=acceptsTab)
      { acceptsTab=value;
        OnAcceptsTabChanged(EventArgs.Empty);
      }
    }
  }

  public bool AntiAliased
  { get { return antiAliased; }
    set
    { if(antiAliased!=value)
      { antiAliased = value;
        Invalidate(false);
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
    set
    { if(scrollBars!=value)
      { scrollBars = value;
        RecalcScrollbars();
      }
    }
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
  { get { return Math.Abs(selLength); }
    set { Select(selStart, value); }
  }
  public int SelectionStart
  { get { return selLength<0 ? selStart-selLength : selStart; }
    set { Select(value, SelectionLength); }
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
  { RecalcScrollbars();
  }
  #endregion

  #region Event overrides
  protected override void Dispose(bool disposing)
  { if(caretControl==this) caretControl=null;
    if(!disposing) GC.SuppressFinalize(this);
    base.Dispose(disposing);
  }

  protected override void OnGotFocus(EventArgs e)
  { base.OnGotFocus(e);
    caretControl = this;
  }

  protected override void OnFontChanged(EventArgs e)
  { base.OnFontChanged(e);
    ExamineFont();
    RecalcScrollbars();
    Invalidate(false);
  }

  protected override void OnKeyPress(KeyPressEventArgs e)
  { if(!e.Handled)
    { if(e.KeyChar==(char)27) { selLength=0; } // escape
      else if(readOnly || e.KeyChar<32 && (e.KeyChar!='\t' || !acceptsTab)) goto done;
      else if(e.KeyChar=='\r' || e.KeyChar=='\n')
      { if(selLength==0) ptab.Insert(selStart++, '\n');
        else SelectedText = "\n";
      }
      else if(e.KeyChar=='\b')
      { if(selLength==0)
        { if(selStart!=0) ptab.Delete(--selStart);
        }
        else SelectedText = "";
      }
      else if(selLength==0) ptab.Insert(selStart++, e.KeyChar);
      else SelectedText = e.KeyChar.ToString();

      Invalidate(false); // FIXME: this definitely needs to be more intelligent
      e.Handled = true;
    }
    done: base.OnKeyPress(e);
  }

  protected override void OnPaint(PaintEventArgs e)
  { base.OnPaint(e);

    Font font = Font;
    Brush brush = new SolidBrush(ForeColor);

    int y=padding, fhei=font.Height, topLine=vbar.Value, nlines=(e.ClipRectangle.Height+fhei-1)/fhei, tlines=LineCount;
    while(y+fhei<e.ClipRectangle.Top) { y += fhei; topLine++; }

    RectangleF rect = e.ClipRectangle;
    rect.Inflate(-padding, 0);
    rect.Y = y;
    rect.Height = fhei;

    e.Graphics.TextRenderingHint =
      antiAliased ? TextRenderingHint.AntiAliasGridFit : TextRenderingHint.SingleBitPerPixelGridFit;

    string[] lines = GetLines(topLine, nlines);
    for(nlines=Math.Min(nlines+topLine, tlines); topLine<nlines; rect.Y+=rect.Height, topLine++)
      e.Graphics.DrawString(lines[topLine], font, brush, rect, StringFormat.GenericTypographic);

    if(Focused && caretControl==this)
    { Rectangle caret = Rectangle.Intersect(CaretRect, e.ClipRectangle);
      caret.Location = PointToScreen(caret.Location);
      if(caret.Width!=0) ControlPaint.FillReversibleRectangle(caret, BackColor);
    }
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

  protected override void OnParentChanged(EventArgs e)
  { if(Parent==null && caretControl==this) caretControl=null;
    base.OnParentChanged(e);
  }

  protected override void OnResize(EventArgs e)
  { base.OnResize(e);
    RecalcScrollbars();
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

  const int padding=2;

  Rectangle CaretRect
  { get
    { //Point pt = GetPositionFromCharIndex(selStart);
      return new Rectangle(padding+(int)Math.Round(selStart*charWidth), padding, 2, Font.Height);
    }
  }

  int LineCount { get { return ptab.LineCount; } }
  int MaxVisibleLines { get { return ClientSize.Height/Font.Height; } }

  void ExamineFont()
  { StringFormat sf = StringFormat.GenericTypographic;

    Graphics g = Graphics.FromHwnd(Handle);
    g.TextRenderingHint =
      antiAliased ? TextRenderingHint.AntiAliasGridFit : TextRenderingHint.SingleBitPerPixelGridFit;

    float w1 = g.MeasureString("WWWWWWWWWWWWWWW", Font, 2000, sf).Width;
    float w2 = g.MeasureString("iiiiiiiiiiiiiii", Font, 2000, sf).Width;
    float w3 = g.MeasureString("lllllllllllllll", Font, 2000, sf).Width;
    float w4 = g.MeasureString("...............", Font, 2000, sf).Width;

    const float epsilon = 0.001f;
    monoSpaced = Math.Abs(w1-w2)<epsilon && Math.Abs(w1-w3)<epsilon && Math.Abs(w1-w4)<epsilon;
    if(monoSpaced) charWidth = (w1+w2+w3+w4)/60f;

    g.Dispose();
  }

  string[] GetLines(int start, int count) { return Lines; }

  void RecalcScrollbars()
  { vbar.LargeChange = MaxVisibleLines;
    int vmax = Math.Max(0, LineCount-vbar.LargeChange+1);
    vbar.Maximum = LineCount;
    if(vbar.Value>vmax) vbar.Value=vmax;

    if((scrollBars&TextEditorScrollBars.Vertical)!=0)
    { bool visible = (scrollBars&TextEditorScrollBars.Forced)==0 ? vbar.Maximum>MaxVisibleLines : true;
      if(visible && vbar.Parent==null) vbar.Parent=this;
      else if(!visible && vbar.Parent!=null) vbar.Parent=null;
    }

    if((scrollBars&TextEditorScrollBars.Horizontal)!=0)
    { bool visible = (scrollBars&TextEditorScrollBars.Forced)==0 ? false : true;
      if(visible && hbar.Parent==null) hbar.Parent=this;
      else if(!visible && hbar.Parent!=null) hbar.Parent=null;
    }
  }

  void ShowCaret() { Invalidate(CaretRect, false); }

  PieceTable ptab=new PieceTable();
  ScrollBar hbar=new HScrollBar(), vbar=new VScrollBar();
  int selStart, selLength, maxLength;
  float charWidth;
  CharacterCasing casing=CharacterCasing.Normal;
  BorderStyle borderStyle=BorderStyle.Fixed3D;
  TextEditorScrollBars scrollBars=TextEditorScrollBars.Both;
  bool acceptsTab=true, antiAliased, createUndos=true, hideSelection=true, modified, monoSpaced, readOnly;

  delegate void CaretDelegate();

  static void CaretTick(object dummy)
  { TextEditor ctl = caretControl;
    if(ctl!=null && ctl.Focused)
      try { ctl.Invoke(new CaretDelegate(ctl.ShowCaret)); }
      catch { }
    caretVisible=!caretVisible;
  }

  static System.Threading.Timer caretTimer =
    new System.Threading.Timer(new System.Threading.TimerCallback(CaretTick), null, 400, 400);
  static TextEditor caretControl;
  static bool caretVisible;
}

} // namespace Boa.IDE
