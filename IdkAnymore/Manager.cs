using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TwitterHentaiBot
{
    public partial class Manager : Form
    {
        public Manager()
        {
            InitializeComponent();
        }

        private void Manager_Load(object sender, EventArgs e)
        {
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"Likes: {Program.likes}");
            builder.AppendLine($"Retweets: {Program.retweets}");
            builder.AppendLine($"Followers: {Program.followers}");
            builder.AppendLine($"Following: {Program.following}");
            builder.AppendLine($"Tweet Count: {Program.tweetCount}");
            builder.Append($"Image Count: {Program.imageCount}");
            richTextBox1.Text = builder.ToString();
        }
    }
}