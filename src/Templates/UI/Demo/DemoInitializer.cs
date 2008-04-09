using System;
using System.Configuration;
using System.IO;
using System.Web;
using N2;
using N2.Plugin;
using N2.Serialization;
using N2.Templates.Items;

namespace Demo
{
	[AutoInitialize]
	public class DemoInitializer : IPluginInitializer
	{
		#region IPlugInInitializer Members

		public void Initialize(N2.Engine.IEngine factory)
		{
			if (ConfigurationManager.AppSettings["ResetData"] == "true")
			{
				ReplaceContent(factory);

				CopyFiles(factory);

				N2.Edit.FileManagement.FileManager.FileUploading += FileManager_FileUploading;
			}
		}

		private static void CopyFiles(N2.Engine.IEngine factory)
		{
			HttpServerUtility server = HttpContext.Current.Server;
			string upload = server.MapPath(factory.EditManager.GetUploadFolderUrl());
			DeleteFilesAndFolders(upload);

			File.Copy(server.MapPath("~/Demo/Resources/Sunset.jpg"), server.MapPath("~/upload/Sunset.jpg"), true);
			File.Copy(server.MapPath("~/Demo/Resources/n2.gif"), server.MapPath("~/upload/n2.gif"), true);
			File.Copy(server.MapPath("~/Demo/Resources/lav.jpg"), server.MapPath("~/upload/lav.jpg"), true);
			File.Copy(server.MapPath("~/Demo/Resources/skal.jpg"), server.MapPath("~/upload/skal.jpg"), true);
			File.Copy(server.MapPath("~/Demo/Resources/thorn.jpg"), server.MapPath("~/upload/thorn.jpg"), true);
		}

		public static void FileManager_FileUploading(object sender, N2.Edit.FileManagement.FileEventArgs e)
		{
			if (!System.Text.RegularExpressions.Regex.IsMatch(e.FileName, "\\.gif$|\\.jpg$|\\.jpeg$|\\.png$|\\.txt$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
				e.Cancel = true;
		}

		private static void DeleteFilesAndFolders(string upload)
		{
			foreach (string path in Directory.GetFiles(upload))
				File.Delete(path);
			foreach (string path in Directory.GetDirectories(upload))
				Directory.Delete(path, true);
		}

		private void ReplaceContent(N2.Engine.IEngine factory)
		{
			Importer imp = factory.Resolve<Importer>();
			IImportRecord record;
			HttpContext context = HttpContext.Current;
			if (context != null && File.Exists(context.Server.MapPath("~/App_Data/export.n2.xml.gz")))
			{
				record = imp.Read(context.Server.MapPath("~/App_Data/export.n2.xml.gz"));
			}
			else
			{
				record = imp.Read(GetType().Assembly.GetManifestResourceStream("Demo.Resources.export.n2.xml.gz"), "export.n2.xml.gz");
			}

			ContentItem imported = record.RootItem;

			AbstractRootPage rootPage = factory.Persister.Get<AbstractRootPage>(factory.UrlParser.CurrentSite.RootItemID);

			factory.SecurityManager.ScopeEnabled = false;
			((N2.Integrity.IntegrityEnforcer)factory.Resolve<N2.Integrity.IIntegrityEnforcer>()).Enabled = false;
			RemoveExistingPages(factory, rootPage);
			((N2.Integrity.IntegrityEnforcer)factory.Resolve<N2.Integrity.IIntegrityEnforcer>()).Enabled = true;
			UpdateRootPage(factory, imported, rootPage);

			imp.Import(record, rootPage, ImportOption.Children);

			foreach(ContentItem item in rootPage.Children)
				if (item is AbstractStartPage)
					factory.Resolve<N2.Web.Site>().StartPageID = item.ID;

			factory.Persister.Save(rootPage);

			//foreach (NewsList nl in Find.Items.Where.Type.Eq(typeof(NewsList)).Select())
			//{
			//    foreach (NewsContainer nc in Find.Items.Where.Type.Eq(typeof (NewsContainer)).Select())
			//    {
			//        nl.Container = nc;
			//        News n = factory.Definitions.CreateInstance<News>(nc);
			//        n.Title = "Demo site created";
			//        n.Introduction = "Today at " + DateTime.Now + " a demo site was generated for your convenience.";
			//        n.Text = "<p>Download the template web if you like.</p>";
			//        n["Syndicate"] = true;
			//        factory.Persister.Save(n);
			//    }
			//}

			//foreach (CalendarTeaser ct in Find.Items.Where.Type.Eq(typeof(CalendarTeaser)).Select())
			//{
			//    foreach (Calendar c in Find.Items.Where.Type.Eq(typeof(Calendar)).Select())
			//    {
			//        ct.Container = c;
			//        Event e = factory.Definitions.CreateInstance<Event>(c);
			//        e.Title = "Demo site scheduled for deletion";
			//        e.Introduction = "30 minutes from now the demo site will be re-created.";
			//        e.EventDate = DateTime.Now.AddMinutes(30);
			//        e["Syndicate"] = true;
			//        factory.Persister.Save(e);
			//    }
			//}

			ClearPreviousVersions(factory, rootPage);

			factory.SecurityManager.ScopeEnabled = true;
		}

		private static void RemoveExistingPages(N2.Engine.IEngine factory, ContentItem rootPage)
		{
			while (rootPage.Children.Count > 0)
				factory.Persister.Delete(rootPage.Children[0]);
		}

		private static void UpdateRootPage(N2.Engine.IEngine factory, ContentItem imported, ContentItem startPage)
		{
			startPage.Title = imported.Title;
			startPage.Name = imported.Name;
			foreach (N2.Details.ContentDetail detail in imported.Details.Values)
				startPage[detail.Name] = detail.Value;
			factory.Persister.Save(startPage);
		}

		private static void ClearPreviousVersions(N2.Engine.IEngine factory, ContentItem rootPage)
		{
			foreach (ContentItem version in factory.Resolve<N2.Persistence.Finder.IItemFinder>().Where.VersionOf.Eq(rootPage).Select())
				factory.Persister.Delete(version);
		}

		#endregion
	}
}
