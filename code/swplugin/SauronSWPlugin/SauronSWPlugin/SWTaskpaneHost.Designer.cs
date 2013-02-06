namespace SauronSWPlugin
{
    partial class SWTaskpaneHost
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.processFeature = new System.Windows.Forms.Button();
            this.insert_camera = new System.Windows.Forms.Button();
            this.register = new System.Windows.Forms.Button();
            this.test_mode = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(19, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 13);
            this.label1.TabIndex = 0;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(137, 114);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(0, 13);
            this.label2.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(19, 190);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(0, 13);
            this.label3.TabIndex = 2;
            // 
            // processFeature
            // 
            this.processFeature.Location = new System.Drawing.Point(44, 98);
            this.processFeature.Name = "processFeature";
            this.processFeature.Size = new System.Drawing.Size(139, 49);
            this.processFeature.TabIndex = 4;
            this.processFeature.Text = "process this feature";
            this.processFeature.UseVisualStyleBackColor = true;
            this.processFeature.Click += new System.EventHandler(this.processFeature_Click);
            // 
            // insert_camera
            // 
            this.insert_camera.Location = new System.Drawing.Point(44, 43);
            this.insert_camera.Name = "insert_camera";
            this.insert_camera.Size = new System.Drawing.Size(139, 49);
            this.insert_camera.TabIndex = 5;
            this.insert_camera.Text = "insert camera";
            this.insert_camera.UseVisualStyleBackColor = true;
            this.insert_camera.Click += new System.EventHandler(this.insert_camera_Click);
            // 
            // register
            // 
            this.register.Location = new System.Drawing.Point(44, 154);
            this.register.Name = "register";
            this.register.Size = new System.Drawing.Size(139, 49);
            this.register.TabIndex = 6;
            this.register.Text = "register on print";
            this.register.UseVisualStyleBackColor = true;
            this.register.Click += new System.EventHandler(this.register_Click);
            // 
            // test_mode
            // 
            this.test_mode.Location = new System.Drawing.Point(44, 209);
            this.test_mode.Name = "test_mode";
            this.test_mode.Size = new System.Drawing.Size(139, 49);
            this.test_mode.TabIndex = 7;
            this.test_mode.Text = "enter test mode";
            this.test_mode.UseVisualStyleBackColor = true;
            this.test_mode.Click += new System.EventHandler(this.test_mode_Click);
            // 
            // SWTaskpaneHost
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.test_mode);
            this.Controls.Add(this.register);
            this.Controls.Add(this.insert_camera);
            this.Controls.Add(this.processFeature);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "SWTaskpaneHost";
            this.Size = new System.Drawing.Size(249, 417);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button processFeature;
        private System.Windows.Forms.Button insert_camera;
        private System.Windows.Forms.Button register;
        private System.Windows.Forms.Button test_mode;
    }
}
