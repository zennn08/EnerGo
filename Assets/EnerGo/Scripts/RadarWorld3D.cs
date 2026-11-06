using System.Collections.Generic;
using UnityEngine;

namespace EnerGo
{
    // Draws the radar as a small chibi world behind the UI: the player stays at the centre, the land slides under them,
    // and the view turns with the compass. Display only; spawns and rules stay in RadarController and GeoSpawner.
    public class RadarWorld3D : MonoBehaviour
    {
        private const int ViewCells = 3;
        private const float PropSize = 8f, MarkerSize = 30f, MarkerLift = 8f, GroundY = RadarWorld.RingHeightM, TileM = 60f;
        private const float CameraPitch = 58f, CameraDistance = 1140f, CameraFov = 28f, AimAhead = -55f;
        private const float RangeM = 150f, SweepDegPerSec = 90f, SweepWidthDeg = 40f;
        private const float DayLight = 0.85f, NightLight = 0.45f;
        private static readonly Color DaySky = new Color(0.62f, 0.83f, 0.93f), NightSky = new Color(0.07f, 0.11f, 0.2f);
        private static readonly Color DayAmbient = new Color(0.55f, 0.56f, 0.58f), NightAmbient = new Color(0.25f, 0.3f, 0.45f);

        private sealed class Marker
        {
            public Transform Root, Model;
            public Renderer Ring;
            public string ResourceId;
        }

        private RadarController controller;
        private Camera cam;
        private Transform rig, land, sweep, playerMark;
        private readonly Dictionary<(long, long), GameObject> cells = new Dictionary<(long, long), GameObject>();
        private readonly Dictionary<string, Marker> markers = new Dictionary<string, Marker>();
        private readonly Dictionary<string, GameObject> models = new Dictionary<string, GameObject>();
        private readonly List<RadarNode> nodes = new List<RadarNode>();
        private readonly HashSet<string> seen = new HashSet<string>();
        private Vector2[] screenPoints = new Vector2[32];
        private string[] screenIds = new string[32];
        private int screenCount;
        private Material ringPlain, ringNear, ringSelected;
        private Mesh markerRing;
        private double anchorEast, anchorNorth;
        private long shownX = long.MinValue, shownZ = long.MinValue;
        private bool anchored;

        public void Init(RadarController owner) => controller = owner;

        private void Start()
        {
            Application.targetFrameRate = 30; // a map does not need 60, and the walk is long

            bool day = GeoSpawner.IsDaytime(System.DateTime.Now.Hour);
            cam = Camera.main;
            cam.orthographic = false;
            cam.fieldOfView = CameraFov;
            cam.nearClipPlane = 100f;
            cam.farClipPlane = 2600f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = day ? DaySky : NightSky;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = day ? DayAmbient : NightAmbient;
            RenderSettings.fog = false;

            var lightGo = new GameObject("Matahari peta");
            var sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = day ? DayLight : NightLight;
            sun.color = day ? new Color(1f, 0.96f, 0.86f) : new Color(0.65f, 0.75f, 1f);
            sun.shadows = LightShadows.None;
            lightGo.transform.rotation = Quaternion.Euler(52f, 35f, 0f);

            rig = new GameObject("Kamera peta").transform;
            cam.transform.SetParent(rig, false);
            float pitch = CameraPitch * Mathf.Deg2Rad;
            cam.transform.localPosition = new Vector3(0f, Mathf.Sin(pitch) * CameraDistance, -Mathf.Cos(pitch) * CameraDistance + AimAhead);
            cam.transform.localRotation = Quaternion.Euler(CameraPitch, 0f, 0f);

            land = new GameObject("Daratan").transform;

            ringPlain = Resources.Load<Material>("Radar/mat_radar_cincin");
            ringNear = Resources.Load<Material>("Radar/mat_radar_cincin_dekat");
            ringSelected = Resources.Load<Material>("Radar/mat_radar_cincin_pilih");
            markerRing = RingMesh(0.82f, 1f, 40);

            AddFlat("Batas radar", RingMesh(1f - 4f / RangeM, 1f, 96), RangeM, ringPlain);
            AddFlat("Jangkauan tangkap", RingMesh(0.9f, 1f, 48), (float)GeoSpawner.CaptureRadiusM, ringNear);
            sweep = AddFlat("Sapuan radar", WedgeMesh(SweepWidthDeg, 16), RangeM, Resources.Load<Material>("Radar/mat_radar_sapu")).transform;
            sweep.position = Vector3.up * (GroundY - 0.2f);

            playerMark = Instantiate(Resources.Load<GameObject>("Radar/radar_penanda")).transform;
            playerMark.localScale = Vector3.one * 8f;
            playerMark.position = Vector3.up * GroundY;
        }

