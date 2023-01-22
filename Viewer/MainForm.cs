using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web.Mvc;
using System.Windows.Forms;
using CefSharp;
using Viewer2.Properties;
namespace Viewer2
{

	public class BrowserKeyboardHandler : IKeyboardHandler
	{
		private EventHandlerList _events;
		protected EventHandlerList Events
		{
			get
			{
				if (_events == null)
					_events = new EventHandlerList();
				return _events;
			}
		}

		public event KeyEventHandler RawKeyDownF11
		{
			add => Events.AddHandler("RawKeyDownF11", value);
			remove => Events.RemoveHandler("RawKeyDownF11", value);
		}

		public event KeyEventHandler RawKeyDownF5
		{
			add => Events.AddHandler("RawKeyDownF5", value);
			remove => Events.RemoveHandler("RawKeyDownF5", value);
		}

		public event KeyEventHandler RawKeyDownF12
		{
			add => Events.AddHandler("RawKeyDownF12", value);
			remove => Events.RemoveHandler("RawKeyDownF12", value);
		}

		public bool OnPreKeyEvent(IWebBrowser chromiumWebBrowser, IBrowser browser, KeyType type, int windowsKeyCode,
			int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey, ref bool isKeyboardShortcut)
		{
			return false;
		}

		public bool OnKeyEvent(IWebBrowser chromiumWebBrowser, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode,
			CefEventFlags modifiers, bool isSystemKey)
		{
			if (type == KeyType.RawKeyDown)
			{
				Delegate d = null;
				KeyEventArgs kea = null;
				if (nativeKeyCode == 4128769)
				{
					d = _events["RawKeyDownF5"];
					kea = new KeyEventArgs(Keys.F5);
				}
				else if (nativeKeyCode == 5767169)
				{
					d = _events["RawKeyDownF12"];
					kea = new KeyEventArgs(Keys.F12);
				}
				else if (nativeKeyCode == 5701633)
				{
					d = _events["RawKeyDownF11"];
					kea = new KeyEventArgs(Keys.F11);
				}

				if (d == null)
					return false;

				if (d.Target is Control c)
					c.BeginInvoke(d, new object[] {browser, kea});
				else
					return false;
			}

			return true;
		}
	}

	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
			var bkh = new BrowserKeyboardHandler();
			bkh.RawKeyDownF5 += MainFormRawKeyPress;
			bkh.RawKeyDownF12 += MainFormRawKeyPress;
			bkh.RawKeyDownF11 += MainFormRawKeyPress;
			Browser.KeyboardHandler = bkh;
			_openFile = Settings.Default.LastOpenFile;
			_isFullScreen = Settings.Default.IsFullScreen;
			UpdateBrowser();
		}

		private string _openFile { get; set; }
		private bool _isFullScreen { get; set; }

		public string CreateHtml(string pdfFile)
		{
			var css = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "viewer.css"));
			var fileReader = File.ReadAllBytes(pdfFile);
			var html = new TagBuilder("html");
			var htmlSb = new StringBuilder();
			var header = new TagBuilder("header") { InnerHtml = $"<link href='http://fonts.googleapis.com/css?family=Roboto' rel='stylesheet' type='text/css'><style>{css}</style>" };
			var body = new TagBuilder("body") { InnerHtml = PdfVisualizer.Process(fileReader).ToString() };

			htmlSb.Append(header);
			htmlSb.Append(body);

			html.InnerHtml = htmlSb.ToString();
			return html.ToString();
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
		}

		private void UpdateBrowser()
		{
			if (!File.Exists(_openFile))
				return;

			var html = CreateHtml(_openFile);
			Browser.LoadHtml(html, "https://localhost");
		}

		private void UpdateFormWindowState()
		{
			if (_isFullScreen)
			{
				WindowState = FormWindowState.Normal;
				FormBorderStyle = FormBorderStyle.None;
				Bounds = Screen.PrimaryScreen.Bounds;
			}
			else
			{
				WindowState = FormWindowState.Maximized;
				FormBorderStyle = FormBorderStyle.Sizable;
			}
		}

		private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
		{
			openFileDialog.InitialDirectory = @"D:\tmp\editor\Новая папка\";
			openFileDialog.Filter = "txt files (*.pdf)|*.|All files (*.*)|*.*";
			openFileDialog.FilterIndex = 2;
			openFileDialog.RestoreDirectory = true;

			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				Settings.Default.LastOpenFile = _openFile = openFileDialog.FileName;
				UpdateBrowser();
				Settings.Default.Save();
			}
		}

		private void DevToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Browser.ShowDevTools();
		}


		private void MainFormRawKeyPress(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.F5)
				UpdateBrowser();
			else if (e.KeyCode == Keys.F12)
				Browser.ShowDevTools();
			else if (e.KeyCode == Keys.F11)
			{
				Settings.Default.IsFullScreen = _isFullScreen = !_isFullScreen;
				UpdateFormWindowState();
				Settings.Default.Save();
			}


		}

		private void menuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{

		}
	}
}
