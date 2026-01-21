using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Xml.XPath;
using Microsoft.Win32;

namespace Gibbed.ProjectData
{

	public sealed class Project
	{

		private Project()
		{
			this.Dependencies = new List<string>();
			this.Settings = new Dictionary<string, string>();
		}


		public string Name { get; private set; }


		public bool Hidden { get; private set; }


		public string InstallPath { get; private set; }


		public string ListsPath { get; private set; }


		internal List<string> Dependencies { get; private set; }


		internal Dictionary<string, string> Settings { get; private set; }


		internal static Project Create(string path, Manager manager)
		{
			path = Path.GetFullPath(path);
			if (path == null)
			{
				throw new InvalidOperationException();
			}
			string directoryName = Path.GetDirectoryName(path);
			if (directoryName == null)
			{
				throw new InvalidOperationException();
			}
			Project project = new Project
			{
				Manager = manager
			};
			XPathDocument xpathDocument = new XPathDocument(path);
			XPathNavigator xpathNavigator = xpathDocument.CreateNavigator();
			XPathNavigator xpathNavigator2 = xpathNavigator.SelectSingleNode("/project/name");
			if (xpathNavigator2 == null)
			{
				throw new InvalidOperationException();
			}
			project.Name = xpathNavigator2.Value;
			XPathNavigator xpathNavigator3 = xpathNavigator.SelectSingleNode("/project/list_location");
			if (xpathNavigator3 == null)
			{
				throw new InvalidOperationException();
			}
			project.ListsPath = xpathNavigator3.Value;
			project.Hidden = (xpathNavigator.SelectSingleNode("/project/hidden") != null);
			if (!Path.IsPathRooted(project.ListsPath))
			{
				project.ListsPath = Path.Combine(directoryName, project.ListsPath);
			}
			project.Dependencies.Clear();
			XPathNodeIterator xpathNodeIterator = xpathNavigator.Select("/project/dependencies/dependency");
			while (xpathNodeIterator.MoveNext() && xpathNodeIterator.Current != null)
			{
				project.Dependencies.Add(xpathNodeIterator.Current.Value);
			}
			project.Settings.Clear();
			XPathNodeIterator xpathNodeIterator2 = xpathNavigator.Select("/project/settings/setting");
			while (xpathNodeIterator2.MoveNext() && xpathNodeIterator2.Current != null)
			{
				string attribute = xpathNodeIterator2.Current.GetAttribute("name", "");
				string value = xpathNodeIterator2.Current.Value;
				if (string.IsNullOrWhiteSpace(attribute))
				{
					throw new InvalidOperationException("setting name cannot be empty");
				}
				project.Settings[attribute.ToLowerInvariant()] = value;
			}
			project.InstallPath = null;
			XPathNodeIterator xpathNodeIterator3 = xpathNavigator.Select("/project/install_locations/install_location");
			while (xpathNodeIterator3.MoveNext() && xpathNodeIterator3.Current != null)
			{
				bool flag = true;
				XPathNodeIterator xpathNodeIterator4 = xpathNodeIterator3.Current.Select("action");
				string text = null;
				while (xpathNodeIterator4.MoveNext() && xpathNodeIterator4.Current != null)
				{
					string attribute2 = xpathNodeIterator4.Current.GetAttribute("type", "");
					if (attribute2 != null)
					{
						if (!(attribute2 == "registry"))
						{
							if (!(attribute2 == "registryview"))
							{
								if (!(attribute2 == "path"))
								{
									if (!(attribute2 == "combine"))
									{
										if (!(attribute2 == "directory_name"))
										{
											if (!(attribute2 == "fix"))
											{
												goto IL_440;
											}
											text = text.Replace('/', '\\');
											flag = false;
										}
										else
										{
											text = Path.GetDirectoryName(text);
											if (Directory.Exists(text))
											{
												flag = false;
											}
										}
									}
									else
									{
										text = Path.Combine(text, xpathNodeIterator4.Current.Value);
										if (Directory.Exists(text))
										{
											flag = false;
										}
									}
								}
								else
								{
									text = xpathNodeIterator4.Current.Value;
									if (Directory.Exists(text))
									{
										flag = false;
									}
								}
							}
							else
							{
								RegistryView view;
								if (!Enum.TryParse<RegistryView>(xpathNodeIterator4.Current.GetAttribute("view", ""), out view))
								{
									throw new InvalidOperationException();
								}
								RegistryHive hKey;
								if (!Enum.TryParse<RegistryHive>(xpathNodeIterator4.Current.GetAttribute("hive", ""), out hKey))
								{
									throw new InvalidOperationException();
								}
								try
								{
									RegistryKey registryKey = RegistryKey.OpenBaseKey(hKey, view);
									string attribute3 = xpathNodeIterator4.Current.GetAttribute("subkey", "");
									registryKey = registryKey.OpenSubKey(attribute3);
									if (registryKey != null)
									{
										string attribute4 = xpathNodeIterator4.Current.GetAttribute("value", "");
										string text2 = (string)registryKey.GetValue(attribute4, null);
										if (!string.IsNullOrEmpty(text2))
										{
											text = text2;
											flag = false;
										}
									}
								}
								catch (SecurityException)
								{
									flag = true;
								}
							}
						}
						else
						{
							string attribute5 = xpathNodeIterator4.Current.GetAttribute("key", "");
							string attribute6 = xpathNodeIterator4.Current.GetAttribute("value", "");
							try
							{
								string text3 = (string)Registry.GetValue(attribute5, attribute6, null);
								if (text3 != null)
								{
									text = text3;
									flag = false;
								}
							}
							catch (SecurityException)
							{
								flag = true;
								throw;
							}
						}
						if (flag)
						{
							break;
						}
						continue;
					}
				IL_440:
					throw new InvalidOperationException("unhandled install location action type");
				}
				if (!flag && Directory.Exists(text))
				{
					project.InstallPath = text;
					break;
				}
			}
			return project;
		}