        private GameObject AddFlat(string name, Mesh mesh, float radius, Material material)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.transform.localScale = new Vector3(radius, 1f, radius);
            go.transform.position = Vector3.up * GroundY;
            return go;
        }

        private void LateUpdate()
        {
            if (controller == null || rig == null) return;
            rig.rotation = Quaternion.Euler(0f, controller.Heading, 0f);
            playerMark.rotation = Quaternion.Euler(0f, controller.Heading + 180f, 0f); // the model's arrow points to its back
            sweep.rotation = Quaternion.Euler(0f, Time.unscaledTime * SweepDegPerSec % 360f, 0f);
            UpdateLand();
            UpdateMarkers();
        }

        private void UpdateLand()
        {
            RadarWorld.ToMetres(controller.Lat, controller.Lon, out double east, out double north);
            if (!anchored) { anchorEast = east; anchorNorth = north; anchored = true; }
            land.position = new Vector3((float)(anchorEast - east), 0f, (float)(anchorNorth - north));
            long cx = RadarWorld.CellOf(east), cz = RadarWorld.CellOf(north);
            if (cx == shownX && cz == shownZ) return;
            shownX = cx;
            shownZ = cz;

            var gone = new List<(long, long)>();
            foreach (var kv in cells)
                if (System.Math.Abs(kv.Key.Item1 - cx) > ViewCells || System.Math.Abs(kv.Key.Item2 - cz) > ViewCells) gone.Add(kv.Key);
            foreach (var key in gone) { Destroy(cells[key]); cells.Remove(key); }
            for (long x = cx - ViewCells; x <= cx + ViewCells; x++)
                for (long z = cz - ViewCells; z <= cz + ViewCells; z++)
                    if (!cells.ContainsKey((x, z))) cells[(x, z)] = BuildCell(x, z);
        }

        private GameObject BuildCell(long x, long z)
        {
            var cell = new GameObject("Petak " + x + "," + z);
            cell.transform.SetParent(land, false);
            cell.transform.localPosition = new Vector3(
                (float)((x + 0.5) * RadarWorld.CellM - anchorEast), 0f, (float)((z + 0.5) * RadarWorld.CellM - anchorNorth));
            var ground = Instantiate(Model("Radar/radar_tanah"), cell.transform);
            ground.transform.localPosition = Vector3.zero;
            ground.transform.localRotation = Quaternion.Euler(0f, ((x * 31 + z * 17) % 4 + 4) % 4 * 90f, 0f);
            ground.transform.localScale = Vector3.one * (RadarWorld.CellM / TileM);
            foreach (var p in RadarWorld.PropsInCell(x, z))
            {
                var prop = Instantiate(Model("Radar/radar_" + p.Kind.ToString().ToLowerInvariant()), cell.transform);
                prop.transform.localPosition = new Vector3(p.X, 0.5f, p.Z);
                prop.transform.localRotation = Quaternion.Euler(0f, p.Yaw, 0f);
                prop.transform.localScale = Vector3.one * (PropSize * p.Scale);
            }
            return cell;
        }

        private GameObject Model(string path)
        {
            if (!models.TryGetValue(path, out var m)) models[path] = m = Resources.Load<GameObject>(path);
            return m;
        }

        private static string ModelPath(string resourceId)
        {
            switch (resourceId)
            {
                case ResourceIds.Sun: return "Sun/sun";
                case ResourceIds.Coal: return "Coal/bara_1";
                case ResourceIds.Gold: return "Gold/sda_emas_keping_besar";
                case ResourceIds.Oil: return "Oil/sda_pompa";
                default: return "Uranium/sda_uranium_bijih";
            }
        }

