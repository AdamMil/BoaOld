using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.ComponentModel;
using System.Windows.Forms;
using Boa.AST;
using Boa.Runtime;

namespace Boa.IDE
{

public class EditForm : System.Windows.Forms.Form
{ public EditForm()
	{ Module module = new Module();
	  boaFrame = new Frame(module);

    module.__setattr__("__builtins__", Importer.Import("__builtin__"));
    module.__setattr__("__name__", "__boaide__");

	  InitializeComponent();
	  edit.Focus();
	  edit.Document.DocumentChanged += new ICSharpCode.TextEditor.Document.DocumentEventHandler(Document_DocumentChanged);
	}

  public string Filename { get { return filename; } }
  public bool Modified { get { return modified; } }

  public new void Load(string path)
  { if(Modified && MessageBox.Show("The current document has been modified. Loading a file will discard these changes. Discard changes?",
                                   "Discard changes?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                                   MessageBoxDefaultButton.Button2) != DialogResult.Yes)
      return;

    edit.LoadFile(path, false);
    SetFilename(Path.GetFullPath(path));
    modified = false;
  }

  public void Run() { Run(edit.Text, false); }
  public void Run(string code, bool interactive)
  { if(code.Trim().Length==0) return;

    Options.Interactive = interactive;
    if(filename!=null && App.MainForm.AutoChdir) Modules.dotnet.chdir(Path.GetDirectoryName(filename));

    try
    { Statement stmt = Parser.FromString(code).Parse();
      stmt.PostProcessForCompile();
      SnippetMaker.Generate(stmt).Run(boaFrame);
    }
    catch(Exception ex) { immediate.AppendLine("Error {0}: {1}", ex.GetType().Name, ex.Message); }
  }

  public void Save()
  { if(filename==null) SaveAs();
    else
    { edit.SaveFile(filename);
      Text = Path.GetFileName(filename);
      modified = false;
    }
  }

  public void SaveAs()
  { SaveFileDialog fd = new SaveFileDialog();
    fd.DefaultExt = ".boa";
    fd.Filter = "Boa files (*.boa)|*.boa|All files (*.*)|*.*";
    if(filename!=null) fd.InitialDirectory = Path.GetDirectoryName(filename);
    fd.RestoreDirectory = true;
    fd.Title = "Select destination file...";
    if(fd.ShowDialog()==DialogResult.OK)
    { SetFilename(fd.FileName);
      Save();
    }
  }

	#region Windows Form Designer generated code
	void InitializeComponent()
	{
    System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(EditForm));
    this.immediate = new Boa.IDE.ImmediateBox();
    this.edit = new Boa.IDE.BoaBox();
    this.acbox = new Boa.IDE.AutoCompleteBox();
    this.lblImmediate = new System.Windows.Forms.Label();
    this.pnlCode = new System.Windows.Forms.Panel();
    this.pnlImmediate = new System.Windows.Forms.Panel();
    this.splitter = new System.Windows.Forms.Splitter();
    this.pnlCode.SuspendLayout();
    this.pnlImmediate.SuspendLayout();
    this.SuspendLayout();
    // 
    // immediate
    // 
    this.immediate.AllowDrop = true;
    this.immediate.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
      | System.Windows.Forms.AnchorStyles.Left) 
      | System.Windows.Forms.AnchorStyles.Right)));
    this.immediate.ConvertTabsToSpaces = true;
    this.immediate.EnableFolding = false;
    this.immediate.Encoding = ((System.Text.Encoding)(resources.GetObject("immediate.Encoding")));
    this.immediate.Location = new System.Drawing.Point(0, 16);
    this.immediate.Name = "immediate";
    this.immediate.ShowInvalidLines = false;
    this.immediate.ShowLineNumbers = false;
    this.immediate.Size = new System.Drawing.Size(656, 94);
    this.immediate.TabIndent = 2;
    this.immediate.TabIndex = 0;
    // 
    // edit
    // 
    this.edit.AllowDrop = true;
    this.edit.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
      | System.Windows.Forms.AnchorStyles.Left) 
      | System.Windows.Forms.AnchorStyles.Right)));
    this.edit.ConvertTabsToSpaces = true;
    this.edit.EnableFolding = false;
    this.edit.Encoding = ((System.Text.Encoding)(resources.GetObject("edit.Encoding")));
    this.edit.Location = new System.Drawing.Point(0, 0);
    this.edit.Name = "edit";
    this.edit.ShowInvalidLines = false;
    this.edit.ShowLineNumbers = false;
    this.edit.Size = new System.Drawing.Size(656, 288);
    this.edit.TabIndent = 2;
    this.edit.TabIndex = 0;
    // 
    // acbox
    // 
    this.acbox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
    this.acbox.Location = new System.Drawing.Point(144, 144);
    this.acbox.Name = "acbox";
    this.acbox.Size = new System.Drawing.Size(208, 106);
    this.acbox.TabIndex = 4;
    this.acbox.Visible = false;
    // 
    // lblImmediate
    // 
    this.lblImmediate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
      | System.Windows.Forms.AnchorStyles.Right)));
    this.lblImmediate.Location = new System.Drawing.Point(0, 0);
    this.lblImmediate.Name = "lblImmediate";
    this.lblImmediate.Size = new System.Drawing.Size(656, 16);
    this.lblImmediate.TabIndex = 2;
    this.lblImmediate.Text = "Immediate";
    // 
    // pnlCode
    // 
    this.pnlCode.Controls.Add(this.edit);
    this.pnlCode.Dock = System.Windows.Forms.DockStyle.Fill;
    this.pnlCode.Location = new System.Drawing.Point(0, 0);
    this.pnlCode.Name = "pnlCode";
    this.pnlCode.Size = new System.Drawing.Size(656, 293);
    this.pnlCode.TabIndex = 3;
    // 
    // pnlImmediate
    // 
    this.pnlImmediate.Controls.Add(this.lblImmediate);
    this.pnlImmediate.Controls.Add(this.immediate);
    this.pnlImmediate.Dock = System.Windows.Forms.DockStyle.Bottom;
    this.pnlImmediate.Location = new System.Drawing.Point(0, 293);
    this.pnlImmediate.Name = "pnlImmediate";
    this.pnlImmediate.Size = new System.Drawing.Size(656, 112);
    this.pnlImmediate.TabIndex = 3;
    // 
    // splitter
    // 
    this.splitter.Dock = System.Windows.Forms.DockStyle.Bottom;
    this.splitter.Location = new System.Drawing.Point(0, 290);
    this.splitter.Name = "splitter";
    this.splitter.Size = new System.Drawing.Size(656, 3);
    this.splitter.TabIndex = 5;
    this.splitter.TabStop = false;
    // 
    // EditForm
    // 
    this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
    this.ClientSize = new System.Drawing.Size(656, 405);
    this.Controls.Add(this.splitter);
    this.Controls.Add(this.pnlCode);
    this.Controls.Add(this.pnlImmediate);
    this.Controls.Add(this.acbox);
    this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
    this.KeyPreview = true;
    this.Name = "EditForm";
    this.Text = "New file";
    this.pnlCode.ResumeLayout(false);
    this.pnlImmediate.ResumeLayout(false);
    this.ResumeLayout(false);

  }
	#endregion

  protected override void OnEnter(EventArgs e)
  { Boa.Modules.sys.displayhook = immediate.displayhook;
    base.OnEnter(e);
  }

  protected override void OnClosing(CancelEventArgs e)
  { if(!e.Cancel && Modified)
    { DialogResult res = MessageBox.Show("This window has not been saved. Save before closing?",
                                         "Save "+(filename==null ? "file" : Path.GetFileName(filename))+"?",
                                         MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
      if(res==DialogResult.Cancel) e.Cancel=true;
      else if(res==DialogResult.Yes)
        try
        { Save();
          e.Cancel = Modified;
        }
        catch { e.Cancel=true; }
    }

    base.OnClosing(e);
  }

  protected override void OnKeyDown(KeyEventArgs e)
  { if(e.KeyData==Keys.F6)
    { if(edit.ContainsFocus) immediate.Focus();
      else edit.Focus();
      e.Handled = true;
    }
    base.OnKeyDown(e);
  }

  void SetFilename(string path)
  { filename = path;
    Text = Path.GetFileName(path);
  }

  void Document_DocumentChanged(object sender, ICSharpCode.TextEditor.Document.DocumentEventArgs e)
  { if(!modified)
    { modified = true;
      Text += "*";
    }
  }

  internal Frame boaFrame;
  internal AutoCompleteBox acbox;
  BoaBox edit;
  internal ImmediateBox immediate;
  string filename;
  bool modified;

  System.Windows.Forms.Label lblImmediate;
  System.Windows.Forms.Panel pnlCode;
  System.Windows.Forms.Panel pnlImmediate;
  System.Windows.Forms.Splitter splitter;
}

} // namespace Boa.IDE
