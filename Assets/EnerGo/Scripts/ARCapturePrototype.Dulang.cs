using UnityEngine;

namespace EnerGo
{
    // The 3D gold pan while panning. It only shows the pan state (goldTilt, goldSwirl, goldProgress, goldPos, goldLost);
    // the rules are in UpdateGoldPan and DulangMath. Water and sand are meshes rebuilt every frame so they follow the
    // bowl's shape; loose grains circle with the swirl and wash over the low rim as the sand goes.
    public partial class ARCapturePrototype
    {
        private const float DulangDistance = 0.6f;    // metres in front of the camera
        private const float DulangRimRadius = 0.255f; // sda_dulang, model units
        private const float DulangGoldRadius = 0.2f;  // goldPos 1 = this far from the middle
        private const float DulangWaterLevel = 0.05f;
        private const float GoldBuried = 0.02f;      // how far a piece sinks into the sand before it shows
        private const int WaterSegments = 40, WaterRings = 7, SandSegments = 32, SandRings = 6, GrainCount = 120;

        private Transform dulang, dulangTilt;
        private Transform[] dulangPieces;
        private Quaternion[] dulangPieceSpin;
        private Mesh dulangWater, dulangSand;
        private Vector3[] waterVerts, sandVerts;
        private Color[] waterColors, sandColors;
        private Vector2 waterSlope, waterSlopeSpeed, sandCentre;
        private ParticleSystem dulangGrainSystem, dulangGlints;
        private ParticleSystem.Particle[] grainParticles;
        private Vector2[] grainPos, grainVel;
        private float[] grainFall, grainDrop; // height above the pan once over the rim, and its speed; NaN = still in the pan
        private float dulangSimTime, dulangProgressBefore, dulangNextGlintAt;

        private void LateUpdate()
        {
            UpdateBorDiorama();
            UpdateUraniumVisual();
            bool show = goldPanning && arCamera != null && EnerGoProgress.Current.pendingCapture == null;
            if (!show) { if (dulang != null) { Destroy(dulang.gameObject); dulang = null; } return; }
            if (dulang == null && !BuildDulang()) return;
            PoseDulang();
            StepDulangSim(Time.deltaTime);
        }

        private bool BuildDulang()
        {
            var panModel = Resources.Load<GameObject>("Gold/sda_dulang");
            var big = Resources.Load<GameObject>("Gold/sda_emas_keping_besar");
            var small = Resources.Load<GameObject>("Gold/sda_emas_keping_kecil");
            var opaque = Resources.Load<Material>("Oil/partikel_bor");
            var clear = Resources.Load<Material>("Gold/partikel_air");
            if (panModel == null || big == null || small == null || opaque == null || clear == null)
            {
                Debug.LogError("EnerGo: missing Resources/Gold/sda_dulang, sda_emas_keping_*, partikel_air or Oil/partikel_bor.");
                return false;
            }
            EnsureModelLight();
            dulang = new GameObject("Dulang 3D").transform;
            dulang.SetParent(arCamera.transform, false);
            dulangTilt = Instantiate(panModel, dulang).transform;
            dulangWater = NewDulangMesh("Air", clear, WaterSegments, WaterRings, out waterVerts, out waterColors);
            dulangSand = NewDulangMesh("Pasir", opaque, SandSegments, SandRings, out sandVerts, out sandColors);

            dulangPieces = new Transform[GoldPieces];
            dulangPieceSpin = new Quaternion[GoldPieces];
            for (int i = 0; i < GoldPieces; i++)
            {
                // Same split as goldSize: the first four are nuggets, the rest flakes.
                dulangPieces[i] = Instantiate(i < 4 ? big : small, dulangTilt).transform;
                dulangPieceSpin[i] = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-15f, 15f));
            }

