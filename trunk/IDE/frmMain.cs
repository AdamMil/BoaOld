using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace Boa.IDE
{
	public class MainForm : Form
	{
    private System.Windows.Forms.MainMenu menuBar;
    private System.Windows.Forms.MenuItem menuFile;
    private System.Windows.Forms.MenuItem menuSaveAs;
    private System.Windows.Forms.MenuItem menuSep1;
    private System.Windows.Forms.MenuItem menuWindow;
    private System.Windows.Forms.MenuItem menuNew;
    private System.Windows.Forms.MenuItem menuOpen;
    private System.Windows.Forms.MenuItem menuClose;
    private System.Windows.Forms.MenuItem menuSave;
    private System.Windows.Forms.MenuItem menuExit;
    private System.Windows.Forms.MenuItem menuDebug;
    private System.Windows.Forms.MenuItem menuExamine;
    private System.Windows.Forms.MenuItem menuSep2;
    private System.Windows.Forms.MenuItem menuCascade;
    private System.Windows.Forms.MenuItem menuHorz;
    private System.Windows.Forms.MenuItem menuVert;
    private System.Windows.Forms.MenuItem menuWindowOutput;
    private System.Windows.Forms.MenuItem menuCompile;
    private System.Windows.Forms.MenuItem menuEdit;
    private System.Windows.Forms.MenuItem menuUndo;
    private System.Windows.Forms.MenuItem menuRedo;
    private OutputForm outputForm;
  
		public MainForm()
		{ InitializeComponent();
		  outputForm = new OutputForm();
		}

		#region Windows Form Designer generated code
		private void InitializeComponent()
    {
      System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(MainForm));
      this.menuBar = new System.Windows.Forms.MainMenu();
      this.menuFile = new System.Windows.Forms.MenuItem();
      this.menuNew = new System.Windows.Forms.MenuItem();
      this.menuOpen = new System.Windows.Forms.MenuItem();
      this.menuClose = new System.Windows.Forms.MenuItem();
      this.menuCompile = new System.Windows.Forms.MenuItem();
      this.menuSave = new System.Windows.Forms.MenuItem();
      this.menuSaveAs = new System.Windows.Forms.MenuItem();
      this.menuSep1 = new System.Windows.Forms.MenuItem();
      this.menuExit = new System.Windows.Forms.MenuItem();
      this.menuEdit = new System.Windows.Forms.MenuItem();
      this.menuUndo = new System.Windows.Forms.MenuItem();
      this.menuRedo = new System.Windows.Forms.MenuItem();
      this.menuDebug = new System.Windows.Forms.MenuItem();
      this.menuExamine = new System.Windows.Forms.MenuItem();
      this.menuWindow = new System.Windows.Forms.MenuItem();
      this.menuWindowOutput = new System.Windows.Forms.MenuItem();
      this.menuSep2 = new System.Windows.Forms.MenuItem();
      this.menuCascade = new System.Windows.Forms.MenuItem();
      this.menuHorz = new System.Windows.Forms.MenuItem();
      this.menuVert = new System.Windows.Forms.MenuItem();
      // 
      // menuBar
      // 
      this.menuBar.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                                                                            this.menuFile,
                                                                            this.menuEdit,
                                                                            this.menuDebug,
                                                                            this.menuWindow});
      // 
      // menuFile
      // 
      this.menuFile.Index = 0;
      this.menuFile.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                                                                             this.menuNew,
                                                                             this.menuOpen,
                                                                             this.menuClose,
                                                                             this.menuCompile,
                                                                             this.menuSave,
                                                                             this.menuSaveAs,
                                                                             this.menuSep1,
                                                                             this.menuExit});
      this.menuFile.Text = "&File";
      this.menuFile.Popup += new System.EventHandler(this.menuFile_Popup);
      // 
      // menuNew
      // 
      this.menuNew.Index = 0;
      this.menuNew.Shortcut = System.Windows.Forms.Shortcut.CtrlN;
      this.menuNew.Text = "&New";
      this.menuNew.Click += new System.EventHandler(this.menuNew_Click);
      // 
      // menuOpen
      // 
      this.menuOpen.Index = 1;
      this.menuOpen.Shortcut = System.Windows.Forms.Shortcut.CtrlO;
      this.menuOpen.Text = "&Open...";
      this.menuOpen.Click += new System.EventHandler(this.menuOpen_Click);
      // 
      // menuClose
      // 
      this.menuClose.Enabled = false;
      this.menuClose.Index = 2;
      this.menuClose.Text = "Close";
      this.menuClose.Click += new System.EventHandler(this.menuClose_Click);
      // 
      // menuCompile
      // 
      this.menuCompile.Enabled = false;
      this.menuCompile.Index = 3;
      this.menuCompile.Text = "&Compile...";
      // 
      // menuSave
      // 
      this.menuSave.Enabled = false;
      this.menuSave.Index = 4;
      this.menuSave.Shortcut = System.Windows.Forms.Shortcut.CtrlS;
      this.menuSave.Text = "&Save";
      this.menuSave.Click += new System.EventHandler(this.menuSave_Click);
      // 
      // menuSaveAs
      // 
      this.menuSaveAs.Enabled = false;
      this.menuSaveAs.Index = 5;
      this.menuSaveAs.Text = "Save &as...";
      this.menuSaveAs.Click += new System.EventHandler(this.menuSaveAs_Click);
      // 
      // menuSep1
      // 
      this.menuSep1.Index = 6;
      this.menuSep1.Text = "-";
      // 
      // menuExit
      // 
      this.menuExit.Index = 7;
      this.menuExit.Text = "E&xit";
      this.menuExit.Click += new System.EventHandler(this.menuExit_Click);
      // 
      // menuEdit
      // 
      this.menuEdit.Index = 1;
      this.menuEdit.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                                                                             this.menuUndo,
                                                                             this.menuRedo});
      this.menuEdit.Text = "&Edit";
      this.menuEdit.Popup += new System.EventHandler(this.menuEdit_Popup);
      // 
      // menuUndo
      // 
      this.menuUndo.Enabled = false;
      this.menuUndo.Index = 0;
      this.menuUndo.Shortcut = System.Windows.Forms.Shortcut.CtrlZ;
      this.menuUndo.Text = "&Undo";
      this.menuUndo.Click += new System.EventHandler(this.menuUndo_Click);
      // 
      // menuRedo
      // 
      this.menuRedo.Enabled = false;
      this.menuRedo.Index = 1;
      this.menuRedo.Shortcut = System.Windows.Forms.Shortcut.CtrlY;
      this.menuRedo.Text = "&Redo";
      this.menuRedo.Click += new System.EventHandler(this.menuRedo_Click);
      // 
      // menuDebug
      // 
      this.menuDebug.Index = 2;
      this.menuDebug.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                                                                              this.menuExamine});
      this.menuDebug.MergeOrder = 1;
      this.menuDebug.MergeType = System.Windows.Forms.MenuMerge.MergeItems;
      this.menuDebug.Text = "&Debug";
      this.menuDebug.Popup += new System.EventHandler(this.menuDebug_Popup);
      // 
      // menuExamine
      // 
      this.menuExamine.Index = 0;
      this.menuExamine.Text = "&Examine object";
      // 
      // menuWindow
      // 
      this.menuWindow.Index = 3;
      this.menuWindow.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                                                                               this.menuWindowOutput,
                                                                               this.menuSep2,
                                                                               this.menuCascade,
                                                                               this.menuHorz,
                                                                               this.menuVert});
      this.menuWindow.MergeOrder = 2;
      this.menuWindow.Text = "&Window";
      // 
      // menuWindowOutput
      // 
      this.menuWindowOutput.Index = 0;
      this.menuWindowOutput.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftO;
      this.menuWindowOutput.Text = "Show &output";
      this.menuWindowOutput.Click += new System.EventHandler(this.menuWindowOutput_Click);
      // 
      // menuSep2
      // 
      this.menuSep2.Index = 1;
      this.menuSep2.Text = "-";
      // 
      // menuCascade
      // 
      this.menuCascade.Index = 2;
      this.menuCascade.Text = "Cascade";
      this.menuCascade.Click += new System.EventHandler(this.menuCascade_Click);
      // 
      // menuHorz
      // 
      this.menuHorz.Index = 3;
      this.menuHorz.Text = "Tile horizontally";
      this.menuHorz.Click += new System.EventHandler(this.menuHorz_Click);
      // 
      // menuVert
      // 
      this.menuVert.Index = 4;
      this.menuVert.Text = "Tile vertically";
      this.menuVert.Click += new System.EventHandler(this.menuVert_Click);
      // 
      // MainForm
      // 
      this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
      this.ClientSize = new System.Drawing.Size(612, 393);
      this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
      this.IsMdiContainer = true;
      this.Menu = this.menuBar;
      this.Name = "MainForm";
      this.Text = "Boa IDE";
      this.WindowState = System.Windows.Forms.FormWindowState.Maximized;

    }
		#endregion

    private void menuFile_Popup(object sender, EventArgs e)
    { menuClose.Enabled = menuCompile.Enabled = menuSave.Enabled = menuSaveAs.Enabled = ActiveMdiChild is EditForm;
    }

    private void menuDebug_Popup(object sender, EventArgs e)
    { menuExamine.Enabled = ActiveMdiChild is EditForm;
    }

    private void menuClose_Click(object sender, System.EventArgs e)
    { EditForm form = ActiveMdiChild as EditForm;
      if(form!=null) form.Close();
    }

    private void menuNew_Click(object sender, System.EventArgs e)
    { EditForm form = new EditForm();
      form.MdiParent = this;
      form.Show();
    }

    private void menuOpen_Click(object sender, System.EventArgs e)
    { EditForm form = ActiveMdiChild as EditForm;
      if(form!=null && form.Filename==null) form=null;

      OpenFileDialog fd = new OpenFileDialog();
      fd.DefaultExt = ".boa";
      fd.Filter = "Boa files (*.boa)|*.boa|All files (*.*)|*.*";
      if(form!=null) fd.InitialDirectory = System.IO.Path.GetDirectoryName(form.Filename);
      fd.RestoreDirectory = true;
      fd.Title = "Select source file...";
      if(fd.ShowDialog()==DialogResult.OK)
      { form = new EditForm();
        form.Load(fd.FileName);
        form.MdiParent = this;
        form.Show();
      }
    }

    private void menuSave_Click(object sender, System.EventArgs e)
    { EditForm form = ActiveMdiChild as EditForm;
      if(form!=null) form.Save();
    }

    private void menuSaveAs_Click(object sender, System.EventArgs e)
    { EditForm form = ActiveMdiChild as EditForm;
      if(form!=null) form.SaveAs();
    }

    private void menuCascade_Click(object sender, System.EventArgs e) { LayoutMdi(MdiLayout.Cascade); }
    private void menuHorz_Click(object sender, System.EventArgs e) { LayoutMdi(MdiLayout.TileHorizontal); }
    private void menuVert_Click(object sender, System.EventArgs e) { LayoutMdi(MdiLayout.TileVertical); }

    private void menuWindowOutput_Click(object sender, System.EventArgs e)
    { if(outputForm.MdiParent==null)
      { outputForm.MdiParent = this;
        outputForm.Show();
      }
      else outputForm.Show();
    }

    private void menuExit_Click(object sender, System.EventArgs e) { Close(); }

    private void menuEdit_Popup(object sender, EventArgs e)
    { Form form = ActiveMdiChild;
      Control ctl = form==null ? null : form.ActiveControl;
      menuRedo.Enabled = ctl is RichTextBox;
      menuUndo.Enabled = menuRedo.Enabled ? true : ctl is TextBoxBase;
    }

    private void menuUndo_Click(object sender, System.EventArgs e)
    { Form form = ActiveMdiChild;
      TextBoxBase ctl = form==null ? null : form.ActiveControl as TextBoxBase;
      ctl.Undo();
    }

    private void menuRedo_Click(object sender, System.EventArgs e)
    { Form form = ActiveMdiChild;
      RichTextBox ctl = form==null ? null : form.ActiveControl as RichTextBox;
      ctl.Redo();
    }
  }
}
