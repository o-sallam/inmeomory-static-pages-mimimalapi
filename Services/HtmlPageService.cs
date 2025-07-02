using System.Collections.Generic;
using System.IO;
using System;
using System.Threading.Tasks;

namespace StaticAotWeb.Services
{
    public class HtmlPageService
    {
        private readonly Dictionary<string, string> _htmlPages = new();
        private readonly string _inlineStyle;

        public HtmlPageService()
        {
            // Load and cache style.css
            _inlineStyle = File.Exists("wwwroot/style.css") ? File.ReadAllText("wwwroot/style.css") : string.Empty;
            // Load and cache HTML pages
            var files = Directory.GetFiles("wwwroot", "*.html");
            foreach (var file in files)
            {
                var pageName = Path.GetFileNameWithoutExtension(file);
                var html = File.ReadAllText(file);
                if (pageName == "index" && !string.IsNullOrEmpty(_inlineStyle))
                {
                    // Replace <link rel="stylesheet" ...> with <style>...</style>
                    html = System.Text.RegularExpressions.Regex.Replace(
                        html,
                        "<link[^>]*rel=[\"']stylesheet[\"'][^>]*>",
                        $"<style>{_inlineStyle}</style>",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    );
                    // Inline images as base64
                    html = InlineImagesAsBase64(html);
                }
                _htmlPages[pageName] = html;
            }
        }

        private string InlineImagesAsBase64(string html)
        {
            // Find all <img ... src="/img/xxx.webp" ...>
            var regex = new System.Text.RegularExpressions.Regex("<img([^>]+)src=\"(/img/([^\"]+))\"([^>]*)>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return regex.Replace(html, match =>
            {
                var src = match.Groups[2].Value; // /img/xxx.webp
                var fileName = match.Groups[3].Value; // xxx.webp
                var imgPath = Path.Combine("wwwroot", "img", fileName);
                if (File.Exists(imgPath))
                {
                    var bytes = File.ReadAllBytes(imgPath);
                    var base64 = Convert.ToBase64String(bytes);
                    var ext = Path.GetExtension(fileName).ToLowerInvariant();
                    string mime;
                    switch (ext)
                    {
                        case ".webp": mime = "image/webp"; break;
                        case ".jpg": mime = "image/jpeg"; break;
                        case ".jpeg": mime = "image/jpeg"; break;
                        case ".png": mime = "image/png"; break;
                        default: mime = "application/octet-stream"; break;
                    }
                    var dataUri = $"data:{mime};base64,{base64}";
                    return $"<img{match.Groups[1].Value}src=\"{dataUri}\"{match.Groups[4].Value}>";
                }
                return match.Value;
            });
        }

        public string? GetPageHtml(string? page)
        {
            var pageKey = string.IsNullOrEmpty(page) ? "index" : page;
            if (!_htmlPages.TryGetValue(pageKey, out var html))
                return null;
            // Example: replace %TITLE% in all pages
            html = html.Replace("%TITLE%", "مرحبًا بك في موقعي");
            return html;
        }

        public async IAsyncEnumerable<string> GetPageHtmlChunksAsync(string? page)
        {
            var html = GetPageHtml(page);
            if (html == null)
            {
                yield return "Page not found";
                yield break;
            }
            const int chunkSize = 4096;
            for (int i = 0; i < html.Length; i += chunkSize)
            {
                var chunk = html.Substring(i, Math.Min(chunkSize, html.Length - i));
                yield return chunk;
                await Task.Yield();
            }
        }
    }
}