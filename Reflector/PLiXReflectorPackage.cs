/**************************************************************************\
* Neumont PLiX (Programming Language in XML) Code Generator                *
*                                                                          *
* Copyright � Neumont University and Matthew Curland. All rights reserved. *
* Copyright � The ORM Foundation. All rights reserved.                     *
*                                                                          *
* The use and distribution terms for this software are covered by the      *
* Common Public License 1.0 (http://opensource.org/licenses/cpl) which     *
* can be found in the file CPL.txt at the root of this distribution.       *
* By using this software in any fashion, you are agreeing to be bound by   *
* the terms of this license.                                               *
*                                                                          *
* You must not remove this notice, or any other, from this software.       *
\**************************************************************************/
using System;
using System.Collections.Generic;
using System.Text;
using Reflector;
using Reflector.CodeModel;

namespace Reflector
{
	/// <summary>
	/// Options for rendering static calls to the current type
	/// as &lt;plx:callThis accessor="static"/&gt; instead of
	/// &lt;plx:callStatic/&gt;.
	/// </summary>
	public enum StaticCallRenderingOption
	{
		/// <summary>
		/// All static calls should be explicitly renders with callStatic
		/// </summary>
		Explicit,
		/// <summary>
		/// All static calls to the current type should be rendered with callThis
		/// </summary>
		ImplicitCurrentType,
		/// <summary>
		/// All static calls to the current type and any of its base types should be rendered with callThis
		/// </summary>
		ImplicitBaseTypes,
	}
	/// <summary>
	/// Interface for providing PLiX configuration options
	/// </summary>
	public interface IPLiXConfiguration
	{
		/// <summary>
		/// Get the current example language. Can be null.
		/// </summary>
		ILanguage ExampleLanguage { get;}
		/// <summary>
		/// Return true to include method implementations as part of the type declaration
		/// </summary>
		bool FullyExpandTypeDeclarations { get;}
		/// <summary>
		/// Return true to include method implementations as part of a namespace declaration
		/// </summary>
		bool FullyExpandNamespaceDeclarations { get;}
		/// <summary>
		/// Return true to write the dataTypeQualifier attribute even if it matches the current active item
		/// </summary>
		bool DisplayContextDataTypeQualifier { get;}
		/// <summary>
		/// Return the current rendering option for static calls to
		/// the current type.
		/// </summary>
		StaticCallRenderingOption StaticCallRenderingOption { get;}
	}
	/// <summary>
	/// Starting point for a Reflector class
	/// </summary>
	public sealed class PLiXLanguagePackage : IPackage
	{
		#region PLiXConfiguration class
		private sealed class PLiXConfiguration : IPLiXConfiguration
		{
			#region Member Variables
			private IConfiguration myConfiguration;
			private string myExampleLanguageName;
			private int myExampleLanguageIndex;
			private ILanguage myExampleLanguage;
			private bool myFullyExpandTypeDeclarations;
			private bool myFullyExpandCurrentTypeDeclaration;
			private bool myFullyExpandCurrentNamespaceDeclaration;
			private bool myDisplayContextDataTypeQualifier;
			private StaticCallRenderingOption myStaticCallOption;
			private PLiXLanguagePackage myPackage;
			#endregion // Member Variables
			#region Constants
			private const string ConfigurationSection = "PLiXLanguage";
			private const string ExampleLanguageValueName = "ExampleLanguage";
			private const string FullyExpandTypeDeclarationsValueName = "FullyExpandTypeDeclarations";
			private const string DisplayContextDataTypeQualifierValueName = "DisplayContextDataTypeQualifier";
			private const string StaticCallRendingOptionValueName = "StaticCallRenderingOption";
			#endregion // Constants
			#region Constructors
			/// <summary>
			/// Create a new PLiXConfiguration class
			/// </summary>
			/// <param name="package">The associated PLiXLanguagePackage</param>
			public PLiXConfiguration(PLiXLanguagePackage package)
			{
				myPackage = package;
				IConfigurationManager configManager = (IConfigurationManager)package.myServiceProvider.GetService(typeof(IConfigurationManager));
				myConfiguration = configManager[ConfigurationSection];

				myExampleLanguageName = myConfiguration.GetProperty(ExampleLanguageValueName, "C#");
				myFullyExpandTypeDeclarations = 0 == string.Compare(myConfiguration.GetProperty(FullyExpandTypeDeclarationsValueName, "false"), "true", StringComparison.CurrentCultureIgnoreCase);
				myDisplayContextDataTypeQualifier = 0 == string.Compare(myConfiguration.GetProperty(DisplayContextDataTypeQualifierValueName, "false"), "true", StringComparison.CurrentCultureIgnoreCase);
				myStaticCallOption = StaticCallRenderingOption.Explicit;
				string callStaticOptionValue = myConfiguration.GetProperty(StaticCallRendingOptionValueName, "Explicit");
				if (0 == string.Compare(callStaticOptionValue, "ImplicitCurrentType", StringComparison.CurrentCultureIgnoreCase))
				{
					myStaticCallOption = StaticCallRenderingOption.ImplicitCurrentType;
				}
				else if (0 == string.Compare(callStaticOptionValue, "ImplicitBaseTypes", StringComparison.CurrentCultureIgnoreCase))
				{
					myStaticCallOption = StaticCallRenderingOption.ImplicitBaseTypes;
				}
			}
			#endregion // Constructors
			#region IPLiXConfiguration Implementation
			public ILanguage ExampleLanguage
			{
				get
				{
					ILanguage retVal = myExampleLanguage;
					ILanguageCollection languages = myPackage.myLanguageManager.Languages;
					if (retVal == null)
					{
						string languageName = myExampleLanguageName;
						if (!string.IsNullOrEmpty(languageName))
						{
							int languageCount = languages.Count;
							for (int i = 0; i < languageCount; ++i)
							{
								ILanguage testLanguage = languages[i];
								if (testLanguage.Name == languageName)
								{
									myExampleLanguage = retVal = testLanguage;
									myExampleLanguageIndex = i;
									break;
								}
							}
							if (retVal == null)
							{
								myConfiguration.SetProperty(ExampleLanguageValueName, "");
							}
						}
					}
					else
					{
						// There is no notification when this set changes, so make a sanity
						// check by verifying that the language at the recorded index has not changed.
						int languageCount = languages.Count;
						int testIndex = myExampleLanguageIndex;
						if (!(testIndex < languageCount && languages[testIndex] == retVal))
						{
							int i = 0;
							for (; i < languageCount; ++i)
							{
								if (languages[i] == retVal)
								{
									myExampleLanguageIndex = i;
									break;
								}
							}
							if (i == languageCount)
							{
								myExampleLanguageName = "";
								myExampleLanguage = retVal = null;
							}
						}
					}
					return retVal;
				}
				set
				{
					if (value != ExampleLanguage)
					{
						myExampleLanguage = value;
						string languageName = (value != null) ? value.Name : "";
						myExampleLanguageName = languageName;
						myConfiguration.SetProperty(ExampleLanguageValueName, languageName);
						RefreshCurrentSelection();
						// myExampleLanguageIndex will refresh itself automatically
					}
				}
			}
			public bool FullyExpandTypeDeclarations
			{
				get
				{
					if (myFullyExpandCurrentTypeDeclaration && myPackage.myAssemblyBrowser.ActiveItem == myPackage.myLastActiveItem)
					{
						return true;
					}
					return myFullyExpandTypeDeclarations;
				}
				set
				{
					if (value != myFullyExpandTypeDeclarations)
					{
						myFullyExpandTypeDeclarations = value;
						myConfiguration.SetProperty(FullyExpandTypeDeclarationsValueName, value ? "true" : "false");
						if (value && myFullyExpandCurrentTypeDeclaration)
						{
							// No refresh is necessary, we already have the correct state
							myFullyExpandCurrentTypeDeclaration = false;
						}
						else if (myPackage.myAssemblyBrowser.ActiveItem is ITypeDeclaration)
						{
							RefreshCurrentSelection();
						}
					}
				}
			}
			public bool FullyExpandNamespaceDeclarations
			{
				get
				{
					return myFullyExpandCurrentNamespaceDeclaration && myPackage.myAssemblyBrowser.ActiveItem == myPackage.myLastActiveItem;
				}
			}
			public bool FullyExpandCurrentTypeDeclaration
			{
				get
				{
					return myFullyExpandCurrentTypeDeclaration;
				}
				set
				{
					if (value != myFullyExpandCurrentTypeDeclaration)
					{
						myFullyExpandCurrentTypeDeclaration = value;
						if (value)
						{
							RefreshCurrentSelection();
						}
					}
				}
			}
			public bool FullyExpandCurrentNamespaceDeclaration
			{
				get
				{
					return myFullyExpandCurrentNamespaceDeclaration;
				}
				set
				{
					if (value != myFullyExpandCurrentNamespaceDeclaration)
					{
						myFullyExpandCurrentNamespaceDeclaration = value;
						if (value)
						{
							RefreshCurrentSelection();
						}
					}
				}
			}
			public bool DisplayContextDataTypeQualifier
			{
				get
				{
					return myDisplayContextDataTypeQualifier;
				}
				set
				{
					if (value != myDisplayContextDataTypeQualifier)
					{
						myDisplayContextDataTypeQualifier = value;
						myConfiguration.SetProperty(DisplayContextDataTypeQualifierValueName, value ? "true" : "false");
						RefreshCurrentSelection();
					}
				}
			}
			public StaticCallRenderingOption StaticCallRenderingOption
			{
				get
				{
					return myStaticCallOption;
				}
				set
				{
					if (value != myStaticCallOption)
					{
						myStaticCallOption = value;
						myConfiguration.SetProperty(StaticCallRendingOptionValueName, value.ToString());
						RefreshCurrentSelection();
					}
				}
			}
			/// <summary>
			/// Force the disassembler page to refresh
			/// </summary>
			private void RefreshCurrentSelection()
			{
				// Forces rerendering of the selected item
				myPackage.myLanguageManager.ActiveLanguage = myPackage.myLanguageManager.ActiveLanguage;
			}
			#endregion // IPLiXConfiguration Implementation
		}
		#endregion // PLiXConfiguraiton class
		#region Member Variables
		private ILanguageManager myLanguageManager;
		private ILanguage myLanguage;
		private IAssemblyBrowser myAssemblyBrowser;
		private IServiceProvider myServiceProvider;
		private ICommandBarMenu myTopMenu;
		private ICommandBarMenu myExampleLanguageMenu;
		private ICommandBarItem myExpandCurrentTypeDeclarationButton;
		private ICommandBarItem myExpandCurrentNamespaceDeclarationButton;
		private ICommandBarCheckBox myFullyExpandTypeDeclarationsCheckBox;
		private ICommandBarCheckBox myDisplayContextDataTypeQualifierCheckBox;
		private ICommandBarCheckBox myExplicitStaticCallCheckBox;
		private ICommandBarCheckBox myImplicitCurrentTypeStaticCallCheckBox;
		private ICommandBarCheckBox myImplicitBaseTypesStaticCallCheckBox;
		private IPLiXConfiguration myConfiguration;
		private object myLastActiveItem;
		#endregion // Member Variables
		#region IPackage Implementation
		void IPackage.Load(IServiceProvider serviceProvider)
		{
			// Set this early so it is easily referenced
			myServiceProvider = serviceProvider;

			IAssemblyBrowser assemblyBrowser = (IAssemblyBrowser)serviceProvider.GetService(typeof(IAssemblyBrowser));
			assemblyBrowser.ActiveItemChanged += new EventHandler(OnActiveItemChanged);
			myLastActiveItem = assemblyBrowser.ActiveItem;
			myAssemblyBrowser = assemblyBrowser;
			ILanguageManager languageManager = (ILanguageManager)serviceProvider.GetService(typeof(ILanguageManager));
			myLanguageManager = languageManager;
			myConfiguration = new PLiXConfiguration(this);
			languageManager.ActiveLanguageChanged += new EventHandler(OnActiveLanguageChanged);
			ILanguage language = new PLiXLanguage((ITranslatorManager)serviceProvider.GetService(typeof(ITranslatorManager)), myConfiguration);
			languageManager.RegisterLanguage(language);
			myLanguage = language;

			// Add our PLiX menu item, activated when the plix language is active
			ICommandBarManager commandBarManager = (ICommandBarManager)serviceProvider.GetService(typeof(ICommandBarManager));
			ICommandBar menuBar = commandBarManager.CommandBars["MenuBar"];
			ICommandBarMenu topMenu = menuBar.Items.InsertMenu(menuBar.Items.Count - 1, "PLiXLanguageOptions", "PLi&X");
			topMenu.Visible = false;
			topMenu.DropDown += new EventHandler(OnOpenTopMenu);

			ICommandBarItemCollection menuItems = topMenu.Items;
			myExampleLanguageMenu = menuItems.AddMenu("PLiXExampleLanguage", "&Example Language");
			menuItems.AddSeparator();
			myExpandCurrentNamespaceDeclarationButton = menuItems.AddButton("E&xpand Current Namespace Declaration", new EventHandler(OnExpandCurrentNamespaceDeclaration));
			myExpandCurrentTypeDeclarationButton = menuItems.AddButton("E&xpand Current Type Declaration", new EventHandler(OnExpandCurrentTypeDeclaration));
			(myFullyExpandTypeDeclarationsCheckBox = menuItems.AddCheckBox("Ex&pand All Type Declarations")).Click += new EventHandler(OnFullyExpandTypeDeclarationsChanged);
			menuItems.AddSeparator();
			(myDisplayContextDataTypeQualifierCheckBox = menuItems.AddCheckBox("Display Context Type &Qualifier")).Click += new EventHandler(OnDisplayContextDataTypeQualifierChanged);
			ICommandBarMenu callStaticOptionsMenu = menuItems.AddMenu("PLiXStaticCallOptions", "&Static Call Options");
			menuItems = callStaticOptionsMenu.Items;
			(myExplicitStaticCallCheckBox = menuItems.AddCheckBox("&Explicit")).Click += new EventHandler(OnExplicitStaticCallCheckBoxChanged);
			(myImplicitCurrentTypeStaticCallCheckBox = menuItems.AddCheckBox("Implicit (&Current Type)")).Click += new EventHandler(OnImplicitCurrentTypeStaticCallCheckBoxChanged);
			(myImplicitBaseTypesStaticCallCheckBox = menuItems.AddCheckBox("Implicit (&Base Types)")).Click += new EventHandler(OnImplicitBaseTypesStaticCallCheckBoxChanged);
			myTopMenu = topMenu;

			ICommandBarControl appRefresh1 = GetCommandbarControl(commandBarManager, "ToolBar", "Application.Refresh");
			if (appRefresh1 != null)
			{
				appRefresh1.Click += new EventHandler(OnApplicationRefresh);
			}
			ICommandBarControl appRefresh2 = GetCommandbarControl(commandBarManager, "View", "Application.Refresh");
			if (appRefresh2 != null && appRefresh2 != appRefresh1)
			{
				appRefresh2.Click += new EventHandler(OnApplicationRefresh);
			}
			//DumpMenus(commandBarManager);
		}
		private void DumpMenus(ICommandBarManager commandBarManager)
		{
			foreach (ICommandBar cb in commandBarManager.CommandBars)
			{
				DumpCommandBar(cb, 0);
			}
		}
		private void DumpCommandBar(ICommandBar commandBar, int indent)
		{
			for (int i = 0; i < indent; ++i)
			{
				System.Diagnostics.Debug.Write("\t");
			}
			System.Diagnostics.Debug.WriteLine(commandBar.Identifier);
			++indent;
			foreach (ICommandBarItem childItem in commandBar.Items)
			{
				ICommandBarControl control;
				ICommandBarMenu menu;
				if (null != (control = childItem as ICommandBarControl))
				{
					for (int i = 0; i < indent; ++i)
					{
						System.Diagnostics.Debug.Write("\t");
					}
					System.Diagnostics.Debug.WriteLine(control.Value);
				}
				else if (null != (menu = childItem as ICommandBarMenu))
				{
					DumpCommandBar(menu, indent);
				}
			}
		}
		void IPackage.Unload()
		{
			myLanguageManager.ActiveLanguageChanged -= new EventHandler(OnActiveLanguageChanged);
			myAssemblyBrowser.ActiveItemChanged -= new EventHandler(OnActiveItemChanged);
			myTopMenu.DropDown -= new EventHandler(OnOpenTopMenu);
			myLanguageManager.UnregisterLanguage(myLanguage);

			ICommandBarManager commandBarManager = (ICommandBarManager)myServiceProvider.GetService(typeof(ICommandBarManager));
			commandBarManager.CommandBars["MenuBar"].Items.Remove(myTopMenu);
			ICommandBarControl appRefresh1 = GetCommandbarControl(commandBarManager, "ToolBar", "Application.Refresh");
			if (appRefresh1 != null)
			{
				appRefresh1.Click -= new EventHandler(OnApplicationRefresh);
			}
			ICommandBarControl appRefresh2 = GetCommandbarControl(commandBarManager, "View", "Application.Refresh");
			if (appRefresh2 != null && appRefresh2 != appRefresh1)
			{
				appRefresh2.Click -= new EventHandler(OnApplicationRefresh);
			}
		}
		private static ICommandBarControl GetCommandbarControl(ICommandBarManager manager, string barIdentifier, string controlValue)
		{
			ICommandBar toolbar;
			if (null != (toolbar = manager.CommandBars[barIdentifier]))
			{
				foreach (ICommandBarItem item in toolbar.Items)
				{
					ICommandBarControl testControl = item as ICommandBarControl;
					string testControlValue;
					if (testControl != null &&
						null != (testControlValue = testControl.Value as string) &&
						testControlValue == controlValue)
					{
						return testControl;
					}
				}
			}
			return null;
		}
		/// <summary>
		/// Event handler to show our PLiX menu when we're the active rendering language
		/// </summary>
		private void OnActiveLanguageChanged(object sender, EventArgs e)
		{
			myTopMenu.Visible = myLanguageManager.ActiveLanguage == myLanguage;
		}
		/// <summary>
		/// Event handler to reenable the 'Expand Current Type Declaration' menu on selection change
		/// </summary>
		private void OnActiveItemChanged(object sender, EventArgs e)
		{
			PLiXConfiguration plixConfig = (PLiXConfiguration)myConfiguration;
			plixConfig.FullyExpandCurrentTypeDeclaration = false;
			plixConfig.FullyExpandCurrentNamespaceDeclaration = false;
			// Reflector does not have an ActiveItemChanging event, so this event handler
			// fires after some other windows are already updated, causing the 'expand current type'
			// setting to be stickier than it should be. To handle this, we track the active item
			// as well so that this setting can be ignored if the last active item is not the current
			// active item.
			myLastActiveItem = myAssemblyBrowser.ActiveItem;
		}
		/// <summary>
		/// Synchronize the example language sub menu with the current set of languages.
		/// Unfortunately, there is no add/remove event when languages are added and removed,
		/// and the DropDown event does not fire on submenus, so we need to synchronize here.
		/// </summary>
		private void OnOpenTopMenu(object sender, EventArgs e)
		{
			ICommandBarItemCollection exampleItems = myExampleLanguageMenu.Items;
			ILanguageCollection languages = myLanguageManager.Languages;
			ILanguage selectedLanguage = myConfiguration.ExampleLanguage;
			PLiXConfiguration plixConfig = (PLiXConfiguration)myConfiguration;
			object activeItem = myAssemblyBrowser.ActiveItem;
			bool activeItemIsTypeDeclaration = activeItem is ITypeDeclaration;
			bool activeItemIsNamespaceDeclaration = !activeItemIsTypeDeclaration && activeItem is INamespace;
			bool alreadyExpandedCurrentType = plixConfig.FullyExpandCurrentTypeDeclaration;
			bool alwaysExpandTypes = alreadyExpandedCurrentType ? false : myConfiguration.FullyExpandTypeDeclarations;
			if (activeItemIsNamespaceDeclaration)
			{
				myExpandCurrentNamespaceDeclarationButton.Visible = true;
				myExpandCurrentNamespaceDeclarationButton.Enabled = !plixConfig.FullyExpandCurrentNamespaceDeclaration;
			}
			else
			{
				myExpandCurrentNamespaceDeclarationButton.Visible = false;
			}
			if (activeItemIsTypeDeclaration)
			{
				myExpandCurrentTypeDeclarationButton.Visible = !alwaysExpandTypes;
				myExpandCurrentTypeDeclarationButton.Enabled = !alreadyExpandedCurrentType;
			}
			else
			{
				myExpandCurrentTypeDeclarationButton.Visible = false;
			}
			myFullyExpandTypeDeclarationsCheckBox.Checked = alwaysExpandTypes;
			myDisplayContextDataTypeQualifierCheckBox.Checked = plixConfig.DisplayContextDataTypeQualifier;
			StaticCallRenderingOption staticRender = plixConfig.StaticCallRenderingOption;
			myExplicitStaticCallCheckBox.Checked = staticRender == StaticCallRenderingOption.Explicit;
			myImplicitCurrentTypeStaticCallCheckBox.Checked = staticRender == StaticCallRenderingOption.ImplicitCurrentType;
			myImplicitBaseTypesStaticCallCheckBox.Checked = staticRender == StaticCallRenderingOption.ImplicitBaseTypes;
			int itemsCount = exampleItems.Count;
			ICommandBarCheckBox currentItem;
			if (itemsCount == 0)
			{
				currentItem = exampleItems.AddCheckBox("None");
				currentItem.Checked = selectedLanguage == null;
				currentItem.Click += new EventHandler(OnExampleLanguageClick);
				++itemsCount;
			}
			else
			{
				currentItem = (ICommandBarCheckBox)exampleItems[0];
				if (currentItem.Checked ^ (selectedLanguage == null))
				{
					currentItem.Checked = selectedLanguage == null;
				}
			}

			int languagesCount = languages.Count;
			int currentItemIndex = 1; // None is at the zero position
			for (int iLanguage = 0; iLanguage < languagesCount; ++iLanguage)
			{
				ILanguage currentLanguage = languages[iLanguage];
				bool isChecked = currentLanguage == selectedLanguage;
				string languageName = currentLanguage.Name;
				if (currentLanguage != myLanguage && !languageName.StartsWith("IL"))
				{
					if (currentItemIndex >= itemsCount)
					{
						currentItem = exampleItems.AddCheckBox(languageName);
						currentItem.Value = currentLanguage;
						if (isChecked)
						{
							currentItem.Checked = true;
						}
						currentItem.Click += new EventHandler(OnExampleLanguageClick);
						// No need to adjust currentItemIndex here, we'll continue to add to the end of the list
					}
					else
					{
						currentItem = (ICommandBarCheckBox)exampleItems[currentItemIndex];
						ILanguage testLanguage = (ILanguage)currentItem.Value;
						if (testLanguage == currentLanguage)
						{
							++currentItemIndex;
							if (currentItem.Checked ^ isChecked)
							{
								currentItem.Checked = isChecked;
							}
							continue;
						}
						else
						{
							// If the testLanguage appears later in the language list, then we need
							// to insert the new language here. Otherwise, we need to remove the
							// existing item.
							int matchingLanguage = iLanguage + 1;
							for (; matchingLanguage < languagesCount; ++matchingLanguage)
							{
								if (testLanguage == languages[matchingLanguage])
								{
									break;
								}
							}
							if (matchingLanguage < languagesCount)
							{
								// The language at this item will match later. Insert the currentLanguage at this position
								currentItem = exampleItems.InsertCheckBox(currentItemIndex, languageName);
								currentItem.Value = currentLanguage;
								if (isChecked)
								{
									currentItem.Checked = true;
								}
								currentItem.Click += new EventHandler(OnExampleLanguageClick);
								++itemsCount;
								++currentItemIndex;
							}
							else
							{
								// The item needs to be removed
								exampleItems.Remove(currentItem);
								--itemsCount;
							}
						}
					}
				}
			}
			// Remove any remaining items that we didn't match with a language
			if (currentItemIndex < itemsCount)
			{
				for (int i = itemsCount - 1; i >= currentItemIndex; --i)
				{
					exampleItems.RemoveAt(i);
				}
			}
		}
		/// <summary>
		/// Event handler for clicking a different example language
		/// </summary>
		private void OnExampleLanguageClick(object sender, EventArgs e)
		{
			((PLiXConfiguration)myConfiguration).ExampleLanguage = ((ICommandBarItem)sender).Value as ILanguage;
		}
		/// <summary>
		/// Event handler for toggling the full type expansion option
		/// </summary>
		private void OnFullyExpandTypeDeclarationsChanged(object sender, EventArgs e)
		{
			PLiXConfiguration config = (PLiXConfiguration)myConfiguration;
			// If the current is explicitly expanded, then FullyExpandTypeDeclarations will return
			// true, even if the cached flag is off.
			if (config.FullyExpandCurrentTypeDeclaration)
			{
				config.FullyExpandTypeDeclarations = true;
			}
			else
			{
				config.FullyExpandTypeDeclarations = !config.FullyExpandTypeDeclarations;
			}
		}
		/// <summary>
		/// Event handler for toggling the display current namespace option
		/// </summary>
		private void OnDisplayContextDataTypeQualifierChanged(object sender, EventArgs e)
		{
			PLiXConfiguration config = (PLiXConfiguration)myConfiguration;
			config.DisplayContextDataTypeQualifier = !config.DisplayContextDataTypeQualifier;
		}
		/// <summary>
		/// Event handler for turning on explicit call static calls
		/// </summary>
		private void OnExplicitStaticCallCheckBoxChanged(object sender, EventArgs e)
		{
			PLiXConfiguration config = (PLiXConfiguration)myConfiguration;
			config.StaticCallRenderingOption = StaticCallRenderingOption.Explicit;
		}
		/// <summary>
		/// Event handler for turning on implicit static calls for the context type
		/// </summary>
		private void OnImplicitCurrentTypeStaticCallCheckBoxChanged(object sender, EventArgs e)
		{
			PLiXConfiguration config = (PLiXConfiguration)myConfiguration;
			config.StaticCallRenderingOption = StaticCallRenderingOption.ImplicitCurrentType;
		}
		/// <summary>
		/// Event handler for turning on implicit static calls for the context type and all base types
		/// </summary>
		private void OnImplicitBaseTypesStaticCallCheckBoxChanged(object sender, EventArgs e)
		{
			PLiXConfiguration config = (PLiXConfiguration)myConfiguration;
			config.StaticCallRenderingOption = StaticCallRenderingOption.ImplicitBaseTypes;
		}
		/// <summary>
		/// Event handler for toggling the full type expansion option
		/// </summary>
		private void OnExpandCurrentTypeDeclaration(object sender, EventArgs e)
		{
			((PLiXConfiguration)myConfiguration).FullyExpandCurrentTypeDeclaration = true;
		}
		/// <summary>
		/// Event handler for toggling the full type expansion option
		/// </summary>
		private void OnExpandCurrentNamespaceDeclaration(object sender, EventArgs e)
		{
			((PLiXConfiguration)myConfiguration).FullyExpandCurrentNamespaceDeclaration = true;
		}
		/// <summary>
		/// Event handler to clear cached information when the Refresh button is clicked
		/// </summary>
		private void OnApplicationRefresh(object sender, EventArgs e)
		{
			PLiXLanguage language = myLanguage as PLiXLanguage;
			if (language != null)
			{
				language.OnRefresh();
			}
		}
		#endregion // IPackage Implementation
	}
}
