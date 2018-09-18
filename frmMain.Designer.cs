namespace MCAP
{
    partial class frmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if(disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.splCtrMain = new System.Windows.Forms.SplitContainer();
            this.splCtrLeft = new System.Windows.Forms.SplitContainer();
            this.lvwChnl = new System.Windows.Forms.ListView();
            this.chNo = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chCount = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lstMsg = new System.Windows.Forms.ListBox();
            this.lvwPoint = new System.Windows.Forms.ListView();
            this.chIndex = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chInID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chOutID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chDesc = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ssOutChnl = new System.Windows.Forms.StatusStrip();
            this.tsslblPrtc = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslblQueue = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslblDTM = new System.Windows.Forms.ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)(this.splCtrMain)).BeginInit();
            this.splCtrMain.Panel1.SuspendLayout();
            this.splCtrMain.Panel2.SuspendLayout();
            this.splCtrMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splCtrLeft)).BeginInit();
            this.splCtrLeft.Panel1.SuspendLayout();
            this.splCtrLeft.Panel2.SuspendLayout();
            this.splCtrLeft.SuspendLayout();
            this.ssOutChnl.SuspendLayout();
            this.SuspendLayout();
            // 
            // splCtrMain
            // 
            this.splCtrMain.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.splCtrMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splCtrMain.Location = new System.Drawing.Point(0, 0);
            this.splCtrMain.Name = "splCtrMain";
            // 
            // splCtrMain.Panel1
            // 
            this.splCtrMain.Panel1.Controls.Add(this.splCtrLeft);
            // 
            // splCtrMain.Panel2
            // 
            this.splCtrMain.Panel2.Controls.Add(this.lvwPoint);
            this.splCtrMain.Size = new System.Drawing.Size(944, 539);
            this.splCtrMain.SplitterDistance = 229;
            this.splCtrMain.TabIndex = 0;
            // 
            // splCtrLeft
            // 
            this.splCtrLeft.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.splCtrLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splCtrLeft.Location = new System.Drawing.Point(0, 0);
            this.splCtrLeft.Name = "splCtrLeft";
            this.splCtrLeft.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splCtrLeft.Panel1
            // 
            this.splCtrLeft.Panel1.Controls.Add(this.lvwChnl);
            // 
            // splCtrLeft.Panel2
            // 
            this.splCtrLeft.Panel2.Controls.Add(this.lstMsg);
            this.splCtrLeft.Size = new System.Drawing.Size(229, 539);
            this.splCtrLeft.SplitterDistance = 364;
            this.splCtrLeft.TabIndex = 0;
            // 
            // lvwChnl
            // 
            this.lvwChnl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lvwChnl.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chNo,
            this.chName,
            this.chCount});
            this.lvwChnl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvwChnl.Font = new System.Drawing.Font("Microsoft YaHei UI Light", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lvwChnl.FullRowSelect = true;
            this.lvwChnl.GridLines = true;
            this.lvwChnl.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lvwChnl.LabelWrap = false;
            this.lvwChnl.Location = new System.Drawing.Point(0, 0);
            this.lvwChnl.MultiSelect = false;
            this.lvwChnl.Name = "lvwChnl";
            this.lvwChnl.Size = new System.Drawing.Size(227, 362);
            this.lvwChnl.TabIndex = 0;
            this.lvwChnl.UseCompatibleStateImageBehavior = false;
            this.lvwChnl.View = System.Windows.Forms.View.Details;
            this.lvwChnl.MouseUp += new System.Windows.Forms.MouseEventHandler(this.LvwChnl_MouseUp);
            // 
            // chNo
            // 
            this.chNo.Text = "No";
            // 
            // chName
            // 
            this.chName.Text = "Desc";
            // 
            // chCount
            // 
            this.chCount.Text = "Update";
            // 
            // lstMsg
            // 
            this.lstMsg.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lstMsg.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstMsg.Font = new System.Drawing.Font("Microsoft YaHei UI Light", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lstMsg.FormattingEnabled = true;
            this.lstMsg.HorizontalScrollbar = true;
            this.lstMsg.ItemHeight = 20;
            this.lstMsg.Location = new System.Drawing.Point(0, 0);
            this.lstMsg.Name = "lstMsg";
            this.lstMsg.Size = new System.Drawing.Size(227, 169);
            this.lstMsg.TabIndex = 5;
            // 
            // lvwPoint
            // 
            this.lvwPoint.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lvwPoint.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chIndex,
            this.chInID,
            this.chOutID,
            this.chDesc,
            this.chValue,
            this.chTime});
            this.lvwPoint.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvwPoint.Font = new System.Drawing.Font("Microsoft YaHei UI Light", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lvwPoint.FullRowSelect = true;
            this.lvwPoint.GridLines = true;
            this.lvwPoint.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lvwPoint.LabelWrap = false;
            this.lvwPoint.Location = new System.Drawing.Point(0, 0);
            this.lvwPoint.MultiSelect = false;
            this.lvwPoint.Name = "lvwPoint";
            this.lvwPoint.Size = new System.Drawing.Size(709, 537);
            this.lvwPoint.TabIndex = 2;
            this.lvwPoint.UseCompatibleStateImageBehavior = false;
            this.lvwPoint.View = System.Windows.Forms.View.Details;
            // 
            // chIndex
            // 
            this.chIndex.Text = "ID";
            this.chIndex.Width = 40;
            // 
            // chInID
            // 
            this.chInID.Text = "输入端点名";
            this.chInID.Width = 112;
            // 
            // chOutID
            // 
            this.chOutID.Text = "输出端点名";
            this.chOutID.Width = 111;
            // 
            // chDesc
            // 
            this.chDesc.Text = "点描述";
            this.chDesc.Width = 137;
            // 
            // chValue
            // 
            this.chValue.Text = "值";
            // 
            // chTime
            // 
            this.chTime.Text = "时间戳";
            // 
            // ssOutChnl
            // 
            this.ssOutChnl.Font = new System.Drawing.Font("Microsoft YaHei UI Light", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ssOutChnl.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsslblPrtc,
            this.tsslblQueue,
            this.tsslblDTM});
            this.ssOutChnl.Location = new System.Drawing.Point(0, 539);
            this.ssOutChnl.Name = "ssOutChnl";
            this.ssOutChnl.Size = new System.Drawing.Size(944, 22);
            this.ssOutChnl.TabIndex = 1;
            this.ssOutChnl.Text = "statusStrip1";
            // 
            // tsslblPrtc
            // 
            this.tsslblPrtc.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsslblPrtc.Name = "tsslblPrtc";
            this.tsslblPrtc.Size = new System.Drawing.Size(0, 17);
            this.tsslblPrtc.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tsslblQueue
            // 
            this.tsslblQueue.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.tsslblQueue.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsslblQueue.Name = "tsslblQueue";
            this.tsslblQueue.Size = new System.Drawing.Size(929, 17);
            this.tsslblQueue.Spring = true;
            this.tsslblQueue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tsslblDTM
            // 
            this.tsslblDTM.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsslblDTM.Name = "tsslblDTM";
            this.tsslblDTM.Size = new System.Drawing.Size(0, 17);
            this.tsslblDTM.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(944, 561);
            this.Controls.Add(this.splCtrMain);
            this.Controls.Add(this.ssOutChnl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "frmMain";
            this.Text = "frmMain";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmMain_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FrmMain_FormClosed);
            this.Load += new System.EventHandler(this.FrmMain_Load);
            this.splCtrMain.Panel1.ResumeLayout(false);
            this.splCtrMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splCtrMain)).EndInit();
            this.splCtrMain.ResumeLayout(false);
            this.splCtrLeft.Panel1.ResumeLayout(false);
            this.splCtrLeft.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splCtrLeft)).EndInit();
            this.splCtrLeft.ResumeLayout(false);
            this.ssOutChnl.ResumeLayout(false);
            this.ssOutChnl.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer splCtrMain;
        private System.Windows.Forms.StatusStrip ssOutChnl;
        private System.Windows.Forms.ListView lvwPoint;
        private System.Windows.Forms.ColumnHeader chIndex;
        private System.Windows.Forms.ColumnHeader chInID;
        private System.Windows.Forms.ColumnHeader chOutID;
        private System.Windows.Forms.ColumnHeader chDesc;
        private System.Windows.Forms.ColumnHeader chValue;
        private System.Windows.Forms.ColumnHeader chTime;
        private System.Windows.Forms.SplitContainer splCtrLeft;
        private System.Windows.Forms.ListBox lstMsg;
        private System.Windows.Forms.ListView lvwChnl;
        private System.Windows.Forms.ColumnHeader chNo;
        private System.Windows.Forms.ColumnHeader chName;
        private System.Windows.Forms.ColumnHeader chCount;
        private System.Windows.Forms.ToolStripStatusLabel tsslblPrtc;
        private System.Windows.Forms.ToolStripStatusLabel tsslblQueue;
        private System.Windows.Forms.ToolStripStatusLabel tsslblDTM;
    }
}