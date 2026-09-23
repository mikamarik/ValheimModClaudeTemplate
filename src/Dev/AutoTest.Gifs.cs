#if DEBUG
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MyValheimMod.Dev
{
    /// <summary>
    /// Records the frames of the README's GIFs into .devtest/gifs/NAME/, one JPG per rendered
    /// frame. <c>Time.captureFramerate</c> holds game time to a fixed step per frame, so a clip
    /// plays smooth however slow the capture is. scripts/make-gifs.py turns the frames into
    /// docs/media/NAME.gif. A tool, not a test: "readme_gifs" is not in "all".
    /// AUTOTEST_GIFS="example" records only those, in that order.
    /// </summary>
    internal static partial class AutoTest
    {
        private const int GifFps = 15;

        private static GifRecorder _gif;

        private sealed class GifRecorder
        {
            public string Folder;
            public bool Running;
            public int Frames;
            public int Writing;
        }

        /// <summary>Every clip by name. Add one per feature the README shows.</summary>
        private static Func<Player, IEnumerator> GifClip(string name)
        {
            switch (name)
            {
                case "example": return GifExample;
                default: return null;
            }
        }

        private static IEnumerator RecordReadmeGifs(Player player)
        {
            var names = (Environment.GetEnvironmentVariable("AUTOTEST_GIFS") ?? "example")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var raw in names)
            {
                var name = raw.Trim();
                var clip = GifClip(name);
                if (clip == null)
                {
                    Check(false, "no such gif: " + name);
                    continue;
                }

                Log($"===== gif {name}");
                yield return clip(player);
                yield return StopGif();
                yield return Reset(player);
            }
        }

        private static void StartGif(string name)
        {
            var folder = Path.Combine(OutDir, "gifs", name);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }

            Directory.CreateDirectory(folder);
            _gif = new GifRecorder { Folder = folder, Running = true };
            Time.captureFramerate = GifFps;
            MyValheimModPlugin.Instance.StartCoroutine(GifFrames(_gif));
            Log($"gif '{name}': recording {Screen.width}x{Screen.height} at {GifFps} fps into {folder}");
        }

        /// <summary>
        /// Tells make-gifs.py which part of the frames to keep: the box round these regions and a
        /// frame round them, in the frames' own pixels (half the screen's), from the top left.
        /// </summary>
        private static void GifCrop(params RectTransform[] regions)
        {
            if (_gif == null)
            {
                return;
            }

            var box = Rect.zero;
            var first = true;
            foreach (var region in regions.Where(r => r != null))
            {
                var r = ScreenBox(region);
                box = first ? r : Rect.MinMaxRect(Mathf.Min(box.xMin, r.xMin), Mathf.Min(box.yMin, r.yMin), Mathf.Max(box.xMax, r.xMax), Mathf.Max(box.yMax, r.yMax));
                first = false;
            }

            if (first)
            {
                return;
            }

            const float frame = 24f;
            var x = Mathf.Max(0, Mathf.FloorToInt((box.xMin - frame) / 2f));
            var y = Mathf.Max(0, Mathf.FloorToInt((Screen.height - box.yMax - frame) / 2f));
            var w = Mathf.Min((Screen.width / 2) - x, Mathf.CeilToInt((box.width + (2f * frame)) / 2f));
            var h = Mathf.Min((Screen.height / 2) - y, Mathf.CeilToInt((box.height + (2f * frame)) / 2f));
            File.WriteAllText(Path.Combine(_gif.Folder, "crop.txt"), $"{x},{y},{w},{h}\n");
        }

        private static IEnumerator StopGif()
        {
            var gif = _gif;
            if (gif == null)
            {
                yield break;
            }

            gif.Running = false;
            yield return Frames(2);
            Time.captureFramerate = 0;
            var waited = 0;
            while (System.Threading.Volatile.Read(ref gif.Writing) > 0 && waited++ < 600)
            {
                yield return null;
            }

            _gif = null;
            Log($"gif: {gif.Frames} frames ({gif.Frames / (float)GifFps:0.0} s) in {gif.Folder}");
        }

        /// <summary>
        /// Every rendered frame, once it is complete: the screen at half size (a 2 x 2 average, as
        /// sharp as the final GIF can use) as a JPG, written off the main thread.
        /// </summary>
        private static IEnumerator GifFrames(GifRecorder gif)
        {
            // The JPG writer lives in Unity's ImageConversion module, which the plugin does not
            // reference; the game has it loaded, so it is looked up once here.
            var encoder = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule")
                ?.GetMethod("EncodeToJPG", new[] { typeof(Texture2D), typeof(int) });
            if (encoder == null)
            {
                Check(false, "Unity's JPG encoder is not loaded");
                yield break;
            }

            var encode = (Func<Texture2D, int, byte[]>)Delegate.CreateDelegate(typeof(Func<Texture2D, int, byte[]>), encoder);
            var width = Screen.width / 2;
            var height = Screen.height / 2;
            var full = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            var half = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            var flip = SystemInfo.graphicsUVStartsAtTop;
            var end = new WaitForEndOfFrame();
            try
            {
                while (gif.Running)
                {
                    yield return end;
                    if (!gif.Running)
                    {
                        break;
                    }

                    ScreenCapture.CaptureScreenshotIntoRenderTexture(full);
                    if (flip)
                    {
                        Graphics.Blit(full, half, new Vector2(1f, -1f), new Vector2(0f, 1f));
                    }
                    else
                    {
                        Graphics.Blit(full, half);
                    }

                    var active = RenderTexture.active;
                    RenderTexture.active = half;
                    pixels.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                    RenderTexture.active = active;
                    var bytes = encode(pixels, 94);
                    var path = Path.Combine(gif.Folder, $"{++gif.Frames:0000}.jpg");
                    System.Threading.Interlocked.Increment(ref gif.Writing);
                    System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            File.WriteAllBytes(path, bytes);
                        }
                        finally
                        {
                            System.Threading.Interlocked.Decrement(ref gif.Writing);
                        }
                    });
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(full);
                UnityEngine.Object.Destroy(half);
                UnityEngine.Object.Destroy(pixels);
            }
        }
    }
}
#endif
