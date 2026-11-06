using UnityEngine;

namespace EnerGo
{
    // The Blender uranium models: the found ore that shakes and breaks, the glowing fragments and the lead flask
    // held at the corner of the view. Display only; the phases and rules stay in ARCapturePrototype.Uranium.cs.
    public partial class ARCapturePrototype
    {
        private const float UraniumOreScale = 1.1f;    // sda_uranium_bijih is about 0.4 m wide
        private const float UraniumOreHalfHeight = 0.2f;
        private const float WadahDistance = 0.6f, WadahModelHeight = 0.28f, WadahCentreY = 0.14f;

        private Transform uraniumRock;
        private Transform[] uraniumChunks;
        private Vector3[] uraniumChunkHome, uraniumChunkVelocity, uraniumChunkSpin;
        private float uraniumBrokeAt;
        private ParticleSystem uraniumSparks, wadahPuff;
        private Transform wadah, wadahModel, wadahTutup;
        private float wadahLandedAt = float.NegativeInfinity, wadahLidAt;

        private Transform MakeUraniumFragment(int index, Vector3 worldPos)
        {
            var model = Resources.Load<GameObject>("Uranium/sda_uranium_serpihan");
            var f = Instantiate(model, target.transform).transform;
            f.name = "Fragment " + index;
            f.position = worldPos;
            f.rotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-15f, 15f));
            f.localScale = Vector3.zero; // pops in after the ore breaks
            return f;
        }

        private void StartUraniumBreak(Vector3 worldPos)
        {
            EnsureModelLight();
            uraniumChunks = null;
            uraniumBrokeAt = -1f;
            var model = Resources.Load<GameObject>("Uranium/sda_uranium_bijih");
            uraniumRock = Instantiate(model, target.transform).transform;
            uraniumRock.name = "Bijih";
            Vector3 toCamera = arCamera.transform.position - worldPos; toCamera.y = 0f;
            uraniumRock.rotation = toCamera.sqrMagnitude > 0.001f ? Quaternion.LookRotation(toCamera, Vector3.up) : Quaternion.identity;
            uraniumRock.position = worldPos - Vector3.up * (UraniumOreHalfHeight * UraniumOreScale);
            uraniumChunkHome = new[] { uraniumRock.position };
            uraniumSparks = MakeBorParticles(target.transform, "Percikan", PrimitiveType.Sphere, -0.5f, Resources.Load<Material>("Oil/partikel_bor"));
        }

        // Called every frame from LateUpdate: moves the display models, whatever phase the game is in.
        private void UpdateUraniumVisual()
        {
            if (resourceId != ResourceIds.Uranium || arCamera == null || EnerGoProgress.Current.pendingCapture != null) return;
            if (modelLight != null) modelLight.transform.rotation = arCamera.transform.rotation * Quaternion.Euler(30f, -25f, 0f);
            float since = Time.time - uraniumRevealAt;

            if (uraniumRock != null)
            {
                if (since < UraniumVisual.BreakSec)
                {
                    float shake = UraniumVisual.OreShake(since), t = Time.time;
                    uraniumRock.position = uraniumChunkHome[0] + new Vector3(shake * Mathf.Sin(t * 57f), shake * 0.6f * Mathf.Sin(t * 69f), shake * 0.5f * Mathf.Sin(t * 81f));
                    uraniumRock.localScale = Vector3.one * (UraniumOreScale * Mathf.SmoothStep(0.2f, 1f, since / 0.15f));
                }
                else BreakUraniumRock();
            }
            AnimateUraniumChunks();
            PoseWadah(since);
        }

        private void BreakUraniumRock()
        {
            var at = uraniumChunkHome[0];
            var rotation = uraniumRock.rotation;
            Destroy(uraniumRock.gameObject);
            uraniumRock = null;
            uraniumBrokeAt = Time.time;
            var pieces = Instantiate(Resources.Load<GameObject>("Uranium/sda_uranium_pecah"), target.transform).transform;
            pieces.name = "Pecahan";
            pieces.SetPositionAndRotation(at, rotation);
            pieces.localScale = Vector3.one * UraniumOreScale;
            int n = pieces.childCount;
            uraniumChunks = new Transform[n];
            uraniumChunkHome = new Vector3[n];
            uraniumChunkVelocity = new Vector3[n];
            uraniumChunkSpin = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                var c = pieces.GetChild(i);
                uraniumChunks[i] = c;
                uraniumChunkHome[i] = c.localPosition;
                // Each quadrant flies away from the centre of the ore, along where its own mesh sits.
                var centre = c.GetComponent<MeshFilter>().sharedMesh.bounds.center;
                centre.y = 0f;
                uraniumChunkVelocity[i] = centre.normalized * Random.Range(0.45f, 0.65f) + Vector3.up * Random.Range(0.45f, 0.75f);
                uraniumChunkSpin[i] = Random.insideUnitSphere * 300f;
            }
            var local = target.transform.InverseTransformPoint(at + Vector3.up * (UraniumOreHalfHeight * UraniumOreScale));
            var glow = UraniumGlow;
            for (int i = 0; i < 46; i++)
                EmitBor(uraniumSparks, local, Random.onUnitSphere * Random.Range(0.25f, 0.9f) + Vector3.up * 0.25f, glow, Random.Range(0.012f, 0.026f), Random.Range(0.45f, 0.9f));
        }

        private void AnimateUraniumChunks()
        {
            if (uraniumChunks == null) return;
            float t = Time.time - uraniumBrokeAt;
            if (t >= UraniumVisual.ChunkLifeSec)
            {
                Destroy(uraniumChunks[0].parent.gameObject);
                uraniumChunks = null;
                return;
            }
            for (int i = 0; i < uraniumChunks.Length; i++)
            {
                uraniumChunks[i].localPosition = uraniumChunkHome[i] + UraniumVisual.ChunkOffset(uraniumChunkVelocity[i], t);
                uraniumChunks[i].localRotation = Quaternion.Euler(uraniumChunkSpin[i] * t);
                uraniumChunks[i].localScale = Vector3.one * UraniumVisual.ChunkScale(t);
            }
        }

        private void PoseWadah(float since)
        {
            bool show = (uraniumPhase == UraniumPhase.Collect || uraniumPhase == UraniumPhase.Done || uraniumPhase == UraniumPhase.Failed) && since >= UraniumVisual.BreakSec;
            if (!show)
            {
                if (wadah != null) { Destroy(wadah.gameObject); wadah = null; }
                return;
            }
            if (wadah == null)
            {
                wadah = new GameObject("Wadah Timbal").transform;
                wadah.SetParent(arCamera.transform, false);
                wadahModel = Instantiate(Resources.Load<GameObject>("Uranium/sda_uranium_wadah"), wadah).transform;
                wadahModel.localPosition = Vector3.up * -WadahCentreY;
                wadahTutup = wadahModel.Find("sda_uranium_tutup");
                wadahTutup.gameObject.SetActive(false);
                wadahPuff = MakeBorParticles(wadah, "Percikan", PrimitiveType.Sphere, -0.5f, Resources.Load<Material>("Oil/partikel_bor"));
                wadahLandedAt = float.NegativeInfinity;
            }
            // Bottom-left, above the RAD meter and clear of the JEPIT button; tipped so its mouth shows.
            wadah.position = arCamera.ScreenToWorldPoint(new Vector3(72f * HudScale, Screen.height - UraniumMath.FlaskCentreGuiY(UraniumHudTop) * HudScale, WadahDistance));
            wadah.localRotation = Quaternion.Euler(20f, 180f, 0f);
            wadah.localScale = Vector3.one * (DulangPose.WorldRadius(92f * HudScale, WadahDistance, arCamera.fieldOfView, Screen.height) / WadahModelHeight);
            float bump = UraniumVisual.FlaskBump(Time.time - wadahLandedAt);
            wadahModel.localScale = new Vector3(1f - 0.05f * bump, 1f + 0.08f * bump, 1f - 0.05f * bump);

            // The lid drops on when TUTUP is pressed and comes off again for the next fragment.
            bool closed = uraniumCollected > 0 && !uraniumBoxOpen;
            if (closed != wadahTutup.gameObject.activeSelf)
            {
                wadahTutup.gameObject.SetActive(closed);
                wadahLidAt = Time.time;
            }
            if (closed) wadahTutup.localPosition = new Vector3(0f, UraniumVisual.LidY(Time.time - wadahLidAt), 0f);
        }

        // Where a grabbed fragment flies to: the flask's mouth, or low in the view before the flask has appeared.
        private Vector3 UraniumFlaskMouth() => wadah != null
            ? wadah.TransformPoint(0f, WadahModelHeight - WadahCentreY, 0f)
            : arCamera.transform.position + arCamera.transform.forward * 0.5f - arCamera.transform.up * 0.4f;

        private void UraniumFragmentLanded()
        {
            wadahLandedAt = Time.time;
            if (wadahPuff == null) return;
            var mouth = new Vector3(0f, WadahModelHeight - WadahCentreY, 0f);
            for (int i = 0; i < 14; i++)
                EmitBor(wadahPuff, mouth, new Vector3(Random.Range(-0.18f, 0.18f), Random.Range(0.2f, 0.5f), Random.Range(-0.1f, 0.1f)), UraniumGlow, Random.Range(0.012f, 0.024f), Random.Range(0.3f, 0.55f));
        }
    }
}
