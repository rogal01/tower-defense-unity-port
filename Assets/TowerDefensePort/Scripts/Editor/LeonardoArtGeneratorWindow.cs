using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace TowerDefensePort.Editor
{
    public sealed class LeonardoArtGeneratorWindow : EditorWindow
    {
        private const string ApiKeyPref = "td_leonardo_api_key";
        private const string BaseUrl = "https://cloud.leonardo.ai/api/rest/v1";
        private const string DefaultModelId = "7b592283-e8a7-4c5a-9ba6-d18c31f258b9";

        private string _apiKey = "";
        private string _modelId = DefaultModelId;
        private string _negativePrompt = "blurry, low quality, watermark, text, logo, ui";
        private string _stylePrompt =
            "2D top-down game asset, clean silhouette, hand-painted look, high readability, cohesive fantasy tower defense art direction";
        private string _prompts =
            "enemy goblin scout, green skin, leather armor, top-down sprite\n" +
            "enemy brute orc, heavy armor, top-down sprite\n" +
            "enemy skeleton, bone warrior, top-down sprite\n" +
            "tower archer turret, wood and metal, top-down sprite\n" +
            "tower magic crystal turret, arcane glow, top-down sprite\n" +
            "tower cannon turret, iron barrel, top-down sprite";

        private int _width = 512;
        private int _height = 512;
        private int _numImages = 1;
        private float _pollIntervalSeconds = 1.5f;
        private int _pollTimeoutSeconds = 60;
        private bool _alchemy = false;
        private string _outputFolder = "Assets/TowerDefensePort/GeneratedArt";
        private bool _isGenerating;
        private Vector2 _scroll;
        private string _status = "Idle";

        [MenuItem("Tools/Tower Defense/Leonardo Art Generator")]
        private static void Open()
        {
            var window = GetWindow<LeonardoArtGeneratorWindow>("Leonardo Art");
            window.minSize = new Vector2(540f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            _apiKey = EditorPrefs.GetString(ApiKeyPref, "");
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("Leonardo AI Art Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Generates tower/enemy art and imports images as sprites. Keep your API key private and do not commit it.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(_isGenerating))
            {
                EditorGUILayout.Space(8f);
                _apiKey = EditorGUILayout.PasswordField("API Key", _apiKey);
                _modelId = EditorGUILayout.TextField("Model ID", _modelId);
                _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);

                EditorGUILayout.Space(8f);
                _width = EditorGUILayout.IntField("Width", _width);
                _height = EditorGUILayout.IntField("Height", _height);
                _numImages = EditorGUILayout.IntSlider("Images per Prompt", _numImages, 1, 4);
                _alchemy = EditorGUILayout.Toggle("Alchemy", _alchemy);
                _pollIntervalSeconds = EditorGUILayout.Slider("Poll Interval (s)", _pollIntervalSeconds, 0.5f, 4f);
                _pollTimeoutSeconds = EditorGUILayout.IntSlider("Timeout (s)", _pollTimeoutSeconds, 15, 180);

                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Style Prefix", EditorStyles.boldLabel);
                _stylePrompt = EditorGUILayout.TextArea(_stylePrompt, GUILayout.MinHeight(54f));

                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Negative Prompt", EditorStyles.boldLabel);
                _negativePrompt = EditorGUILayout.TextArea(_negativePrompt, GUILayout.MinHeight(54f));

                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Prompts (one per line)", EditorStyles.boldLabel);
                _prompts = EditorGUILayout.TextArea(_prompts, GUILayout.MinHeight(160f));

                EditorGUILayout.Space(12f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Save API Key", GUILayout.Height(30f)))
                    {
                        EditorPrefs.SetString(ApiKeyPref, _apiKey);
                        _status = "API key saved to EditorPrefs.";
                    }

                    if (GUILayout.Button("Generate Pack", GUILayout.Height(30f)))
                    {
                        _ = GeneratePackAsync();
                    }
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(_status, MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private async Task GeneratePackAsync()
        {
            if (_isGenerating)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _status = "Missing API key.";
                return;
            }

            var promptLines = SplitPrompts(_prompts);
            if (promptLines.Count == 0)
            {
                _status = "No prompts provided.";
                return;
            }

            try
            {
                _isGenerating = true;
                _status = "Generating...";
                Repaint();
                EnsureOutputFolder(_outputFolder);

                var savedCount = 0;
                for (var i = 0; i < promptLines.Count; i++)
                {
                    var subjectPrompt = promptLines[i];
                    var finalPrompt = _stylePrompt + ", " + subjectPrompt;
                    _status = "Submitting " + (i + 1) + "/" + promptLines.Count + ": " + subjectPrompt;
                    Repaint();

                    var generationId = await SubmitGenerationAsync(
                        _apiKey,
                        finalPrompt,
                        _negativePrompt,
                        _modelId,
                        _width,
                        _height,
                        _numImages,
                        _alchemy);

                    if (string.IsNullOrEmpty(generationId))
                    {
                        _status = "Failed to create generation for: " + subjectPrompt;
                        continue;
                    }

                    _status = "Waiting for images: " + generationId;
                    Repaint();
                    var urls = await PollGeneratedImageUrlsAsync(_apiKey, generationId, _pollIntervalSeconds, _pollTimeoutSeconds);
                    if (urls.Count == 0)
                    {
                        _status = "No images returned for generation: " + generationId;
                        continue;
                    }

                    for (var imageIndex = 0; imageIndex < urls.Count; imageIndex++)
                    {
                        var bytes = await DownloadBytesAsync(urls[imageIndex]);
                        if (bytes == null || bytes.Length == 0)
                        {
                            continue;
                        }

                        var fileName = SanitizeFileName(subjectPrompt) + "_" + imageIndex + ".png";
                        var absolutePath = Path.Combine(Application.dataPath, _outputFolder.Replace("Assets/", "") + "/" + fileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? Application.dataPath);
                        File.WriteAllBytes(absolutePath, bytes);
                        savedCount++;
                    }

                    AssetDatabase.Refresh();
                    ConfigureImportedSprites(_outputFolder);
                }

                AssetDatabase.SaveAssets();
                _status = "Done. Saved " + savedCount + " image(s) to " + _outputFolder + ".";
            }
            catch (Exception ex)
            {
                _status = "Generation failed: " + ex.Message;
            }
            finally
            {
                _isGenerating = false;
                Repaint();
            }
        }

        private static List<string> SplitPrompts(string input)
        {
            var lines = new List<string>();
            var raw = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in raw)
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    lines.Add(trimmed);
                }
            }
            return lines;
        }

        private static async Task<string> SubmitGenerationAsync(
            string apiKey,
            string prompt,
            string negativePrompt,
            string modelId,
            int width,
            int height,
            int numImages,
            bool alchemy)
        {
            var body = BuildGenerationPayload(prompt, negativePrompt, modelId, width, height, numImages, alchemy);
            var json = await SendJsonAsync(BaseUrl + "/generations", "POST", apiKey, body);
            return ExtractGenerationId(json);
        }

        private static async Task<List<string>> PollGeneratedImageUrlsAsync(
            string apiKey,
            string generationId,
            float pollIntervalSeconds,
            int timeoutSeconds)
        {
            var urls = new List<string>();
            var start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start).TotalSeconds < timeoutSeconds)
            {
                var json = await SendJsonAsync(BaseUrl + "/generations/" + generationId, "GET", apiKey, null);
                urls = ExtractImageUrls(json);
                if (urls.Count > 0)
                {
                    return urls;
                }
                await Task.Delay(Mathf.RoundToInt(pollIntervalSeconds * 1000f));
            }
            return urls;
        }

        private static async Task<byte[]> DownloadBytesAsync(string url)
        {
            using var request = UnityWebRequest.Get(url);
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }
            if (request.result != UnityWebRequest.Result.Success)
            {
                return null;
            }
            return request.downloadHandler.data;
        }

        private static async Task<string> SendJsonAsync(string url, string method, string apiKey, string body)
        {
            var request = new UnityWebRequest(url, method);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("accept", "application/json");
            request.SetRequestHeader("authorization", "Bearer " + apiKey);
            if (!string.IsNullOrEmpty(body))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.SetRequestHeader("content-type", "application/json");
            }

            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                var errorJson = request.downloadHandler != null ? request.downloadHandler.text : "";
                throw new Exception("HTTP " + request.responseCode + " " + request.error + " " + errorJson);
            }

            return request.downloadHandler.text;
        }

        private static string BuildGenerationPayload(
            string prompt,
            string negativePrompt,
            string modelId,
            int width,
            int height,
            int numImages,
            bool alchemy)
        {
            var safePrompt = EscapeJson(prompt);
            var safeNegative = EscapeJson(negativePrompt);
            var safeModel = EscapeJson(modelId);
            return "{"
                   + "\"prompt\":\"" + safePrompt + "\","
                   + "\"negative_prompt\":\"" + safeNegative + "\","
                   + "\"modelId\":\"" + safeModel + "\","
                   + "\"width\":" + Mathf.Clamp(width, 256, 2048) + ","
                   + "\"height\":" + Mathf.Clamp(height, 256, 2048) + ","
                   + "\"num_images\":" + Mathf.Clamp(numImages, 1, 8) + ","
                   + "\"alchemy\":" + (alchemy ? "true" : "false")
                   + "}";
        }

        private static string ExtractGenerationId(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return "";
            }

            var match = Regex.Match(json, "\"generationId\"\\s*:\\s*\"([^\"]+)\"");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            match = Regex.Match(json, "\"generation_id\"\\s*:\\s*\"([^\"]+)\"");
            return match.Success ? match.Groups[1].Value : "";
        }

        private static List<string> ExtractImageUrls(string json)
        {
            var urls = new List<string>();
            if (string.IsNullOrEmpty(json))
            {
                return urls;
            }

            var matches = Regex.Matches(json, "\"url\"\\s*:\\s*\"(https?:\\\\/\\\\/[^\"\\\\]+(?:\\\\/[^\"\\\\]+)*)\"");
            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                var url = match.Groups[1].Value.Replace("\\/", "/");
                if (!urls.Contains(url))
                {
                    urls.Add(url);
                }
            }
            return urls;
        }

        private static string EscapeJson(string value)
        {
            if (value == null)
            {
                return "";
            }
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        private static string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (var c in name)
            {
                if (Array.IndexOf(invalidChars, c) >= 0 || char.IsWhiteSpace(c))
                {
                    sb.Append('_');
                }
                else
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
            }
            return sb.ToString().Trim('_');
        }

        private static void EnsureOutputFolder(string folderPath)
        {
            var absolute = Path.Combine(Application.dataPath, folderPath.Replace("Assets/", ""));
            Directory.CreateDirectory(absolute);
        }

        private static void ConfigureImportedSprites(string folder)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                var changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }
                if (importer.alphaIsTransparency != true)
                {
                    importer.alphaIsTransparency = true;
                    changed = true;
                }
                if (importer.mipmapEnabled != false)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }
    }
}
