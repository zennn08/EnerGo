using UnityEngine;

namespace EnerGo
{
    // The 3D drilling cross-section shown while drilling. It only displays the existing state
    // (oilLayers, oilDepth, oilSpeed, oilGasPressure, oilFailText); the rules stay in UpdateOil / OilMath.
    public partial class ARCapturePrototype
    {
        private const float BorDistance = 0.6f;   // metres in front of the camera
        private const float BorBitX = -0.18f, BorBitZ = 0.145f, BorParticleZ = 0.17f; // sda_bor model units
        private const float BorLowerSec = 0.7f;   // the bit comes down out of the derrick this fast
        private const float OilSplashHold = 0.9f; // the section stays up this long after striking oil, for the splash

        private static readonly string[] BorLayerObjects = { "sda_bor_tanah", "sda_bor_batuan", "sda_bor_penutup", "sda_bor_gas", "sda_bor_minyak", "sda_bor_air" };
        private static readonly float[] BorShake = { 0.0006f, 0.0012f, 0.0026f, 0.0008f, 0.0006f, 0.0008f }; // harder rock shakes harder
        private static readonly float[] BorChipRate = { 14f, 20f, 30f, 24f, 10f, 10f };                       // per second at full speed

        private Transform bor, borPipa, borMata, borLubang, borMenara, borGelembung;
        private Transform[] borLapis;
        private float[] borShownLayers;
        private BorPose.Slab[] borSlabs;
        private ParticleSystem borChips, borBubbles, borDrops;
        private float borLowered, borShakeNow, borSpin, borBubblePhase, borBubbleAmp, borChipCarry;
        private bool borSplashed;
        private Quaternion oilLightRotation = Quaternion.identity;

        private bool OilSectionVisible =>
            resourceId == ResourceIds.Oil && arCamera != null && oilLayers != null && EnerGoProgress.Current.pendingCapture == null
            && (oilPhase == OilPhase.Drill
                || (oilPhase == OilPhase.Failed && !oilPumping)
                || (oilPhase == OilPhase.Pump && Time.time - oilPhaseAt < OilSplashHold));

        private void UpdateBorDiorama()
        {
            if (!OilSectionVisible)
            {
                if (bor != null) { Destroy(bor.gameObject); bor = null; }
                if (resourceId == ResourceIds.Oil && modelLight != null) modelLight.transform.rotation = oilLightRotation;
                return;
            }
            if (bor == null && !BuildBor()) return;
            PoseBor();
        }

        private bool BuildBor()
        {
            var model = Resources.Load<GameObject>("Oil/sda_bor");
            var particleMat = Resources.Load<Material>("Oil/partikel_bor");
            if (model == null || particleMat == null)
            {
                Debug.LogError("EnerGo: missing Resources/Oil/sda_bor or partikel_bor.");
                return false;
            }
            EnsureModelLight();
            bor = Instantiate(model, arCamera.transform).transform;
            bor.name = "Bor 3D";
            borPipa = bor.Find("sda_bor_pipa");
            borMata = bor.Find("sda_bor_mata");
            borLubang = bor.Find("sda_bor_lubang");
            borMenara = bor.Find("sda_bor_menara");
            borGelembung = bor.Find("sda_bor_gelembung");
            borLapis = new Transform[BorLayerObjects.Length];
            for (int i = 0; i < borLapis.Length; i++) borLapis[i] = bor.Find(BorLayerObjects[i]);
            borShownLayers = null;
            borLowered = borShakeNow = borSpin = borChipCarry = 0f;
            borBubbleAmp = 0.004f;
            borSplashed = false;
            borChips = MakeBorParticles(bor, "Serpihan", PrimitiveType.Cube, -0.9f, particleMat);
            borBubbles = MakeBorParticles(bor, "Gelembung", PrimitiveType.Sphere, 0f, particleMat);
            borDrops = MakeBorParticles(bor, "Tetesan", PrimitiveType.Sphere, -0.9f, particleMat);
            return true;
        }

