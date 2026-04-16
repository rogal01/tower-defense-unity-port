using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TowerDefensePort
{
    public static class TDVisuals
    {
        private static Sprite _whiteSprite;
        private static Sprite _circleSprite;
        private static Font _font;
        private static Material _particleMaterial;

        public static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite == null)
                {
                    var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    texture.name = "TD_WhiteTexture";
                    texture.SetPixel(0, 0, Color.white);
                    texture.Apply();
                    _whiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                }

                return _whiteSprite;
            }
        }

        public static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite == null)
                {
                    const int size = 64;
                    var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    texture.name = "TD_CircleTexture";
                    texture.filterMode = FilterMode.Bilinear;

                    var center = (size - 1) * 0.5f;
                    var radius = size * 0.5f - 1f;
                    for (var y = 0; y < size; y++)
                    {
                        for (var x = 0; x < size; x++)
                        {
                            var dx = x - center;
                            var dy = y - center;
                            var distance = Mathf.Sqrt(dx * dx + dy * dy);
                            var alpha = Mathf.Clamp01((radius - distance) / 1.5f);
                            texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                        }
                    }

                    texture.Apply();
                    _circleSprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, size, size),
                        new Vector2(0.5f, 0.5f),
                        size);
                }

                return _circleSprite;
            }
        }

        public static Font UIFont
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return _font;
            }
        }

        private static Material ParticleMaterial
        {
            get
            {
                if (_particleMaterial == null)
                {
                    _particleMaterial = new Material(Shader.Find("Sprites/Default"));
                    _particleMaterial.hideFlags = HideFlags.HideAndDontSave;
                }

                return _particleMaterial;
            }
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(go);
        }

        public static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            return canvas;
        }

        public static GameObject CreateUiBlock(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var image = go.GetComponent<Image>();
            image.sprite = WhiteSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return go;
        }

        public static Text CreateText(Transform parent, string name, string value, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow), typeof(Outline));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<Text>();
            text.font = UIFont;
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var shadow = go.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(1.2f, -1.2f);

            var outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.22f);
            outline.effectDistance = new Vector2(0.8f, -0.8f);
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, Color background, Color textColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Shadow));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = WhiteSprite;
            image.color = Color.Lerp(background, new Color(0.06f, 0.09f, 0.14f, 1f), 0.08f);

            var shadow = go.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
            shadow.effectDistance = new Vector2(0f, -2f);

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = background;
            colors.highlightedColor = background * new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = background * new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(background.r * 0.5f, background.g * 0.5f, background.b * 0.5f, 0.8f);
            button.colors = colors;

            var labelText = CreateText(go.transform, "Label", label, 30, TextAnchor.MiddleCenter, textColor);
            var rect = labelText.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return button;
        }

        public static GameObject CreateWorldSprite(string name, Transform parent, Vector2 position, Vector2 scale, Color color, int sortingOrder)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = WhiteSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return go;
        }

        public static GameObject CreateWorldDisc(string name, Transform parent, Vector2 position, Vector2 scale, Color color, int sortingOrder)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = CircleSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return go;
        }

        public static LineRenderer CreateLine(string name, Transform parent, Color color, float width, int order)
        {
            var go = new GameObject(name, typeof(LineRenderer));
            go.transform.SetParent(parent, false);
            var line = go.GetComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = color;
            line.endColor = color;
            line.startWidth = width;
            line.endWidth = width;
            line.numCapVertices = 6;
            line.numCornerVertices = 6;
            line.useWorldSpace = true;
            line.sortingOrder = order;
            return line;
        }

        public static ParticleSystem CreateLoopParticles(
            string name,
            Transform parent,
            Vector2 localPosition,
            Color colorA,
            Color colorB,
            float radius,
            float rate,
            float speed,
            float lifetime,
            float sizeMin,
            float sizeMax,
            int sortingOrder)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);

            var particles = go.GetComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.maxParticles = 120;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.gravityModifier = 0f;

            var emission = particles.emission;
            emission.rateOverTime = rate;

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(0f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.14f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var colorGradient = new Gradient();
            colorGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(colorA, 0f),
                    new GradientColorKey(colorB, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.85f, 0.2f),
                    new GradientAlphaKey(0.65f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGradient);

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.material = ParticleMaterial;
            renderer.sortingOrder = sortingOrder;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            particles.Play();
            return particles;
        }

        public static void SpawnBurstParticles(
            Transform parent,
            string name,
            Vector2 position,
            Color color,
            int count,
            float speed,
            float lifetime,
            float size,
            int sortingOrder)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);

            var particles = go.GetComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.65f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.7f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.65f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(color.r, color.g, color.b, 0.95f),
                new Color(Mathf.Min(1f, color.r * 1.12f), Mathf.Min(1f, color.g * 1.12f), Mathf.Min(1f, color.b * 1.12f), 0.7f));
            main.maxParticles = Mathf.Clamp(count * 3, 16, 120);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, 60)) });

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = size * 0.4f;

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var burstGradient = new Gradient();
            burstGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(Color.Lerp(color, Color.white, 0.35f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.4f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(burstGradient);

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(0.7f, 0.74f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.material = ParticleMaterial;
            renderer.sortingOrder = sortingOrder;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            particles.Play();
            Object.Destroy(go, lifetime + 0.6f);
        }
    }
}
