using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace F1_2020_Names_Changer {
	public partial class Form1 : Form {

		public string selectedFile;
		public string namesLookupFile;
		public string teamsLookupFile;

		bool willLooseChanges = false;
		bool haveShownCustomOffsetsPopup = false;

		public Form1() {
			Application.EnableVisualStyles(); // enable the scrolling marquee
			InitializeComponent();
		}

		private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

		private void Form1_Load(object sender, EventArgs e) {
			filePath.Text = Directory.GetCurrentDirectory();

			checkForDefaultLookups();
		}

		private void writeToF1ToolStripMenuItem_Click_1(object sender, EventArgs e) {
			start();
		}

		private void infoToolStripMenuItem_Click(object sender, EventArgs e) {
			var rule = LogManager.Configuration.LoggingRules[2]; // static because c# hates me trying to find the right one
			rule.DisableLoggingForLevels(LogLevel.Trace, LogLevel.Fatal);
			rule.EnableLoggingForLevels(LogLevel.Info, LogLevel.Fatal);
			LogManager.ReconfigExistingLoggers();
			infoToolStripMenuItem.Checked = true;
			debugToolStripMenuItem.Checked = false;
			traceToolStripMenuItem.Checked = false;

		}

		private void debugToolStripMenuItem_Click(object sender, EventArgs e) {
			var rule = LogManager.Configuration.LoggingRules[2]; // static because c# hates me trying to find the right one
			rule.DisableLoggingForLevels(LogLevel.Trace, LogLevel.Fatal);
			rule.EnableLoggingForLevels(LogLevel.Debug, LogLevel.Fatal);
			LogManager.ReconfigExistingLoggers();
			infoToolStripMenuItem.Checked = false;
			debugToolStripMenuItem.Checked = true;
			traceToolStripMenuItem.Checked = false;
		}

		private void traceToolStripMenuItem_Click(object sender, EventArgs e) {
			var rule = LogManager.Configuration.LoggingRules[2]; // static because c# hates me trying to find the right one
			rule.EnableLoggingForLevels(LogLevel.Trace, LogLevel.Fatal);
			LogManager.ReconfigExistingLoggers();
			infoToolStripMenuItem.Checked = false;
			debugToolStripMenuItem.Checked = false;
			traceToolStripMenuItem.Checked = true;
		}

		private void indexToolStripMenuItem_Click(object sender, EventArgs e) {
			System.Diagnostics.Process.Start("https://github.com/Electronics/F1NameChanger");
		}

		private void aboutToolStripMenuItem1_Click(object sender, EventArgs e) {
			var formPopup = new about();
			formPopup.Show(this);
		}

		private void stopToolStripMenuItem_Click(object sender, EventArgs e) {
			stop();
		}

		private void toolStripButtonWriteF1_click(object sender, EventArgs e) {
			start();
		}

		private void start() {
			resetIndicators();

			// if we find an offsets file from a previous run (hopefully sucessful...) ask the user whether we should use it
			if (File.Exists("offsets.json")) {
				if (!haveShownCustomOffsetsPopup) {
					DialogResult dialogResult = MessageBox.Show("A custom offset file has been detected (likely generated from a previous run). Would you like to use the custom offsets?", "Use Custom Offsets?", MessageBoxButtons.YesNoCancel);
					if (dialogResult == DialogResult.Cancel) return;
					else if(dialogResult == DialogResult.Yes) {
						useCustomOffset.Checked = true;
					}
					haveShownCustomOffsetsPopup = true;
				}
			}
			Task.Run(() => MemoryChanger.run(namesLookupFile, teamsLookupFile, false, useCustomOffset.Checked));
			toolStripButtonWriteF1.Enabled = false;
			writeToF1ToolStripMenuItem.Enabled = false;
			undoChangesToolStripMenuItem.Enabled = false;
			toolStripButtonUndo.Enabled = false;
			toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
		}

		private void stop() {
			MemoryChanger.Stop();
			toolStripButtonWriteF1.Enabled = true;
			writeToF1ToolStripMenuItem.Enabled = true;
			toolStripProgressBar1.Style = ProgressBarStyle.Blocks;
		}

		public void Stopped(bool alert=true) {
			if (alert) {
				MessageBox.Show("Something went wrong - check the console log at the bottom of the screen for details");
			}
			if (toolStrip1.InvokeRequired) toolStrip1.Invoke(new MethodInvoker(delegate {
				stop(); // call this from the ui thread (via the toolstrip because why not)
			}));
		}

		public void Finished(bool tryFindOffsets = false) {
			if (toolStrip1.InvokeRequired) toolStrip1.Invoke(new MethodInvoker(delegate {
				undoChangesToolStripMenuItem.Enabled = true; // call this from the ui thread (via the toolstrip because why not)
				toolStripButtonUndo.Enabled = true;
				toolStripButtonWriteF1.Enabled = true;
				writeToF1ToolStripMenuItem.Enabled = true;
				toolStripProgressBar1.Style = ProgressBarStyle.Blocks;


				if (tryFindOffsets) {
					DialogResult dialogResult = MessageBox.Show("It looks like some areas of memory were not found properly. Consider trying \"Game->Find Offsets\" to find customised offsets for your game and then re-run.\n NB: \"Find Offsets\" should be done on a fresh-restart of the game (where no memory changing has taken place), otherwise it may fail!", "Custom Offsets", MessageBoxButtons.OK);
				}
			}));
		}

		public void Update(string item, int value) {
			if (toolStrip1.InvokeRequired) toolStrip1.Invoke(new MethodInvoker(delegate {
				Color c;
				Color fc;
				if (value==2) {
					c = Color.Gold;
					fc = SystemColors.ControlText; // forecolor does nothing on disabled buttons :(
				} else if (value > 0) {
					c = Color.SpringGreen;
					fc = Color.White;
				} else if (value < 0) {
					c = SystemColors.Control;
					fc = SystemColors.ControlText;
				} else {
					c = Color.LightCoral;
					fc = Color.White;
				}
				switch (item) {
					case "region1":
						menuRegion1Indicator.BackColor = c;
						menuRegion1Indicator.ForeColor = fc;
						break;
					case "region2":
						menuRegion2Indicator.BackColor = c;
						menuRegion2Indicator.ForeColor = fc;
						break;
					case "charRegion":
						charSelectRegionIndicator.BackColor = c;
						charSelectRegionIndicator.ForeColor = fc;
						break;
					case "gameRegion":
						gameRegionIndicator.BackColor = c;
						gameRegionIndicator.ForeColor = fc;
						break;
					case "lookups":
						lookupIndicator.BackColor = c;
						lookupIndicator.ForeColor = fc;
						break;
				}
			}));
		}

		private void resetIndicators() {
			menuRegion1Indicator.BackColor = SystemColors.Control;
			menuRegion2Indicator.BackColor = SystemColors.Control;
			charSelectRegionIndicator.BackColor = SystemColors.Control;
			gameRegionIndicator.BackColor = SystemColors.Control;
			lookupIndicator.BackColor = SystemColors.Control;
			menuRegion1Indicator.ForeColor = SystemColors.ControlText;
			menuRegion2Indicator.ForeColor = SystemColors.ControlText;
			charSelectRegionIndicator.ForeColor = SystemColors.ControlText;
			gameRegionIndicator.ForeColor = SystemColors.ControlText;
			lookupIndicator.ForeColor = SystemColors.ControlText;
		}

		private void undo() {
			if (MessageBox.Show("Undo is an experimental feature and may not correctly undo all changes you have made. Always restart the game to be sure", "Experimental Feature!", MessageBoxButtons.YesNo) == DialogResult.No) {
				return;
			}
			resetIndicators();
			Task.Run(() => MemoryChanger.run(namesLookupFile, teamsLookupFile, true, useCustomOffset.Checked));
			toolStripButtonWriteF1.Enabled = false;
			writeToF1ToolStripMenuItem.Enabled = false;
			undoChangesToolStripMenuItem.Enabled = false;
			toolStripButtonUndo.Enabled = false;
			toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
		}

		private void updateTree() { // useful bits from https://www.c-sharpcorner.com/article/display-sub-directories-and-files-in-treeview/
			treeView1.Nodes.Clear();
			if (filePath.Text!= "" && Directory.Exists(filePath.Text)) {
				DirectoryInfo di = new DirectoryInfo(filePath.Text);
				TreeNode tds = treeView1.Nodes.Add(di.Name);
				tds.Tag = di.FullName;
				tds.StateImageIndex = 0;
				string[] Files = Directory.GetFiles(filePath.Text, "*.*");

				// Loop through them to see files  
				foreach (string file in Files) {
					FileInfo fi = new FileInfo(file);
					TreeNode tds_file = tds.Nodes.Add(fi.Name);
					tds_file.Tag = fi.FullName;
					tds_file.StateImageIndex = 1;

				}
				tds.Expand();
				//TODO: subdirs?

				checkForDefaultLookups();
			}
		}

		private void checkForDefaultLookups() {
			// see if we can find a "teams.txt" or "names.txt" or json etc
			bool teamsFound = false;
			bool namesFound = false;
			foreach (TreeNode n in treeView1.Nodes[0].Nodes) {
				if (n.Text == "teams.txt") {
					if (teamsFound) continue;
					log.Debug("Found existing teams.txt file, using as the default");
					n.NodeFont = new Font(treeView1.Font, FontStyle.Bold);
					n.Text += "[TEAM]";
					teamsLookupFile = n.Tag.ToString();
					teamsFound = true;
				} else if (n.Text == "teams.json") {
					if (teamsFound) continue;
					log.Debug("Found existing teams.json file, using as the default");
					n.NodeFont = new Font(treeView1.Font, FontStyle.Bold);
					n.Text += "[TEAM]";
					teamsLookupFile = n.Tag.ToString();
					teamsFound = true;
				} else if (n.Text == "names.txt") {
					if (namesFound) continue;
					log.Debug("Found existing teams.txt file, using as the default");
					n.NodeFont = new Font(treeView1.Font, FontStyle.Bold);
					n.Text += "[NAME]";
					namesLookupFile = n.Tag.ToString();
					namesFound = true;
				} else if (n.Text == "names.json") {
					if (namesFound) continue;
					log.Debug("Found existing names.json file, using as the default");
					n.NodeFont = new Font(treeView1.Font, FontStyle.Bold);
					n.Text += "[NAME]";
					namesLookupFile = n.Tag.ToString();
					namesFound = true;
				}
			}
		}

		private void openFileToEdit(string filename) {
			// double check this is actually a file
			FileAttributes attr = File.GetAttributes(filename);
			if (attr.HasFlag(FileAttributes.Directory)) return;

			// are we going to loose any changes?
			if (willLooseChanges) {
				log.Trace("We will be loosing changes");
				if(MessageBox.Show("Are you sure you want to discard changes made to the file?", "Are you sure?", MessageBoxButtons.YesNo)==DialogResult.No) {
					return;
				}
			}
			selectedFile = Path.GetFileName(filename);
			editorBox.Text = File.ReadAllText(filename);
		}

		private void toolStripButtonStop_Click(object sender, EventArgs e) {
			stop();
		}

		private void toolStripButtonUndo_Click(object sender, EventArgs e) {
			undo();
		}

		private void undoChangesToolStripMenuItem_Click(object sender, EventArgs e) {
			undo();
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e) {
			using (OpenFileDialog openFileDialog = new OpenFileDialog()) {
				openFileDialog.InitialDirectory = filePath.Text;
				openFileDialog.Filter = "txt files (*.txt)|*.txt|json files (*.json)|*.json|All files (*.*)|*.*";
				openFileDialog.FilterIndex = 0;
				openFileDialog.RestoreDirectory = true;

				if (openFileDialog.ShowDialog() == DialogResult.OK) {
					//Get the path of specified file
					filePath.Text = Path.GetDirectoryName(openFileDialog.FileName);
					openFileToEdit(openFileDialog.FileName);
					updateTree();
				}
			}
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
			File.WriteAllText(Path.Combine(filePath.Text, selectedFile), editorBox.Text);
			willLooseChanges = false;
			log.Info($"Saved {selectedFile}");
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e) {
			using (SaveFileDialog saveFileDialog = new SaveFileDialog()) {
				saveFileDialog.InitialDirectory = filePath.Text;
				saveFileDialog.Filter = "txt files (*.txt)|*.txt|json files (*.json)|*.json|All files (*.*)|*.*";
				saveFileDialog.FilterIndex = 0;
				saveFileDialog.RestoreDirectory = true;

				if (saveFileDialog.ShowDialog() == DialogResult.OK) {
					File.WriteAllText(saveFileDialog.FileName, editorBox.Text);
				}
			}
			willLooseChanges = false;
		}

		private void fontToolStripMenuItem_Click(object sender, EventArgs e) {
			using (FontDialog fd = new FontDialog()) {
				fd.Font = editorBox.Font;
				if (fd.ShowDialog() == DialogResult.OK) {
					editorBox.Font = fd.Font;
					logBox.Font = fd.Font;
				}
			}
		}

		private void treeView1_MouseMove(object sender, MouseEventArgs e) {
			// Get the node at the current mouse pointer location.  
			TreeNode theNode = this.treeView1.GetNodeAt(e.X, e.Y);

			// Set a ToolTip only if the mouse pointer is actually paused on a node.  
			if (theNode != null && theNode.Tag != null) {
				// Change the ToolTip only if the pointer moved to a new node.  
				if (theNode.Tag.ToString() != this.toolTip1.GetToolTip(this.treeView1))
					this.toolTip1.Show(theNode.Tag.ToString(), this.treeView1, e.Location.X+10, e.Location.Y+10, 2000);

			} else     // Pointer is not over a node so clear the ToolTip.  
			  {
				this.toolTip1.Hide(this.treeView1);
			}
		}

		private void filePath_TextChanged(object sender, EventArgs e) {
			updateTree();
		}

		private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e) {
			if (e.Button == MouseButtons.Right) {
				// Select the clicked node
				treeView1.SelectedNode = treeView1.GetNodeAt(e.X, e.Y);
			}
		}

		private void setNameLookup_Click(object sender, EventArgs e) {
			treeView1.SelectedNode.NodeFont = new Font(treeView1.Font, FontStyle.Bold);
			treeView1.SelectedNode.Text += "[NAME]"; // add a string to force-update the size of the text
			// find the original and remove the boldface
			// foreach as find is useless for some reason
			foreach (TreeNode n in treeView1.Nodes[0].Nodes) {
				if (n.Tag.Equals(namesLookupFile)) {
					n.NodeFont = new Font(treeView1.Font, FontStyle.Regular);
					n.Text = n.Text.Replace("[NAME]", "");
					log.Trace("Found old lookup file, changed font back to regular");
				}
			}
			namesLookupFile = treeView1.SelectedNode.Tag.ToString();
			log.Debug($"Names lookup file changed to: {namesLookupFile}");
		}

		private void setTeamLookup_Click(object sender, EventArgs e) {
			treeView1.SelectedNode.NodeFont = new Font(treeView1.Font, FontStyle.Bold);
			treeView1.SelectedNode.Text += "[TEAM]"; // add a string to force-update the size of the text
			// find the original and remove the boldface
			// foreach as find is useless for some reason
			foreach (TreeNode n in treeView1.Nodes[0].Nodes) {
				if (n.Tag.Equals(teamsLookupFile)) {
					n.NodeFont = new Font(treeView1.Font, FontStyle.Regular);
					n.Text = n.Text.Replace("[TEAM]", "");
					log.Trace("Found old lookup file, changed font back to regular");
				}
			}
			teamsLookupFile = treeView1.SelectedNode.Tag.ToString();
			log.Debug($"Teams lookup file changed to: {teamsLookupFile}");
		}

		private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e) {
			if (e.Node.Tag != null) {
				openFileToEdit(e.Node.Tag.ToString());
			}
		}

		private void editorBox_TextChanged(object sender, EventArgs e) {
			if (((RichTextBox)sender).ContainsFocus) {
				willLooseChanges = true; // only if a user has changed something
			}
		}

		private void logBox_TextChanged(object sender, EventArgs e) {
			logBox.SelectionStart = logBox.TextLength; // autoscroll to end
			logBox.ScrollToCaret();
		}

		private void cutToolStripMenuItem_Click(object sender, EventArgs e) {
			editorBox.Cut();
		}

		private void copyToolStripMenuItem_Click(object sender, EventArgs e) {
			editorBox.Copy();
		}

		private void pasteToolStripMenuItem_Click(object sender, EventArgs e) {
			editorBox.Paste();
		}

		private void selectAllToolStripMenuItem_Click(object sender, EventArgs e) {
			editorBox.SelectAll();
		}

		private void undoToolStripMenuItem_Click(object sender, EventArgs e) {
			editorBox.Undo();
		}

		private void redoToolStripMenuItem_Click(object sender, EventArgs e) {
			editorBox.Redo();
		}

		private void findOffsetsToolStripMenuItem_Click(object sender, EventArgs e) {
			Task.Run(() => MemoryChanger.findOffsets());
			useCustomOffset.Checked = true;
			haveShownCustomOffsetsPopup = true;
		}
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
			bool bHandled = false;
			switch (keyData) {
				case Keys.F5:
					if (treeView1.Focused) {
						updateTree();
					}
					bHandled = true;
					break;
			}
			return bHandled;
		}
	}
}
