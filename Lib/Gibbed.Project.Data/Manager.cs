using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Gibbed.ProjectData
{

	public class Manager : IEnumerable<Project>, IEnumerable
	{

		private Manager()
		{
		}


		public Project ActiveProject
		{
			get
			{
				return this._ActiveProject;
			}
			set
			{
				if (value == null)
				{
					File.Delete(Path.Combine(this._ProjectPath, "current.txt"));
				}
				else
				{
					using (FileStream fileStream = File.Create(Path.Combine(this._ProjectPath, "current.txt")))
					{
						using (StreamWriter streamWriter = new StreamWriter(fileStream))
						{
							streamWriter.WriteLine(value.Name);
						}
					}
				}
				this._ActiveProject = value;
			}
		}


		public Project this[string name]
		{
			get
			{
				return this._Projects.SingleOrDefault((Project p) => p.Name.ToLowerInvariant() == name.ToLowerInvariant());
			}
		}


		public static Manager Load()
		{
			return Manager.Load(null);
		}


		public static Manager Load(string currentProject)
		{
			Manager manager = new Manager();
			string text = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			text = Path.Combine(text, "projects");
			manager._ProjectPath = text;
			if (Directory.Exists(text))
			{
				foreach (string path in Directory.GetFiles(text, "*.xml", SearchOption.TopDirectoryOnly))
				{
					manager._Projects.Add(Project.Create(path, manager));
				}
			}
			if (currentProject != null)
			{
				manager._ActiveProject = null;
				currentProject = currentProject.Trim();
				if (manager[currentProject] != null)
				{
					manager._ActiveProject = manager[currentProject];
				}
			}
			else
			{
				string path2 = Path.Combine(text, "current.txt");
				manager._ActiveProject = null;
				if (File.Exists(path2))
				{
					using (FileStream fileStream = File.OpenRead(path2))
					{
						StreamReader streamReader = new StreamReader(fileStream);
						string text2 = streamReader.ReadLine();
						if (text2 != null)
						{
							text2 = text2.Trim();
							if (manager[text2] != null)
							{
								manager._ActiveProject = manager[text2];
							}
						}
					}
				}
			}
			return manager;
		}


		public IEnumerator<Project> GetEnumerator()
		{
			return (from p in this._Projects
					where !p.Hidden && p.InstallPath != null
					select p).GetEnumerator();
		}


		IEnumerator IEnumerable.GetEnumerator()
		{
			return (from p in this._Projects
					where !p.Hidden && p.InstallPath != null
					select p).GetEnumerator();
		}


		public HashList<TType> LoadLists<TType>(string filter, Func<string, TType> hasher, Func<string, string> modifier)
		{
			HashList<TType> result;
			if (this.ActiveProject == null)
			{
				result = HashList<TType>.Dummy;
			}
			else
			{
				result = this.ActiveProject.LoadLists<TType>(filter, hasher, modifier);
			}
			return result;
		}


		public TType GetSetting<TType>(string name, TType defaultValue) where TType : struct
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			TType result;
			if (this.ActiveProject == null)
			{
				result = defaultValue;
			}
			else
			{
				result = this.ActiveProject.GetSetting<TType>(name, defaultValue);
			}
			return result;
		}


		private string _ProjectPath;


		private readonly List<Project> _Projects = new List<Project>();


		private Project _ActiveProject;
	}
}
