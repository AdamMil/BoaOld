using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace Boa.IDE
{

public class OutputForm : System.Windows.Forms.Form
{ public OutputForm()
	{ InitializeComponent();
		Console.SetOut(new Writer(textBox));
	}

  sealed class Writer : TextWriter
  { public Writer(System.Windows.Forms.TextBox textBox) { box=textBox; }

    public override System.Text.Encoding Encoding
    { get { return System.Text.Encoding.Unicode; }
    }

    public override void Write(char value)
    { bool end = box.SelectionStart==box.TextLength;
      if(box.TextLength==box.MaxLength) box.Text = box.Text.Substring(box.TextLength/2);
      box.Text += value;
      if(end)
      { box.SelectionStart = box.TextLength;
        box.SelectionLength = 0;
      }
    }
    
    public override void Write(string value)
    { bool end = box.SelectionStart==box.TextLength;
      if(value.Length>box.MaxLength) value = value.Substring(0, box.MaxLength);
      int remove = box.TextLength+value.Length - box.MaxLength;
      if(remove>0) box.Text = box.Text.Substring(Math.Max(box.TextLength/2, remove));
      box.Text += value;
      if(end)
      { box.SelectionStart = box.TextLength;
        box.SelectionLength = 0;
      }
    }

    System.Windows.Forms.TextBox box;
  }

  System.Windows.Forms.TextBox textBox;

	#region Windows Form Designer generated code
	void InitializeComponent()
	{
    System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(OutputForm));
    this.textBox = new System.Windows.Forms.TextBox();
    this.SuspendLayout();
    // 
    // textBox
    // 
    this.textBox.Dock = System.Windows.Forms.DockStyle.Fill;
    this.textBox.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
    this.textBox.Location = new System.Drawing.Point(0, 0);
    this.textBox.Multiline = true;
    this.textBox.Name = "textBox";
    this.textBox.Size = new System.Drawing.Size(576, 197);
    this.textBox.TabIndex = 0;
    this.textBox.Text = "";
    // 
    // OutputForm
    // 
    this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
    this.ClientSize = new System.Drawing.Size(576, 197);
    this.Controls.Add(this.textBox);
    this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
    this.Name = "OutputForm";
    this.Text = "Console Output";
    this.ResumeLayout(false);
  }
	#endregion
	
  protected override void OnClosing(CancelEventArgs e)
  { Hide();
    e.Cancel = true;
    base.OnClosing(e);
  }

}

} // namespace Boa.IDE