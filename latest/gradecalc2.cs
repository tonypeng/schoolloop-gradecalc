using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace gradecalc2
{
    public partial class Form1 : Form
    {
        Dictionary<string, float> currentTotalPoints, currentActualPoints;
        Dictionary<string, float> currentWeights;

        Dictionary<string, float> masterCurrentTotalPoints, masterCurrentActualPoints, masterCurrentWeights;

        public Form1()
        {
            InitializeComponent();

            currentTotalPoints = new Dictionary<string, float>();
            currentActualPoints = new Dictionary<string, float>();
            currentWeights = new Dictionary<string,float>();

            masterCurrentActualPoints = new Dictionary<string, float>();
            masterCurrentTotalPoints = new Dictionary<string, float>();
            masterCurrentWeights = new Dictionary<string, float>();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                process();
            }
            catch
            {
                richTextBox2.Text = "Invalid input.";
            }
        }

        private void process()
        {
            clearOut();

            string input = richTextBox1.Text;
            Dictionary<string, float> totalPoints = new Dictionary<string, float>();
            Dictionary<string, float> actualPoints = new Dictionary<string, float>();
            Dictionary<string, float> sectionWeights = new Dictionary<string, float>();

            // STEP 1: parse the actual grades
            int start = input.IndexOf("Comment:");
            start += 10;

            string grades = input.Substring(start);
            string[] assignments = grades.Split(new string[] { "\t\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < assignments.Length - 1; i++) // -1 because of grade trend
            {
                string[] parts = assignments[i].Split('\n');
                // first line: section
                string title = parts[0].Trim();

                if (!totalPoints.ContainsKey(title))
                {
                    totalPoints.Add(title, 0);
                    actualPoints.Add(title, 0);
                }

                string score = parts[3];

                if (score.Trim() == string.Empty || score == "Excused" || score == "E")
                    continue;

                int equalsIndex = score.IndexOf('=');
                score = score.Substring(0, equalsIndex);
                string[] scoreParts = score.Split('/');

                float actual;
                float total;

                if (!Single.TryParse(scoreParts[0].Trim(), out actual))
                    actual = 0.0f;
                if (!Single.TryParse(scoreParts[1].Trim(), out total))
                    total = 0.0f;

                totalPoints[title] += total;
                actualPoints[title] += actual;
            }

            // STEP 2: parse the weights
            int weightsIndex = input.IndexOf("Category:");
            string weightSection = input.Substring(weightsIndex);
            int weightEndIndex = weightSection.IndexOf("\n\n");
            weightSection = weightSection.Substring(0, weightEndIndex);
            string[] weights = weightSection.Split('\n');

            // does the teacher even use weighting?
            if (weights[0].Split('\t').Length == 3) // yes
            {
                for (int i = 1; i < weights.Length; i++)
                {
                    string section = weights[i];

                    string[] sParts = section.Split('\t');
                    string weightPercentage = sParts[1].Substring(0, sParts[1].Length - 1); // get rid of the '%'

                    sectionWeights.Add(sParts[0], Single.Parse(weightPercentage));
                }

                // check if there are any sections without any points (i.e. finals section before finals)
                foreach (KeyValuePair<string, float> kvp in sectionWeights)
                {
                    string section = kvp.Key;

                    if (!actualPoints.ContainsKey(section)) // no points
                    {
                        actualPoints.Add(section, -1f);
                        totalPoints.Add(section, -1f);
                    }
                }

                // check if there are any sections that don't exist in the weight...
                foreach (KeyValuePair<string, float> kvp in actualPoints)
                {
                    if (!sectionWeights.ContainsKey(kvp.Key))
                        sectionWeights.Add(kvp.Key, 0);
                }
            }
            // otherwise, just put everything on one section
            else
            {
                float unweightedActual = 0.0f;
                float unweightedTotal = 0.0f;

                foreach (KeyValuePair<string, float> kvp in actualPoints)
                {
                    unweightedActual += kvp.Value;
                    unweightedTotal += totalPoints[kvp.Key];
                }

                actualPoints.Clear();
                totalPoints.Clear();
                sectionWeights.Clear();

                actualPoints.Add("All", unweightedActual);
                totalPoints.Add("All", unweightedTotal);
                sectionWeights.Add("All", 1.0f);
            }

            currentActualPoints = actualPoints;
            currentTotalPoints = totalPoints;
            currentWeights = sectionWeights;

            masterCurrentActualPoints.Clear();
            masterCurrentTotalPoints.Clear();
            masterCurrentWeights.Clear();

            // deep copy; don't copy by reference
            foreach (KeyValuePair<string, float> kvp in currentActualPoints) masterCurrentActualPoints.Add(kvp.Key, kvp.Value);
            foreach (KeyValuePair<string, float> kvp in currentTotalPoints) masterCurrentTotalPoints.Add(kvp.Key, kvp.Value);
            foreach (KeyValuePair<string, float> kvp in currentWeights) masterCurrentWeights.Add(kvp.Key, kvp.Value);

            writeCurrents();
        }

        private void clearOut()
        {
            richTextBox2.Text = "";
        }

        private void writeLine(string str)
        {
            richTextBox2.Text += str + "\n";
        }

        private void writeCurrents()
        {
            float weightedScore = 0.0f;
            float weightedScoreTotalPossible = 0.0f;

            foreach (KeyValuePair<string, float> kvp in currentTotalPoints)
            {
                string key = kvp.Key;

                float total = currentTotalPoints[key];
                float actual = currentActualPoints[key];

                if (total < 0 && actual < 0)
                {
                    writeLine(key + ": - / -; N/A");
                    continue;
                }

                writeLine(key + ": " + actual + " / " + total + "; " + ((float)((int)((100.0f * actual / total) * 100 + 0.5f)) / 100.0f) + "%");

                weightedScore += (actual / total) * currentWeights[key];
                weightedScoreTotalPossible += currentWeights[key];
            }

            float overallScore = (weightedScore / weightedScoreTotalPossible);

            // get it to percentage form
            overallScore = overallScore * 100.0f;

            writeLine("");
            writeLine("Overall: " + overallScore + "%");
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void addAnAssignmentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InputBoxResult res = InputBox.Show("Enter the section: ", "Step 1 - Entry Section", "Assignments", new InputBoxValidatingHandler(inputBox_ValidatingSection));

            if (!res.OK)
                return;

            InputBoxResult res2 = InputBox.Show("Enter the total points: ", "Step 2 - Entry Total Points", "10.0", new InputBoxValidatingHandler(inputBox_ValidatingFloat));

            if (!res2.OK)
                return;

            InputBoxResult res3 = InputBox.Show("Enter the received points: ", "Step 3 - Entry Received Points", "0.0", new InputBoxValidatingHandler(inputBox_ValidatingFloat));

            if (!res3.OK)
                return;

            if (!currentTotalPoints.ContainsKey(res.Text))
            {
                currentTotalPoints.Add(res.Text, 0.0f);
                currentActualPoints.Add(res.Text, 0.0f);
            }
            else if (currentTotalPoints[res.Text] < 0.0) currentTotalPoints[res.Text] = currentActualPoints[res.Text] = 0;

            currentTotalPoints[res.Text] += Single.Parse(res2.Text);
            currentActualPoints[res.Text] += Single.Parse(res3.Text);

            clearOut();

            writeLine("MODIFIED: ");
            writeCurrents();
        }

        private void inputBox_Validating(object sender, InputBoxValidatingArgs e)
        {
            if (e.Text.Trim().Length == 0)
            {
                e.Cancel = true;
                e.Message = "Required";
            }
        }

        private void inputBox_ValidatingSection(object sender, InputBoxValidatingArgs e)
        {
            if (e.Text.Trim().Length == 0)
            {
                e.Cancel = true;
                e.Message = "Required";
            }

            if (!currentWeights.ContainsKey(e.Text))
            {
                e.Cancel = true;
                e.Message = "The specified section name does not exist.";
            }
        }

        private void inputBox_ValidatingFloat(object sender, InputBoxValidatingArgs e)
        {
            if (e.Text.Trim().Length == 0)
            {
                e.Cancel = true;
                e.Message = "Required";
            }

            float f;

            if (!Single.TryParse(e.Text, out f))
            {
                e.Cancel = true;
                e.Message = "Must be a number.  Decimals are allowed.";
            }
        }

        private void minimumToGetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InputBoxResult res = InputBox.Show("Enter the section: ", "Step 1 - Entry Section", "Assignments", new InputBoxValidatingHandler(inputBox_ValidatingSection));

            if (!res.OK)
                return;

            InputBoxResult res2 = InputBox.Show("Enter the total possible points for the entry: ", "Step 2 - Entry Total Points", "10.0", new InputBoxValidatingHandler(inputBox_ValidatingFloat));

            if (!res2.OK)
                return;

            InputBoxResult res3 = InputBox.Show("Enter minimum overall grade percentage: ", "Step 3 - Minimum Grade Percentage", "89.5", new InputBoxValidatingHandler(inputBox_ValidatingFloat));

            if (!res3.OK)
                return;

            List<string> usedWeights = new List<string>();
            float totalWeightDenominator = 0.0f; // not all sections are used

            foreach (KeyValuePair<string, float> kvp in currentWeights)
            {
                if (currentActualPoints[kvp.Key] >= 0)
                {
                    totalWeightDenominator += kvp.Value;
                    usedWeights.Add(kvp.Key);
                }
            }

            // if the section we want to check is not used, add it
            if (currentActualPoints[res.Text] < 0)
                totalWeightDenominator += currentWeights[res.Text];

            // do the heavy lifting:

            float desiredPercent = Single.Parse(res3.Text) / 100.0f * totalWeightDenominator;

            // find how much the chosen section has to change
            foreach (string str in usedWeights)
            {
                if (str != res.Text)
                {
                    desiredPercent -= currentWeights[str] * currentActualPoints[str] / currentTotalPoints[str];
                }
            }

            float sectionTotal = currentTotalPoints[res.Text];
            if (sectionTotal < 0) sectionTotal = 0;
            float sectionActual = currentActualPoints[res.Text];
            if (sectionActual < 0) sectionActual = 0;

            float entryTotal = Single.Parse(res2.Text);

            // note: desiredPercent is after all other weights have been subtracted from the true desired percent.
            // desiredPercent / sectionWeight = (sectionActual + x) / (sectionTotal + total)
            // x = (desiredPercent / sectionWeight) * (sectionTotal + entryTotal) - sectionActual

            float x = (desiredPercent / currentWeights[res.Text]) * (sectionTotal + entryTotal) - sectionActual;

            MessageBox.Show("Minimum score: " + x + " / " + entryTotal + (x > entryTotal ? " :(" : ""));
        }

        private void resetChangesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentActualPoints.Clear();
            currentTotalPoints.Clear();
            currentWeights.Clear();

            // deep copy; don't copy by reference
            foreach (KeyValuePair<string, float> kvp in masterCurrentActualPoints) currentActualPoints.Add(kvp.Key, kvp.Value);
            foreach (KeyValuePair<string, float> kvp in masterCurrentTotalPoints) currentTotalPoints.Add(kvp.Key, kvp.Value);
            foreach (KeyValuePair<string, float> kvp in masterCurrentWeights) currentWeights.Add(kvp.Key, kvp.Value);

            clearOut();
            writeCurrents();
        }

        private void howToUseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Instructions().Show();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox1().Show();
        }

        private void richTextBox1_Click(object sender, EventArgs e)
        {
            richTextBox1.SelectAll();
        }
    }
}
