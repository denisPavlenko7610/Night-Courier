using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public static class TemplatePackages
{
	private const double TimeoutSeconds = 600;
	private const string ExcludedPackageName = "com.rd.haptics";
	private static AddAndRemoveRequest _request;
	private static double _deadline;

	[MenuItem("Tools/Unity Templates/Install All Local Packages")]
	public static void InstallAll()
	{
		if (_request != null)
			throw new InvalidOperationException("Template package installation is already running.");

		var projectRoot = Directory.GetParent(Application.dataPath).FullName;
		var templateRoot = Path.GetFullPath(Path.Combine(projectRoot, "../UnityTemplates/Assets/UnityTemplates"));
		if (!Directory.Exists(templateRoot))
			throw new DirectoryNotFoundException(
				"Expected the UnityTemplates project next to this project: " + templateRoot
			);

		var manifests = Directory.GetFiles(templateRoot, "package.json", SearchOption.AllDirectories)
			.Where(path => Path.GetDirectoryName(path) != templateRoot)
			.Where(path => JsonUtility.FromJson<PackageManifest>(File.ReadAllText(path)).name != ExcludedPackageName)
			.OrderBy(path => path, StringComparer.Ordinal)
			.ToArray();
		if (manifests.Length == 0)
			throw new InvalidOperationException(
				"No individual packages found. Run UnityTemplates/Publish/generate_upm.py first."
			);

		var names = manifests.Select(path => JsonUtility.FromJson<PackageManifest>(File.ReadAllText(path)).name)
			.ToArray();
		if (names.Any(name => string.IsNullOrEmpty(name) || !name.StartsWith("com.rd.", StringComparison.Ordinal)) ||
			names.Distinct().Count() != names.Length)
			throw new InvalidOperationException("Template package names must be unique and start with com.rd.");

		var packages = manifests.Select(path =>
			"file:" +
			Path.GetRelativePath(Path.Combine(projectRoot, "Packages"), Path.GetDirectoryName(path)).Replace('\\', '/')
		).ToArray();
		Debug.Log("Installing " + packages.Length + " local template packages: " + string.Join(", ", names));
		_request = Client.AddAndRemove(packages);
		_deadline = EditorApplication.timeSinceStartup + TimeoutSeconds;
		EditorApplication.update += Poll;
	}

	private static void Poll()
	{
		if (!_request.IsCompleted && EditorApplication.timeSinceStartup < _deadline)
			return;

		EditorApplication.update -= Poll;
		var request = _request;
		_request = null;
		if (!request.IsCompleted)
			Debug.LogError("Template package installation timed out. Check Package Manager before retrying.");
		else if (request.Status == StatusCode.Success)
			Debug.Log("Local template packages installed successfully.");
		else
			Debug.LogError("Template package installation failed: " + request.Error?.message);
	}

	[Serializable]
	private sealed class PackageManifest
	{
		public string name;
	}
}