		public override string ToString()
		{
			return this.Name;
		}


		public TType GetSetting<TType>(string name, TType defaultValue) where TType : struct
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			name = name.ToLowerInvariant();
			TType result;
			if (!this.Settings.ContainsKey(name))
			{
				result = defaultValue;
			}
			else
			{
				Type typeFromHandle = typeof(TType);
				if (typeFromHandle.IsEnum)
				{
					TType ttype;
					if (!Enum.TryParse<TType>(this.Settings[name], out ttype))
					{
						throw new ArgumentException("bad enum value", "name");
					}
					result = ttype;
				}
				else
				{
					result = (TType)((object)Convert.ChangeType(this.Settings[name], typeFromHandle));
				}
			}
			return result;
		}


		public HashList<TType> LoadLists<TType>(string filter, Func<string, TType> hasher, Func<string, string> modifier)
		{
			HashList<TType> hashList = new HashList<TType>();
			foreach (string name in this.Dependencies)
			{
				Project project = this.Manager[name];
				if (project != null)
				{
					Project.LoadListsFrom<TType>(project.ListsPath, filter, hasher, modifier, hashList);
				}
			}
			Project.LoadListsFrom<TType>(this.ListsPath, filter, hasher, modifier, hashList);
			return hashList;
		}


		private static void LoadListsFrom<TType>(string basePath, string filter, Func<string, TType> hasher, Func<string, string> modifier, HashList<TType> list)
		{
			if (Directory.Exists(basePath))
			{
				foreach (string path in Directory.GetFiles(basePath, filter, SearchOption.AllDirectories))
				{
					using (FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					{
						StreamReader streamReader = new StreamReader(fileStream);
						string text;
						TType key;
						for (; ; )
						{
							text = streamReader.ReadLine();
							if (text == null)
							{
								break;
							}
							if (!text.StartsWith(";"))
							{
								text = text.Trim();
								if (text.Length > 0)
								{
									if (modifier != null)
									{
										text = modifier(text);
									}
									key = hasher(text);
									if (list.Lookup.ContainsKey(key) && list.Lookup[key] != text)
									{
										goto Block_9;
									}
									list.Lookup[key] = text;
								}
							}
						}
						goto IL_11D;
					Block_9:
						string arg = list.Lookup[key];
						throw new InvalidOperationException(string.Format("hash collision ('{0}' vs '{1}')", text, arg));
					}
				IL_11D:;
				}
			}
		}


		internal Manager Manager;
	}
}