            dulangGrainSystem = MakeBorParticles(dulangTilt, "Butir Pasir", PrimitiveType.Cube, 0f, opaque);
            dulangGlints = MakeBorParticles(dulangTilt, "Kilau", PrimitiveType.Sphere, 0f, opaque);
            grainParticles = new ParticleSystem.Particle[GrainCount];
            grainPos = new Vector2[GrainCount];
            grainVel = new Vector2[GrainCount];
            grainFall = new float[GrainCount];
            grainDrop = new float[GrainCount];
            for (int i = 0; i < GrainCount; i++)
            {
                grainPos[i] = Random.insideUnitCircle * 0.17f;
                grainFall[i] = float.NaN;
                var shade = Color.Lerp(new Color(0.46f, 0.35f, 0.21f), new Color(0.72f, 0.58f, 0.38f), Random.value);
                grainParticles[i] = new ParticleSystem.Particle
                {
                    startColor = shade,
                    startSize = Random.Range(0.0035f, 0.0075f),
                    startLifetime = 1000f,
                    remainingLifetime = 1000f,
                    rotation3D = new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f))
                };
            }
            waterSlope = waterSlopeSpeed = sandCentre = Vector2.zero;
            dulangProgressBefore = goldProgress;
            dulangSimTime = 0f;
            return true;
        }

        // A disc with a centre vertex and `rings` rings of `segments` vertices; the vertices are rewritten every frame.
        private Mesh NewDulangMesh(string name, Material mat, int segments, int rings, out Vector3[] verts, out Color[] colors)
        {
            var go = new GameObject(name);
            go.transform.SetParent(dulangTilt, false);
            var mesh = new Mesh { name = name };
            mesh.MarkDynamic();
            verts = new Vector3[1 + segments * rings];
            colors = new Color[verts.Length];
            var tris = new int[segments * 3 + (rings - 1) * segments * 6];
            int t = 0;
            for (int s = 0; s < segments; s++)
            {
                int n = (s + 1) % segments;
                tris[t++] = 0; tris[t++] = 1 + n; tris[t++] = 1 + s;
                for (int r = 0; r < rings - 1; r++)
                {
                    int a = 1 + r * segments + s, b = 1 + r * segments + n, c = a + segments, d = b + segments;
                    tris[t++] = a; tris[t++] = b; tris[t++] = c;
                    tris[t++] = b; tris[t++] = d; tris[t++] = c;
                }
            }
            mesh.vertices = verts;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return mesh;
        }

        private void PoseDulang()
        {
            // Same footprint as GoldPanRect, so the HUD pill and overlays still line up.
            float sw = Screen.width / HudScale, sh = Screen.height / HudScale;
            GoldPanRect(sw, sh, out Vector2 c, out float R);
            var px = new Vector3(c.x * HudScale, Screen.height - c.y * HudScale, DulangDistance);
            dulang.position = arCamera.ScreenToWorldPoint(px);
            float worldR = DulangPose.WorldRadius(R * HudScale, DulangDistance, arCamera.fieldOfView, Screen.height);
            dulang.localScale = Vector3.one * (worldR / DulangRimRadius);
            // Pan-local x = screen right, z = screen up, y = toward the viewer.
            dulang.localRotation = DulangPose.Rest;
            modelLight.transform.rotation = arCamera.transform.rotation * Quaternion.Euler(50f, -35f, 0f);
            dulangTilt.localRotation = DulangPose.Tilt(goldTilt);
        }

        // Advances water, sand, grains and gold by dt. Split from the pose so a run can be stepped by hand.
        private void StepDulangSim(float dt)
        {
            dulangSimTime += dt;
            float swirlNorm = Mathf.Clamp01(Mathf.Abs(goldSwirl) / 240f);

            // Level water climbs the low side of the tipped pan; the spring lets it lag and slosh.
            float tipDeg = Mathf.Min(goldTilt.magnitude * DulangPose.DegPerTilt, DulangPose.MaxTiltDeg);
            Vector2 wantSlope = goldTilt.sqrMagnitude > 1e-6f ? goldTilt.normalized * Mathf.Tan(tipDeg * Mathf.Deg2Rad) * 0.42f : Vector2.zero;
            waterSlopeSpeed += ((wantSlope - waterSlope) * 70f - waterSlopeSpeed * 7f) * dt;
            waterSlope += waterSlopeSpeed * dt;
            sandCentre = Vector2.Lerp(sandCentre, Vector2.ClampMagnitude(goldTilt * 0.14f, 0.045f), 1f - Mathf.Exp(-4f * dt));

            WriteSand();
            WriteWater(swirlNorm);
            StepGrains(dt);
            PoseGold(dt);
            dulangProgressBefore = goldProgress;
        }

        // The sand surface at a point on the pan: its mound lies on the bowl.
        private float SandTop(Vector2 p) =>
            DulangMath.InnerHeight(p.magnitude) + DulangMath.SandThickness((p - sandCentre).magnitude, goldProgress);

        private void WriteSand()
        {
            float radius = DulangMath.SandRadius(goldProgress), left = 1f - goldProgress;
            var wet = new Color(0.36f, 0.27f, 0.16f); var dry = new Color(0.58f, 0.45f, 0.28f);
            sandVerts[0] = new Vector3(sandCentre.x, SandTop(sandCentre) + 0.0006f, sandCentre.y);
            sandColors[0] = dry;
            for (int s = 0; s < SandSegments; s++)
            {
                float phi = s * Mathf.PI * 2f / SandSegments;
                float lump = 1f + 0.12f * Mathf.Sin(phi * 5f + 1.3f) * Mathf.Sin(phi * 3f + 0.4f);
                for (int r = 0; r < SandRings; r++)
                {
                    float k = (r + 1f) / SandRings, d = radius * k * lump;
                    Vector2 p = sandCentre + new Vector2(Mathf.Cos(phi), Mathf.Sin(phi)) * d;
                    if (p.magnitude > DulangMath.InnerRim) p = p.normalized * DulangMath.InnerRim;
                    float ripple = 0.0012f * left * Mathf.Sin(d * 90f + phi * 3f) * (1f - k);
                    int v = 1 + r * SandSegments + s;
                    sandVerts[v] = new Vector3(p.x, SandTop(p) + ripple + (r == SandRings - 1 ? -0.0015f : 0.0006f), p.y);
                    // Lit from the upper left, wetter toward the edge, with a grainy variation.
                    float light = 0.5f + 0.5f * (-Mathf.Cos(phi) * 0.5f + Mathf.Sin(phi) * 0.5f) * k;
                    float speck = Mathf.Sin(s * 12.9f + r * 78.2f) * 0.5f + 0.5f;
                    sandColors[v] = Color.Lerp(wet, dry, Mathf.Clamp01(0.65f - k * 0.45f + light * 0.3f + speck * 0.12f));
                }
            }
            dulangSand.vertices = sandVerts;
            dulangSand.colors = sandColors;
        }

        private void WriteWater(float swirlNorm)
        {
            float t = dulangSimTime, dir = goldSwirl >= 0f ? 1f : -1f;
            float waveHeight = 0.0012f + 0.0034f * swirlNorm;
            // Thin and clear, so the sand and gold under it keep their colour.
            var deep = new Color(0.45f, 0.68f, 0.72f, 0.1f); var shine = new Color(0.72f, 0.9f, 0.95f, 0.28f); var foam = new Color(0.92f, 0.97f, 1f, 0.38f);
            float Plane(Vector2 p) => DulangWaterLevel + Vector2.Dot(p, waterSlope);
            waterVerts[0] = new Vector3(0f, Plane(Vector2.zero), 0f);
            waterColors[0] = deep;
            for (int s = 0; s < WaterSegments; s++)
            {
                float phi = s * Mathf.PI * 2f / WaterSegments;
                float edge = DulangMath.WaterEdge(phi, DulangWaterLevel, waterSlope);
                var unit = new Vector2(Mathf.Cos(phi), Mathf.Sin(phi));
                for (int r = 0; r < WaterRings; r++)
                {
                    float k = (r + 1f) / WaterRings, d = edge * k;
                    Vector2 p = unit * d;
                    // The swirl pushes a three-armed wave round the pan; it dies at the wall so the edge stays on it.
                    float wave = Mathf.Sin(phi * 3f - dir * t * (2f + swirlNorm * 6f) + d * 26f);
                    float y = Plane(p) + waveHeight * wave * (1f - k * k);
                    int v = 1 + r * WaterSegments + s;
                    waterVerts[v] = new Vector3(p.x, y + 0.0004f, p.y);
                    Color c = Color.Lerp(deep, shine, Mathf.Clamp01(wave * 0.5f + 0.15f) * (0.35f + 0.65f * swirlNorm));
                    waterColors[v] = r == WaterRings - 1 ? foam : c;
                }
            }
            dulangWater.vertices = waterVerts;
            dulangWater.colors = waterColors;
        }

        // Loose grains ride round with the swirl, drift downhill, and wash over the low rim as the sand goes.
        private void StepGrains(float dt)
        {
            int keep = Mathf.RoundToInt(GrainCount * Mathf.Clamp01(1f - goldProgress));
            bool washing = goldProgress > dulangProgressBefore;
            float spin = goldSwirl * Mathf.Deg2Rad * 0.55f;
            Vector2 low = goldTilt.sqrMagnitude > 1e-6f ? goldTilt.normalized : Vector2.zero;
            for (int i = 0; i < GrainCount; i++)
            {
                ref var g = ref grainParticles[i];
                if (!float.IsNaN(grainFall[i]))
                {
                    // Over the rim: carried outward and falling away from the pan.
                    grainDrop[i] -= 1.1f * dt;
                    grainFall[i] += grainDrop[i] * dt;
                    grainPos[i] += grainVel[i] * dt;
                    float height = DulangMath.InnerHeight(DulangMath.InnerRim) + grainFall[i];
                    g.position = new Vector3(grainPos[i].x, height, grainPos[i].y);
                    if (height >= -0.25f) { g.remainingLifetime = 1000f; continue; }
                    grainFall[i] = float.NaN;
                    grainPos[i] = Random.insideUnitCircle * 0.1f;
                    grainVel[i] = Vector2.zero;
                }
                if (i >= keep) { g.remainingLifetime = -1f; continue; }
                Vector2 p = grainPos[i];
                Vector2 tangent = new Vector2(-p.y, p.x);
                float each = 0.6f + 0.4f * Mathf.Sin(i * 7.3f);
                Vector2 acc = (tangent * spin * each - grainVel[i]) * 5f + goldTilt * 0.55f - p * 0.5f;
                grainVel[i] += acc * dt;
                p += grainVel[i] * dt;
                float r = p.magnitude;
                if (washing && r > 0.17f && Vector2.Dot(p, low) > r * 0.5f && i >= keep - 12)
                {
                    // Near the low rim while the sand is washing: this one goes over.
                    grainFall[i] = 0f;
                    grainDrop[i] = Random.Range(0.1f, 0.3f);
                    grainPos[i] = p / r * DulangMath.InnerRim;
                    grainVel[i] = p / r * Random.Range(0.12f, 0.28f);
                    g.position = new Vector3(grainPos[i].x, DulangMath.InnerHeight(DulangMath.InnerRim), grainPos[i].y);
                    g.remainingLifetime = 1000f;
                    continue;
                }
                if (r > 0.205f) { p = p / r * 0.205f; grainVel[i] *= 0.4f; }
                grainPos[i] = p;
                g.position = new Vector3(p.x, SandTop(p) + 0.002f, p.y);
                g.remainingLifetime = 1000f;
            }
            dulangGrainSystem.SetParticles(grainParticles, GrainCount);
        }

        // Pieces lie on the bowl under the sand, so washing it away uncovers them, and they catch the light once they show.
        private void PoseGold(float dt)
        {
            for (int i = 0; i < GoldPieces; i++)
            {
                var p = dulangPieces[i];
                float scale = 1.2f * goldSize[i] / (i < 4 ? 7.5f : 4f);
                if (!goldLost[i])
                {
                    p.gameObject.SetActive(true);
                    // goldPos is in GUI space (y down); pan-local z is screen up.
                    Vector2 at = new Vector2(goldPos[i].x, -goldPos[i].y) * DulangGoldRadius;
                    p.localPosition = new Vector3(at.x, DulangMath.GoldHeight(at.magnitude), at.y);
                    p.localScale = Vector3.one * scale;
                    // Rolls a little as it slides.
                    Vector2 v = goldVel[i];
                    if (v.sqrMagnitude > 1e-5f)
                        dulangPieceSpin[i] = Quaternion.AngleAxis(v.magnitude * dt * 420f, new Vector3(v.y, 0f, v.x).normalized) * dulangPieceSpin[i];
                    p.localRotation = dulangPieceSpin[i];
                    continue;
                }
                // Spilled: thrown over the rim, then falls away tumbling.
                float t = (Time.time - goldLostAt[i]) / 0.9f;
                if (t >= 1f) { p.gameObject.SetActive(false); continue; }
                Vector2 d = goldLostDir[i];
                var outward = new Vector3(d.x, 0f, -d.y) * DulangRimRadius * (0.85f + t * 0.8f);
                p.localPosition = outward + Vector3.up * (DulangMath.InnerHeight(DulangMath.InnerRim) + 0.16f * t - 0.5f * t * t);
                p.localScale = Vector3.one * scale;
                p.Rotate(240f * dt, 620f * dt, 0f, Space.Self);
            }

            // A glint now and then on a piece the sand has uncovered.
            if (dulangSimTime < dulangNextGlintAt) return;
            dulangNextGlintAt = dulangSimTime + Random.Range(0.12f, 0.4f);
            int pick = Random.Range(0, GoldPieces);
            if (goldLost[pick]) return;
            Vector2 where = new Vector2(goldPos[pick].x, -goldPos[pick].y) * DulangGoldRadius;
            if (DulangMath.SandThickness((where - sandCentre).magnitude, goldProgress) > GoldBuried * 0.6f) return;
            var pos = new Vector3(where.x, SandTop(where) + 0.012f, where.y);
            EmitBor(dulangGlints, pos, Vector3.up * 0.01f, new Color(1f, 0.97f, 0.75f), Random.Range(0.012f, 0.02f), 0.22f);
        }
    }
}
