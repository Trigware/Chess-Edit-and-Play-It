using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class Localization : Node
{
	public static Language chosenLanguage = Language.English;
	private static Dictionary<Language, Dictionary<string, string>> AllLanguageTexts = new();
	private readonly static Language BaseLanguageFile = Language.English;
	public enum Language
	{
		English,
		Czech
	}
	public enum Path
	{
		PauseTitle,
		PauseDescription,
		GamePaused,
		Players,
		PauseInteraction
	}
	public override void _Ready()
	{
		Array allLanguages = Enum.GetValues(typeof(Language));
		foreach (Language lang in allLanguages)
			LoadLanguage(lang);
		CheckIfAllBaseLanguagePropertiesAreValid(allLanguages);
	}
	private static void LoadLanguage(Language lang)
	{
		string pathToFile = $"res://Data Resources/Text{lang}.json";
		AllLanguageTexts.Add(lang, FlattenJSONData(LoadJSON(pathToFile), lang));
	}
	private static JsonElement LoadJSON(string pathToFile)
	{
		string fileContents = FileAccess.GetFileAsString(pathToFile);
		JsonDocument jsonDoc = JsonDocument.Parse(fileContents);
		return jsonDoc.RootElement;
	}
	private static Dictionary<string, string> FlattenJSONData(JsonElement jsonFileData, Language lang)
	{
		Dictionary<string, string> pureJsonData = new();
		FlattenNestedObjects(jsonFileData, pureJsonData, lang);
		return pureJsonData;

	}
	private static void FlattenNestedObjects(JsonElement element, Dictionary<string, string> pureJsonData, Language lang, string currentPath = "")
	{
		switch (element.ValueKind)
		{
			case JsonValueKind.Object:
				foreach (JsonProperty property in element.EnumerateObject())
				{
					string keyWithPath = currentPath == "" ? property.Name : currentPath + "." + property.Name;
					FlattenNestedObjects(property.Value, pureJsonData, lang, keyWithPath);
				}	
				break;
			case JsonValueKind.String:
				if (AllLanguageTexts.ContainsKey(BaseLanguageFile) && !AllLanguageTexts[BaseLanguageFile].ContainsKey(currentPath))
					GD.PrintErr($"{lang} language file contains '{currentPath}' which doesn't exist in the {BaseLanguageFile} language file!");
				pureJsonData.Add(currentPath, element.GetString());
				break;
			default:
				GD.PrintErr($"The only JsonElement kinds currently supported are strings and objects.\nPlease modify '{currentPath}' in the {lang} language file!");
				break;
		}
	}
	private static void CheckIfAllBaseLanguagePropertiesAreValid(Array allLanguages)
	{
		foreach (string propertyName in AllLanguageTexts[BaseLanguageFile].Keys)
		{
			foreach (Language lang in allLanguages)
			{
				if (lang == BaseLanguageFile) continue;
				if (!AllLanguageTexts[lang].ContainsKey(propertyName))
					GD.PrintErr($"{BaseLanguageFile} language file contains '{propertyName}' which doesn't exist in the {lang} language file!");
			}
		}
	}
	public record TextVariables(params object[] Variables);
	public static string GetText(TextVariables textVariables, Path textPath, string propertyPathFinalizer = "")
	{
		string textPathWithFinalizer = GetTextPathFromEnum(textPath);
		if (propertyPathFinalizer != "") textPathWithFinalizer += '.' + propertyPathFinalizer;
		return GetTextFullPath(textVariables,  textPathWithFinalizer);
	}
	public static string GetText(Path textPath, string propertyPathFinalizer = "") => GetText(new(), textPath, propertyPathFinalizer);
	private static string GetTextFullPath(TextVariables textVariables, string textPath)
	{
		Dictionary<string, string> languageFileText = AllLanguageTexts[chosenLanguage];
		if (!languageFileText.ContainsKey(textPath))
		{
			GD.PrintErr($"Attempting to load non-existent text at '{textPath}' from the {chosenLanguage} language file!");
			return '{' + textPath + '}' + $", {chosenLanguage}";
		}
		return ReplaceNamesForValues(textVariables, languageFileText[textPath], textPath);
	}
	private static string ReplaceNamesForValues(TextVariables textVariables, string text, string textPath)
	{
		Dictionary<string, object> cachedVariables = new();
		string modifiedText = "", variableName = "";
		bool inTextVariable = false;
		foreach (char ch in text)
		{
			switch (ch)
			{
				case '{':
					if (!inTextVariable)
						variableName = "";
					else
						GD.PushError($"Cannot have a variable inside of another variable! ({textPath}, {chosenLanguage})");
					inTextVariable = true;
					break;
				case '}':
					object variableValue = '{' + variableName + '}';
					if (cachedVariables.ContainsKey(variableName))
						variableValue = cachedVariables[variableName];
					else if (cachedVariables.Count < textVariables.Variables.Length)
					{
						variableValue = textVariables.Variables[cachedVariables.Count];
						cachedVariables.Add(variableName, variableValue);
					}
					else
						GD.PushError($"Not enough text variables have been given, so '{variableName}' (index: {cachedVariables.Count}) cannot be replaced! ({textPath}, {chosenLanguage})");
					inTextVariable = false;
					variableName = "";
					modifiedText += variableValue;
					break;
				default:
					if (inTextVariable) variableName += ch;
					else modifiedText += ch;
					break;
			}
		}
		if (cachedVariables.Count != textVariables.Variables.Length)
			GD.PushWarning($"For text at '{textPath}' ({chosenLanguage}) didn't encounter as many variables as expected (Expected: {textVariables.Variables.Length}, Actual: {cachedVariables.Count})");
		return modifiedText;
	}
	private static string GetTextPathFromEnum(Path textPathAsEnum)
	{
		return textPathAsEnum switch
		{
			Path.PauseTitle => "Pause Menu.Title Text",
			Path.PauseDescription => "Pause Menu.Description Text",
			Path.GamePaused => "Pause Menu.Title Text.Paused",
			Path.Players => "Players",
			Path.PauseInteraction => "Pause Menu.Interaction Text",
			_ => throw new Exception($"Please update the {textPathAsEnum} enum, text path missing!")
		};
	}
}
