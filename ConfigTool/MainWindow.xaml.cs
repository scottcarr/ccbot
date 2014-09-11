/*
  ReviewBot 0.1
  Copyright (c) Microsoft Corporation
  All rights reserved. 
  
  MIT License
  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
  The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
  THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace Microsoft.Research.ReviewBot.ReviewBotConfigTool
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
    }

    #region File menu click event handlers
    private void Open_Click(object sender, RoutedEventArgs e)
    {
      var dialog = new OpenFileDialog();
      dialog.Filter = "XML File|*.xml";
      if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        var path = dialog.FileName;
        var text = File.ReadAllText(path);
        MyTextBox.Text = text;
        IsValidConfig(text);
      }
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
      Configuration config;
      if (!TryGetCurrentConfig(out config)) { Messages.Text = "Can't save an invalid configuration."; return; }
      if (!IsValidConfig(config)) { Messages.Text = "Can't save an invalid configuration."; return; }
      var dialog = new SaveFileDialog();
      dialog.Filter = "XML File|*.xml";
      if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        var path = dialog.FileName;
        var xs = new XmlSerializer(typeof(Configuration));
        var file = File.Create(path);
        xs.Serialize(file, config);
        file.Flush();
        file.Close();
      }
    }
    #endregion File menu click event handlers
    private bool TryGetConfigurationFromString(string text, out Configuration config)
    {
      var ms = new MemoryStream();
      var sw = new StreamWriter(ms);
      sw.Write(text);
      sw.Flush();
      ms.Position = 0;
      var sr = new StreamReader(ms);
      var xs = new XmlSerializer(typeof(Configuration));
      try
      {
        config = xs.Deserialize(sr) as Configuration;
        return true;
      }
      catch (InvalidOperationException)
      {
        Messages.Text = "The configuration is invalid";
        config = null;
        return false;
      }
    }
    private bool IsValidConfig(string text)
    {
      Configuration config;
      if (!TryGetConfigurationFromString(text, out config)) { return false; }
      return IsValidConfig(config);
    }
    private void ReportConfigError(string message, string name = null)
    {
      if(!String.IsNullOrEmpty(name))
      {
        message = string.Format("{0} ({1})", message, name);
      }
      Messages.AppendText(message);
    }
    private bool IsValidConfig(Configuration config)
    {
      Messages.Text = "";
      if (String.IsNullOrEmpty(config.Cccheck) || !File.Exists(config.Cccheck) || !config.Cccheck.EndsWith(".exe")) 
      {
        ReportConfigError("Given cccheck.exe path doesn't exist.", config.Cccheck);
        return false; 
      }
      if (String.IsNullOrEmpty(config.Git) || !File.Exists(config.Git) || !config.Git.EndsWith(".exe")) 
      { 
        ReportConfigError("Given git.exe path doesn't exist.", config.Git);
        return false; 
      }
      if (!Directory.Exists(config.GitRoot)) 
      { 
        ReportConfigError("Given git root directory doesn't exist.", config.GitRoot);
        return false; 
      }
      if (!Directory.Exists(config.GitRoot + "\\.git"))
      {
        ReportConfigError("Given git root directory isn't really a git repo", config.GitRoot + "\\.git");
        return false; 
      }
      if (String.IsNullOrEmpty(config.MSBuild) || !File.Exists(config.MSBuild) || !config.MSBuild.EndsWith(".exe")) 
      { 
        ReportConfigError("Given msbuild.exe path doesn't exist.", config.MSBuild);
        return false; 
      }
      if (String.IsNullOrEmpty(config.Project) ||!File.Exists(config.Project) || !config.Project.EndsWith(".csproj")) 
      { 
        ReportConfigError("Given *.csproj path doesn't exist.", config.Project);
        return false; 
      }
      if (String.IsNullOrEmpty(config.RSP) || !File.Exists(config.RSP) || !config.RSP.EndsWith(".rsp")) 
      { 
        ReportConfigError("Given *.rsp path doesn't exist.", config.RSP);
        return false; 
      }
      if (String.IsNullOrEmpty(config.Solution) || !File.Exists(config.Solution) || !config.Solution.EndsWith(".sln")) 
      { 
        ReportConfigError("Given *.sln path doesn't exist.", config.Solution);
        return false; 
      }
      Messages.Text = "The configuration seems valid.";
      return true;
    }
    #region Configuration change click event handlers
    private void Solution_Click(object sender, RoutedEventArgs e)
    {
      Configuration config;
      if (!TryGetCurrentConfig(out config)) { return; }
      var dialog = new OpenFileDialog();
      dialog.Filter = "Visual Studio Solution|*.sln";
      if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && File.Exists(dialog.FileName))
      {
        config.Solution = dialog.FileName;
        MyTextBox.Text = GetConfigText(config);
        IsValidConfig(config);
      }
    }
    private void Project_Click(object sender, RoutedEventArgs e)
    {
      Configuration config;
      if (!TryGetCurrentConfig(out config)) { return; }
      var dialog = new OpenFileDialog();
      dialog.Filter = "Visual Studio Project|*.csproj";
      if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && File.Exists(dialog.FileName))
      {
        config.Project = dialog.FileName;
        MyTextBox.Text = GetConfigText(config);
        IsValidConfig(config);
      }
    }
    private void GitRoot_Click(object sender, RoutedEventArgs e)
    {
      Configuration config;
      if (!TryGetCurrentConfig(out config)) { return; }
      var dialog = new FolderBrowserDialog();
      if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && Directory.Exists(dialog.SelectedPath))
      {
        config.GitRoot = dialog.SelectedPath;
        MyTextBox.Text = GetConfigText(config);
        IsValidConfig(config);
      }
    }
    private void Git_Click(object sender, RoutedEventArgs e)
    {
      Configuration config;
      if (!TryGetCurrentConfig(out config)) { return; }
      var dialog = new OpenFileDialog();
      dialog.Filter = "Git executable|git.exe";
      if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && File.Exists(dialog.FileName))
      {
        config.Git = dialog.FileName;
        MyTextBox.Text = GetConfigText(config);
        IsValidConfig(config);
      }
    }
    private void MSBuild_Click(object sender, RoutedEventArgs e)
    {
      Configuration config;
      if (!TryGetCurrentConfig(out config)) { return; }
      var dialog = new OpenFileDialog();
      dialog.Filter = "MSBuild executable|msbuild.exe";
      if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && File.Exists(dialog.FileName))
      {
        config.MSBuild = dialog.FileName;
        MyTextBox.Text = GetConfigText(config);
        IsValidConfig(config);
      }
    }
    private void RSP_Click(object sender, RoutedEventArgs e)
    {
      Configuration config;
      if (!TryGetCurrentConfig(out config)) { return; }
      var dialog = new OpenFileDialog();
      dialog.Filter = "Response file|*.rsp";
      if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && File.Exists(dialog.FileName))
      {
        config.RSP = dialog.FileName;
        MyTextBox.Text = GetConfigText(config);
        IsValidConfig(config);
      }
    }
    #endregion Configuration change click event handlers
    private bool TryGetCurrentConfig(out Configuration config)
    {
      var text = MyTextBox.Text;
      return TryGetConfigurationFromString(text, out config);
    }
    private string GetConfigText(Configuration config)
    {
      var ms = new MemoryStream();
      var xs = new XmlSerializer(typeof(Configuration));
      xs.Serialize(ms, config);
      ms.Position = 0;
      var sr = new StreamReader(ms);
      return sr.ReadToEnd();
    }
    private void TextChanged(object sender, TextChangedEventArgs e)
    {
      if (MyTextBox == null || Messages == null) { return; }
      if (IsValidConfig(MyTextBox.Text))
      ConfigMenu.IsEnabled = true;
    }
  }
}