        // Emitted by hand with per-particle colour and speed; local space so they stay with their parent as the phone moves.
        private ParticleSystem MakeBorParticles(Transform parent, string name, PrimitiveType shape, float force, Material mat)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 300;
            var emission = ps.emission;
            emission.enabled = false;
            var shapeModule = ps.shape;
            shapeModule.enabled = false;
            var pull = ps.forceOverLifetime;
            pull.enabled = force != 0f;
            pull.space = ParticleSystemSimulationSpace.Local;
            pull.y = force;
            var shrink = ps.sizeOverLifetime;
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0f)));
            var temp = GameObject.CreatePrimitive(shape);
            temp.SetActive(false);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            renderer.sharedMaterial = mat;
            Destroy(temp);
            go.SetActive(true);
            ps.Play();
            return ps;
        }

        private static void EmitBor(ParticleSystem ps, Vector3 pos, Vector3 velocity, Color color, float size, float life)
        {
            var e = new ParticleSystem.EmitParams
            {
                position = pos,
                velocity = velocity,
                startColor = color,
                startSize = size,
                startLifetime = life,
                rotation3D = new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f))
            };
            ps.Emit(e, 1);
        }

        private void PoseBor()
        {
            float dt = Time.deltaTime;
            OilSectionRect(Screen.width / HudScale, Screen.height / HudScale, out Rect p);
            bor.position = arCamera.ScreenToWorldPoint(new Vector3(p.center.x * HudScale, Screen.height - p.y * HudScale, BorDistance));
            bor.localRotation = Quaternion.Euler(0f, 180f, 0f); // the cut face looks at the viewer
            bor.localScale = Vector3.one * DulangPose.WorldRadius(p.width * HudScale, BorDistance, arCamera.fieldOfView, Screen.height);
            modelLight.transform.rotation = arCamera.transform.rotation * Quaternion.Euler(30f, -25f, 0f); // from over the viewer's shoulder

            float height = p.height / p.width;
            if (borShownLayers != oilLayers)
            {
                borShownLayers = oilLayers;
                borSlabs = BorPose.Slabs(oilLayers, height);
                for (int i = 0; i < borLapis.Length; i++)
                {
                    borLapis[i].localPosition = new Vector3(0f, borSlabs[i].TopY, 0f);
                    borLapis[i].localScale = new Vector3(1f, borSlabs[i].ScaleY, 1f);
                }
            }

            bool drilling = oilPhase == OilPhase.Drill;
            if (drilling && oilWasHeld) borLowered = Mathf.Min(1f, borLowered + dt / BorLowerSec);
            float bitY = BorPose.BitY(oilDepth, height, borLowered);
            var layer = OilLayerNow;
            bool cutting = drilling && oilSpeed > 0.01f && bitY < 0f;

            borShakeNow = Mathf.Lerp(borShakeNow, cutting ? BorShake[(int)layer] : 0f, 1f - Mathf.Exp(-6f * dt));
            float t = Time.time, shakeX = borShakeNow * (Mathf.Sin(t * 38f) + 0.5f * Mathf.Sin(t * 61f + 1f)), shakeY = borShakeNow * 0.6f * Mathf.Sin(t * 47f + 2f);
            borSpin += oilSpeed * 3400f * dt;

            borPipa.localPosition = new Vector3(BorBitX + shakeX, BorPose.CrownY, BorBitZ);
            borPipa.localScale = new Vector3(1f, BorPose.PipeLength(bitY), 1f);
            borMata.localPosition = new Vector3(BorBitX + shakeX, bitY + shakeY, BorBitZ);
            borMata.localRotation = Quaternion.Euler(0f, borSpin, 0f);
            borMenara.localPosition = new Vector3(shakeX * 1.1f, shakeY * 0.4f, 0f);
            borLubang.localScale = new Vector3(1f, BorPose.HoleLength(bitY), 1f);

            // Bubbles grow restless with the gas pressure and while the bit is in the gas.
            borBubbleAmp = Mathf.Lerp(borBubbleAmp, layer == OilMath.Layer.Gas ? 0.014f + oilGasPressure * 0.02f : 0.004f, 1f - Mathf.Exp(-2.5f * dt));
            borBubblePhase += dt * (layer == OilMath.Layer.Gas ? 4f + oilGasPressure * 8f : 1.4f);
            borGelembung.localPosition = new Vector3(0f, borSlabs[3].TopY + borBubbleAmp * Mathf.Sin(borBubblePhase), 0f);

            var tip = new Vector3(BorBitX, bitY - 0.075f, BorParticleZ);
            if (cutting)
            {
                borChipCarry += BorChipRate[(int)layer] * Mathf.Clamp01(oilSpeed / 0.15f) * dt;
                var color = Color.Lerp(OilLayerColors[(int)layer], Color.white, 0.18f);
                for (; borChipCarry >= 1f; borChipCarry -= 1f)
                {
                    float side = Random.Range(-0.22f, 0.22f);
                    if (layer == OilMath.Layer.Gas)
                        EmitBor(borBubbles, tip, new Vector3(side * 0.5f, Random.Range(0.12f, 0.28f), 0f), Color.Lerp(color, Color.white, 0.45f), Random.Range(0.012f, 0.024f), Random.Range(0.6f, 1f));
                    else
                        EmitBor(borChips, tip, new Vector3(side, Random.Range(0.12f, 0.34f), 0f), color, Random.Range(0.012f, 0.024f), Random.Range(0.4f, 0.65f));
                }
            }
            if (oilPhase == OilPhase.Pump && !borSplashed)
            {
                borSplashed = true;
                for (int i = 0; i < 26; i++)
                    EmitBor(borDrops, tip, new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.25f, 0.6f), 0f), OilLayerColors[(int)OilMath.Layer.Oil], Random.Range(0.016f, 0.03f), Random.Range(0.5f, 0.8f));
            }
            if (oilPhase == OilPhase.Failed && Time.time - oilPhaseAt < 1f)
            {
                bool water = oilFailText == "KENA AIR!";
                var crown = new Vector3(BorBitX, BorPose.CrownY + 0.02f, BorBitZ);
                for (int i = 0; i < 2; i++)
                {
                    if (water)
                        EmitBor(borDrops, crown, new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(0.55f, 0.85f), 0f), Color.Lerp(OilLayerColors[(int)OilMath.Layer.Brine], Color.white, 0.4f), Random.Range(0.012f, 0.022f), Random.Range(0.5f, 0.8f));
                    else
                        EmitBor(borBubbles, crown, new Vector3(Random.Range(-0.08f, 0.08f), Random.Range(0.3f, 0.5f), 0f), Color.Lerp(OilLayerColors[(int)OilMath.Layer.Gas], Color.white, 0.35f), Random.Range(0.02f, 0.04f), Random.Range(0.6f, 1f));
                }
            }
        }
    }
}
