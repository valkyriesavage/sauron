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
            this.processFeatures = new System.Windows.Forms.Button();
            this.insert_camera = new System.Windows.Forms.Button();
            this.register = new System.Windows.Forms.Button();
            this.test_mode = new System.Windows.Forms.Button();
            this.quick_check = new System.Windows.Forms.Button();
            this.print = new System.Windows.Forms.Button();
            this.saurontitle = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.visualize = new System.Windows.Forms.CheckBox();
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
            // processFeatures
            // 
            this.processFeatures.Location = new System.Drawing.Point(44, 176);
            this.processFeatures.Name = "processFeatures";
            this.processFeatures.Size = new System.Drawing.Size(139, 49);
            this.processFeatures.TabIndex = 4;
            this.processFeatures.Text = "computer vision-ize components";
            this.processFeatures.UseVisualStyleBackColor = true;
            this.processFeatures.Click += new System.EventHandler(this.processFeatures_Click);
            // 
            // insert_camera
            // 
            this.insert_camera.Location = new System.Drawing.Point(44, 43);
            this.insert_camera.Name = "insert_camera";
            this.insert_camera.Size = new System.Drawing.Size(139, 49);
            this.insert_camera.TabIndex = 5;
            this.insert_camera.Text = "place camera";
            this.insert_camera.UseVisualStyleBackColor = true;
            this.insert_camera.Click += new System.EventHandler(this.insert_camera_Click);
            // 
            // register
            // 
            this.register.Location = new System.Drawing.Point(44, 286);
            this.register.Name = "register";
            this.register.Size = new System.Drawing.Size(139, 49);
            this.register.TabIndex = 6;
            this.register.Text = "register printed components";
            this.register.UseVisualStyleBackColor = true;
            this.register.Click += new System.EventHandler(this.register_Click);
            // 
            // test_mode
            // 
            this.test_mode.Location = new System.Drawing.Point(44, 341);
            this.test_mode.Name = "test_mode";
            this.test_mode.Size = new System.Drawing.Size(139, 49);
            this.test_mode.TabIndex = 7;
            this.test_mode.Text = "start testing!";
            this.test_mode.UseVisualStyleBackColor = true;
            this.test_mode.Click += new System.EventHandler(this.test_mode_Click);
            // 
            // quick_check
            // 
            this.quick_check.Location = new System.Drawing.Point(44, 98);
            this.quick_check.Name = "quick_check";
            this.quick_check.Size = new System.Drawing.Size(139, 49);
            this.quick_check.TabIndex = 8;
            this.quick_check.Text = "quick check";
            this.quick_check.UseVisualStyleBackColor = true;
            this.quick_check.Click += new System.EventHandler(this.quick_check_Click);
            // 
            // print
            // 
            this.print.Location = new System.Drawing.Point(44, 231);
            this.print.Name = "print";
            this.print.Size = new System.Drawing.Size(139, 49);
            this.print.TabIndex = 9;
            this.print.Text = "print whole device";
            this.print.UseVisualStyleBackColor = true;
            this.print.Click += new System.EventHandler(this.print_Click);
            // 
            // saurontitle
            // 
            this.saurontitle.AutoSize = true;
            this.saurontitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.saurontitle.Location = new System.Drawing.Point(25, 0);
            this.saurontitle.Name = "saurontitle";
            this.saurontitle.Size = new System.Drawing.Size(186, 17);
            this.saurontitle.TabIndex = 10;
            this.saurontitle.Text = "sauron : computer vision-ize";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(74, 17);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(85, 17);
            this.label4.TabIndex = 11;
            this.label4.Text = "your models";
            // 
            // visualize
            // 
            this.visualize.AutoSize = true;
            this.visualize.Location = new System.Drawing.Point(77, 153);
            this.visualize.Name = "visualize";
            this.visualize.Size = new System.Drawing.Size(66, 17);
            this.visualize.TabIndex = 12;
            this.visualize.Text = "visualize";
            this.visualize.UseVisualStyleBackColor = true;
            // 
            // SWTaskpaneHost
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.visualize);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.saurontitle);
            this.Controls.Add(this.print);
            this.Controls.Add(this.quick_check);
            this.Controls.Add(this.test_mode);
            this.Controls.Add(this.register);
            this.Controls.Add(this.insert_camera);
            this.Controls.Add(this.processFeatures);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "SWTaskpaneHost";
            this.Size = new System.Drawing.Size(225, 417);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button processFeatures;
        private System.Windows.Forms.Button insert_camera;
        private System.Windows.Forms.Button register;
        private System.Windows.Forms.Button test_mode;
        private System.Windows.Forms.Button quick_check;
        private System.Windows.Forms.Button print;
        private System.Windows.Forms.Label saurontitle;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox visualize;
    }
}
