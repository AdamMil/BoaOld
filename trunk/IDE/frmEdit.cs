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

	  InitializeComponent();
	  edit.Focus();
	  
	  timer = new Timer();
	  timer.Interval = 5000;
	  timer.Tick += new EventHandler(highLight_Tick);
	  timer.Enabled = true;
	}

  public string Filename { get { return filename; } }
  public bool Modified { get { return modified; } }

  public new void Load(string path)
  { if(modified && MessageBox.Show("The current document has been modified. Loading a file will discard these changes. Discard changes?",
                                   "Discard changes?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                                   MessageBoxDefaultButton.Button2) != DialogResult.Yes)
      return;

    StreamReader sw = new StreamReader(path);
    edit.Text = sw.ReadToEnd();
    sw.Close();
    SetFilename(Path.GetFullPath(path));
  }

  public void Run()
  { Run(edit.Text, false);
    Options.Interactive = false;

    if(edit.Text.Trim()=="") return;
    try
    { Statement stmt = Parser.FromString(edit.Text).Parse();
      stmt.PostProcessForCompile();
      SnippetMaker.Generate(stmt).Run(boaFrame);
    }
    catch(Exception ex) { immediate.AppendLine("Error {0}: {1}", ex.GetType().Name, ex.Message); }
  }

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
    { StreamWriter sw = new StreamWriter(filename);
      sw.Write(edit.Text);
      sw.Close();
      modified = false;
      Text = Path.GetFileName(filename);
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
    this.AcBox = new Boa.IDE.AutoCompleteBox();
    this.immediate = new Boa.IDE.ImmediateBox();
    this.edit = new Boa.IDE.BoaBox();
    this.lblCode = new System.Windows.Forms.Label();
    this.lblImmediate = new System.Windows.Forms.Label();
    this.pnlCode = new System.Windows.Forms.Panel();
    this.pnlImmediate = new System.Windows.Forms.Panel();
    this.splitter = new System.Windows.Forms.Splitter();
    this.pnlCode.SuspendLayout();
    this.pnlImmediate.SuspendLayout();
    this.SuspendLayout();
    // 
    // AcBox
    // 
    this.AcBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
    this.AcBox.Location = new System.Drawing.Point(144, 144);
    this.AcBox.Name = "AcBox";
    this.AcBox.Size = new System.Drawing.Size(208, 106);
    this.AcBox.TabIndex = 4;
    this.AcBox.Visible = false;
    // 
    // immediate
    // 
    this.immediate.AcceptsTab = true;
    this.immediate.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
      | System.Windows.Forms.AnchorStyles.Left) 
      | System.Windows.Forms.AnchorStyles.Right)));
    this.immediate.Font = new System.Drawing.Font("Courier New", 10F);
    this.immediate.Location = new System.Drawing.Point(0, 16);
    this.immediate.Name = "immediate";
    this.immediate.Size = new System.Drawing.Size(656, 84);
    this.immediate.TabIndex = 0;
    this.immediate.Text = "";
    this.immediate.WordWrap = false;
    // 
    // edit
    // 
    this.edit.AcceptsTab = true;
    this.edit.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
      | System.Windows.Forms.AnchorStyles.Left) 
      | System.Windows.Forms.AnchorStyles.Right)));
    this.edit.Font = new System.Drawing.Font("Courier New", 10F);
    this.edit.Location = new System.Drawing.Point(0, 16);
    this.edit.Name = "edit";
    this.edit.Size = new System.Drawing.Size(656, 289);
    this.edit.TabIndex = 0;
    this.edit.Text = "";
    this.edit.WordWrap = false;
    this.edit.TextChanged += new System.EventHandler(this.edit_TextChanged);
    // 
    // lblCode
    // 
    this.lblCode.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
      | System.Windows.Forms.AnchorStyles.Right)));
    this.lblCode.Location = new System.Drawing.Point(0, 0);
    this.lblCode.Name = "lblCode";
    this.lblCode.Size = new System.Drawing.Size(656, 16);
    this.lblCode.TabIndex = 2;
    this.lblCode.Text = "Source code";
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
    this.pnlCode.Controls.Add(this.lblCode);
    this.pnlCode.Controls.Add(this.edit);
    this.pnlCode.Dock = System.Windows.Forms.DockStyle.Fill;
    this.pnlCode.Location = new System.Drawing.Point(0, 0);
    this.pnlCode.Name = "pnlCode";
    this.pnlCode.Size = new System.Drawing.Size(656, 305);
    this.pnlCode.TabIndex = 3;
    // 
    // pnlImmediate
    // 
    this.pnlImmediate.Controls.Add(this.lblImmediate);
    this.pnlImmediate.Controls.Add(this.immediate);
    this.pnlImmediate.Dock = System.Windows.Forms.DockStyle.Bottom;
    this.pnlImmediate.Location = new System.Drawing.Point(0, 305);
    this.pnlImmediate.Name = "pnlImmediate";
    this.pnlImmediate.Size = new System.Drawing.Size(656, 100);
    this.pnlImmediate.TabIndex = 3;
    // 
    // splitter
    // 
    this.splitter.Dock = System.Windows.Forms.DockStyle.Bottom;
    this.splitter.Location = new System.Drawing.Point(0, 302);
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
    this.Controls.Add(this.AcBox);
    this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
    this.KeyPreview = true;
    this.Name = "EditForm";
    this.Text = "Editor";
    this.pnlCode.ResumeLayout(false);
    this.pnlImmediate.ResumeLayout(false);
    this.ResumeLayout(false);

  }
	#endregion

  protected override void Dispose(bool disposing)
  { timer.Dispose();
    base.Dispose(disposing);
  }

  protected override void OnEnter(EventArgs e)
  { Boa.Modules.sys.displayhook = immediate.displayhook;
    base.OnEnter(e);
  }

  protected override void OnClosing(CancelEventArgs e)
  { if(!e.Cancel && modified)
    { DialogResult res = MessageBox.Show("This window has not been saved. Save before closing?",
                                         "Save "+(filename==null ? "file" : Path.GetFileName(filename))+"?",
                                         MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
      if(res==DialogResult.Cancel) e.Cancel=true;
      else if(res==DialogResult.Yes)
        try
        { Save();
          e.Cancel = modified;
        }
        catch { e.Cancel=true; }
    }

    base.OnClosing(e);
  }

  protected override void OnKeyDown(KeyEventArgs e)
  { if(e.KeyData==Keys.F6)
    { if(edit.Focused) immediate.Focus();
      else edit.Focus();
      e.Handled = true;
    }
    base.OnKeyDown(e);
  }

  void edit_TextChanged(object sender, EventArgs e)
  { if(!modified && edit.Modified)
    { modified=true;
      Text += "*";
    }
  }

  void highLight_Tick(object sender, EventArgs e)
  { edit.PerformSyntaxHighlighting();
    immediate.PerformSyntaxHighlighting();
  }

  void SetFilename(string path)
  { filename = path;
    Text = Path.GetFileName(path);
  }

  internal BoaBox edit;
  internal ImmediateBox immediate;
  internal AutoCompleteBox AcBox;
  internal Frame boaFrame;
  Timer timer;
  string filename;
  bool modified;

  System.Windows.Forms.Label lblImmediate;
  System.Windows.Forms.Label lblCode;
  System.Windows.Forms.Panel pnlCode;
  System.Windows.Forms.Panel pnlImmediate;
  System.Windows.Forms.Splitter splitter;
}

} // namespace Boa.IDE
