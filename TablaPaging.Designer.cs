
namespace PagingRoundRobin
{
    partial class TablaPaging
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
            if (disposing && (components != null))
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
            this.tabla_gridview = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.tabla_gridview)).BeginInit();
            this.SuspendLayout();
            // 
            // tabla_gridview
            // 
            this.tabla_gridview.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.tabla_gridview.BackgroundColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.tabla_gridview.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.tabla_gridview.GridColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.tabla_gridview.Location = new System.Drawing.Point(12, 12);
            this.tabla_gridview.Name = "tabla_gridview";
            this.tabla_gridview.ReadOnly = true;
            this.tabla_gridview.RowTemplate.Height = 25;
            this.tabla_gridview.Size = new System.Drawing.Size(357, 374);
            this.tabla_gridview.TabIndex = 1;
            // 
            // TablaPaging
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(381, 398);
            this.Controls.Add(this.tabla_gridview);
            this.Name = "TablaPaging";
            this.Text = "Tabla de paginación simple";
            this.Load += new System.EventHandler(this.TablaPaging_Load);
            ((System.ComponentModel.ISupportInitialize)(this.tabla_gridview)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView tabla_gridview;
    }
}