        private void UpdateMarkers()
        {
            nodes.Clear();
            seen.Clear();
            controller.FillNodes(nodes);
            if (screenPoints.Length < nodes.Count) { screenPoints = new Vector2[nodes.Count]; screenIds = new string[nodes.Count]; }
            screenCount = 0;
            float t = Time.unscaledTime;
            foreach (var n in nodes)
            {
                seen.Add(n.Id);
                if (!markers.TryGetValue(n.Id, out var m)) markers[n.Id] = m = BuildMarker(n);
                var at = RadarWorld.Around(n.Bearing, n.Distance);
                m.Root.position = new Vector3(at.x, GroundY, at.y);
                bool chosen = n.Id == controller.SelectedId;
                m.Ring.sharedMaterial = chosen ? ringSelected : n.InRange ? ringNear : ringPlain;
                m.Model.localPosition = Vector3.up * (MarkerLift + Mathf.Sin(t * 2f + n.Distance.GetHashCode()) * 1.2f);
                m.Model.localRotation = Quaternion.Euler(0f, t * 40f, 0f);
                float pulse = n.ResourceId == ResourceIds.Uranium || n.InRange ? 1f + 0.08f * Mathf.Sin(t * 5f) : 1f; // rare or reachable: keep drawing the eye
                m.Root.localScale = Vector3.one * pulse;
                screenPoints[screenCount] = cam.WorldToScreenPoint(m.Root.position + Vector3.up * MarkerLift);
                screenIds[screenCount++] = n.Id;
            }
            if (markers.Count > seen.Count)
            {
                var old = new List<string>();
                foreach (var id in markers.Keys) if (!seen.Contains(id)) old.Add(id);
                foreach (var id in old) { Destroy(markers[id].Root.gameObject); markers.Remove(id); }
            }
        }

        private Marker BuildMarker(RadarNode n)
        {
            var root = new GameObject("SDA " + n.ResourceId).transform;
            var holder = new GameObject("Model").transform;
            holder.SetParent(root, false);
            var loaded = Model(ModelPath(n.ResourceId));
            if (loaded != null) FitInto(Instantiate(loaded, holder), MarkerSize);
            var ring = new GameObject("Cincin");
            ring.transform.SetParent(root, false);
            ring.transform.localScale = new Vector3(MarkerSize * 0.6f, 1f, MarkerSize * 0.6f);
            ring.AddComponent<MeshFilter>().sharedMesh = markerRing;
            var renderer = ring.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ringPlain;
            return new Marker { Root = root, Model = holder, Ring = renderer, ResourceId = n.ResourceId };
        }

        // Models come in very different sizes: scale to one size and stand them on the holder's origin.
        private static void FitInto(GameObject model, float size)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            float longest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (longest <= 0.0001f) return;
            float s = size / longest;
            model.transform.localScale *= s;
            model.transform.position += (model.transform.parent.position - b.center) * s + Vector3.up * (b.extents.y * s);
        }

        // Screen pixels (origin bottom left) -> the spawn under a tap, or null.
        public string PickAt(Vector2 screen, float reachPx)
        {
            int i = RadarWorld.NearestWithin(screen, screenPoints, screenCount, reachPx);
            return i >= 0 ? screenIds[i] : null;
        }

        // Where north lies on screen, for the compass letter.
        public Vector2 NorthOnScreen(float metres) => cam != null ? (Vector2)cam.WorldToScreenPoint(new Vector3(0f, GroundY, metres)) : Vector2.zero;

        private static Mesh RingMesh(float inner, float outer, int segments)
        {
            var verts = new Vector3[segments * 2];
            var tris = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                verts[i * 2] = dir * inner;
                verts[i * 2 + 1] = dir * outer;
                int n = (i + 1) % segments;
                tris[i * 6] = i * 2; tris[i * 6 + 1] = i * 2 + 1; tris[i * 6 + 2] = n * 2 + 1;
                tris[i * 6 + 3] = i * 2; tris[i * 6 + 4] = n * 2 + 1; tris[i * 6 + 5] = n * 2;
            }
            var mesh = new Mesh { vertices = verts, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh WedgeMesh(float widthDeg, int segments)
        {
            var verts = new Vector3[segments + 2];
            var tris = new int[segments * 3];
            verts[0] = Vector3.zero;
            for (int i = 0; i <= segments; i++)
            {
                float a = -widthDeg * Mathf.Deg2Rad * i / segments;
                verts[i + 1] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            }
            for (int i = 0; i < segments; i++)
            {
                tris[i * 3] = 0; tris[i * 3 + 1] = i + 2; tris[i * 3 + 2] = i + 1;
            }
            var mesh = new Mesh { vertices = verts, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
