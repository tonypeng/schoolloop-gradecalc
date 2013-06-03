using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace gradecalc
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
            richTextBox1.SelectAll();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            richTextBox2.Clear();

            //try
            //{
                string[] parts = richTextBox1.Text.Split(new string[] { "SCORE PER CATEGORY" }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2)
                    return;

                // parts 2 should be the weight table
                Dictionary<string, float> weightMap = new Dictionary<string, float>();

                string[] weights = parts[1].Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                bool isweighted = true;

                foreach (string str in weights)
                {
                    if (str == "GRADE SCALE" || str == "Click here for an explanation of weighting.")
                        break;

                    string[] details = str.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    if (details.Length == 3)
                    {
                        details[1] = details[1].Substring(0, details[1].Length - 1);

                        float weight;

                        if (float.TryParse(details[1], out weight))
                        {
                            weightMap.Add(details[0], weight / 100f);
                        }
                    }
                    else
                    {
                        isweighted = false;
                        weightMap.Add("Total Grade", 1.0f);
                        break;
                    }
                }

                string[] grades = parts[0].Split(new string[] { "Submissions:" }, StringSplitOptions.RemoveEmptyEntries);

                grades[1] = grades[1].Trim();

                string[] assignments = grades[1].Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                Dictionary<string, float> bases = new Dictionary<string, float>();
                Dictionary<string, float> scores = new Dictionary<string, float>();

                float totalWeight = 0f;

                foreach (string assignment in assignments)
                {
                    string[] assignment_parts = assignment.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    if (assignment_parts.Length < 5)
                        continue; 
                    
                    string weight_type = !isweighted ? "Total Grade" : assignment_parts[0].Trim();

                    string[] score_parts = assignment_parts[4].Trim().Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries);

                    string[] num_den = score_parts[0].Trim().Split(new string[] { " / " }, StringSplitOptions.RemoveEmptyEntries);

                    if (!bases.ContainsKey(weight_type))
                    {
                        bases.Add(weight_type, 0f);
                        totalWeight += weightMap[weight_type];
                    }

                    bases[weight_type] += float.Parse(num_den[1]);

                    if (!scores.ContainsKey(weight_type))
                        scores.Add(weight_type, 0f);

                    scores[weight_type] += float.Parse(num_den[0]);
                }

                float finalScore = 0f;

                foreach (KeyValuePair<string, float> kvp in weightMap)
                {
                    if (scores.ContainsKey(kvp.Key))
                    {
                        finalScore += kvp.Value * scores[kvp.Key] / bases[kvp.Key];

                        writeline(kvp.Key + ": " + scores[kvp.Key] + " / " + bases[kvp.Key]);
                    }
                    else
                    {
                        writeline(kvp.Key + ": 0 / 0");
                    }
                }

                finalScore /= totalWeight;

                writeline(finalScore.ToString());
            //}
            //catch (Exception ex)
            //{
            //    writeline("error parsing text: " + ex.Message);
            //}
        }

        private void writeline(string str)
        {
            richTextBox2.Text += str + "\n";
        }
    }
}
