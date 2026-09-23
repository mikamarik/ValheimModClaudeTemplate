#if DEBUG
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MyValheimMod.Dev
{
    /// <summary>
    /// The world side of the tests: one flat build spot every scenario shares, the hammer, pieces
    /// the test character left behind, screenshots, and small helpers.
    /// </summary>
    internal static partial class AutoTest
    {
        /// <summary>The flat spot the world-building scenarios share, found once a run.</summary>
        private static readonly Quaternion BuildFacing = Quaternion.identity;

        /// <summary>Good enough to stop looking. The levelling after it does the real flattening.</summary>
        private const float FlatEnough = 0.4f;

        private static Vector3 _buildSpot;
        private static bool _haveBuildSpot;

        // ---------- scenario: build_spot ----------

        /// <summary>The shared build spot and the hammer, the start of every scenario that builds.</summary>
        private static IEnumerator TestBuildSpot(Player player)
        {
            yield return MoveToBuildSpot(player);
            yield return EquipHammer(player);
            RemovePlayerPieces(player, 60f);
            yield return Screenshot("build-spot");
        }

        // ---------- the build spot ----------

        /// <summary>
        /// The spawn stones are a no-build zone, so go to the flattest dry meadow near the middle of
        /// the world. The search starts from a fixed point and the player lands facing a fixed way,
        /// so every scenario in a run, and every run, builds on the same ground. Without that a
        /// chained run shifts the spot by a metre each time and a piece can lose its support.
        /// Heights come from the world generator, which needs no loaded area; the teleport waits for it.
        /// </summary>
        private static IEnumerator MoveToBuildSpot(Player player)
        {
            if (!_haveBuildSpot)
            {
                var best = Vector3.zero;
                var bestBumps = float.MaxValue;
                for (var distance = 60f; distance <= 600f && bestBumps > FlatEnough; distance += 20f)
                {
                    for (var angle = 0; angle < 360 && bestBumps > FlatEnough; angle += 15)
                    {
                        // Polar sweep from the middle of the world, where the spawn stones stand.
                        var spot = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance;
                        var bumps = Bumpiness(spot, out var height);
                        if (bumps < bestBumps)
                        {
                            bestBumps = bumps;
                            best = new Vector3(spot.x, height, spot.z);
                        }
                    }
                }

                if (bestBumps == float.MaxValue)
                {
                    Check(false, "a dry meadow within 600 m of the middle of the world (change AutoTest.WorldSeed?)");
                    yield break;
                }

                _buildSpot = best;
                _haveBuildSpot = true;
                Log($"build spot {V(best)}, {best.magnitude:0} m from the middle, ground varies {bestBumps:0.00} m");
            }

            yield return new WaitForSeconds(2.5f); // teleport cooldown after spawning
            Check(player.TeleportTo(_buildSpot + Vector3.up, BuildFacing, true), "teleport to the build spot");
            while (player.IsTeleporting())
            {
                yield return null;
            }

            // The same facing every time: the aim, and so the spot a piece lands on, follows it.
            player.m_lookYaw = BuildFacing;
            player.m_lookPitch = 0f;
            yield return new WaitForSeconds(3f);
            ClearVegetation(player.transform.position, 20f);
            yield return new WaitForSeconds(1f);
            var before = Standing(player.transform.position, 8f);
            yield return LevelGround(player.transform.position, 14f);
            Check(Standing(player.transform.position, 8f) < 0.1f,
                $"the ground is flat: it varied {before:0.00} m, now {Standing(player.transform.position, 8f):0.00} m");
            Check(!Location.IsInsideNoBuildLocation(player.transform.position), $"moved to {V(player.transform.position)}");
            Check(!player.IsSwimming(), "on dry land (swimming puts the hammer away)");
        }

        /// <summary>
        /// Height spread of the ground in a 16 m square, sampled every 2 m; MaxValue for water or
        /// another biome. A coarse pass throws away the steep hills first. Meadows roll by 1 to 2 m
        /// over 16 m, so the coarse limit is 3 m: at 1 m, the seed AUTOTST1 had no spot at all
        /// within 400 m and the player landed in the sea.
        /// </summary>
        private static float Bumpiness(Vector3 spot, out float height)
        {
            var world = WorldGenerator.instance;
            var water = ZoneSystem.instance.m_waterLevel;
            height = world.GetHeight(spot.x, spot.z);
            if (world.GetBiome(spot) != Heightmap.Biome.Meadows || height < water + 2f)
            {
                return float.MaxValue;
            }

            if (Spread(world, spot, water, 8f, 8f, height) > 3f)
            {
                return float.MaxValue;   // a hill: not worth the dense sweep
            }

            return Spread(world, spot, water, 8f, 2f, height);
        }

        private static float Spread(WorldGenerator world, Vector3 spot, float water, float reach, float step, float height)
        {
            var min = height;
            var max = height;
            for (var x = -reach; x <= reach; x += step)
            {
                for (var z = -reach; z <= reach; z += step)
                {
                    var h = world.GetHeight(spot.x + x, spot.z + z);
                    if (h < water + 1f)
                    {
                        return float.MaxValue;
                    }

                    min = Mathf.Min(min, h);
                    max = Mathf.Max(max, h);
                }
            }

            return max - min;
        }

        /// <summary>
        /// Levels the ground, the way a player levels a site with the hoe before building. Valheim's
        /// meadows roll by 1 to 2 m over a 16 m square, and no seed has a square flat enough for a
        /// building to stand on everywhere, so without this a support check is a coin flip.
        ///
        /// A TerrainOp applies itself to every heightmap in range in its own Awake and then deletes
        /// itself, so the object is built switched off and switched on once its settings are in.
        /// </summary>
        private static IEnumerator LevelGround(Vector3 center, float radius)
        {
            var settings = new TerrainOp.Settings
            {
                m_level = true,
                m_levelRadius = radius,
                m_square = true,
                m_smooth = false,
                m_paintCleared = false,
            };

            // The operation travels as a routed RPC that carries only the op's prefab name, and the
            // far end reads the settings out of ObjectDB. A made-up op has to be registered there
            // or it arrives as an error and nothing happens. The keeper is switched off, so its own
            // Awake never fires; only the live object below applies itself. Test code only: the
            // mod itself sends no RPC.
            var name = MyValheimModPlugin.PluginName + "_LevelGround";
            var hash = name.GetStableHashCode();
            var keeper = new GameObject(name);
            keeper.SetActive(false);
            keeper.AddComponent<TerrainOp>().m_settings = settings;
            ObjectDB.instance.m_terrainOpsByHash[hash] = keeper.GetComponent<TerrainOp>();

            var go = new GameObject(name);
            go.SetActive(false);
            go.transform.position = center;
            go.AddComponent<TerrainOp>().m_settings = settings;
            go.SetActive(true);   // Awake applies it to every heightmap in range, then deletes it

            yield return new WaitForSeconds(1.5f);
            ObjectDB.instance.m_terrainOpsByHash.Remove(hash);
            Object.Destroy(keeper);
            Log($"levelled the ground {radius:0} m around {V(center)}");
        }

        /// <summary>The height spread of the loaded ground, which is what a piece actually stands on.</summary>
        private static float Standing(Vector3 center, float reach)
        {
            var min = float.MaxValue;
            var max = float.MinValue;
            for (var x = -reach; x <= reach; x += 2f)
            {
                for (var z = -reach; z <= reach; z += 2f)
                {
                    var at = new Vector3(center.x + x, center.y + 50f, center.z + z);
                    if (Physics.Raycast(at, Vector3.down, out var hit, 200f, LayerMask.GetMask("terrain")))
                    {
                        min = Mathf.Min(min, hit.point.y);
                        max = Mathf.Max(max, hit.point.y);
                    }
                }
            }

            return max > min ? max - min : 0f;
        }

        /// <summary>Trees and rocks would catch the aim and block the view in screenshots.</summary>
        private static void ClearVegetation(Vector3 center, float radius)
        {
            var removed = 0;
            foreach (var collider in Physics.OverlapSphere(center, radius))
            {
                var view = collider.GetComponentInParent<ZNetView>();
                if (view == null || !view.IsValid() || view.GetComponent<Piece>() != null || view.GetComponent<Character>() != null)
                {
                    continue;
                }

                if (view.GetComponent<TreeBase>() || view.GetComponent<TreeLog>() || view.GetComponent<Destructible>()
                    || view.GetComponent<MineRock>() || view.GetComponent<MineRock5>() || view.GetComponent<Pickable>())
                {
                    ZNetScene.instance.Destroy(view.gameObject);
                    removed++;
                }
            }

            Log($"cleared {removed} trees and rocks around the build spot");
        }

        // ---------- the character ----------

        /// <summary>
        /// The hammer in hand, repaired. The test character is saved on quit, so its hammer wears
        /// down run after run; at 0 the game will not equip it, and build mode quietly never starts.
        /// </summary>
        private static IEnumerator EquipHammer(Player player)
        {
            var inventory = player.GetInventory();
            var hammer = inventory.GetAllItems().FirstOrDefault(i => i.m_dropPrefab && i.m_dropPrefab.name == "Hammer")
                ?? inventory.AddItem("Hammer", 1, 1, 0, 0L, "", false);

            hammer.m_durability = hammer.GetMaxDurability();
            if (!player.IsItemEquiped(hammer))
            {
                player.EquipItem(hammer);
            }

            yield return new WaitForSeconds(0.5f);
            Check(player.InPlaceMode(), "hammer equipped, build mode on");
        }

        /// <summary>Everything the character carries goes, but the items named.</summary>
        private static void ClearInventoryExcept(Player player, params string[] keep)
        {
            var inventory = player.GetInventory();
            foreach (var item in inventory.GetAllItems().ToList())
            {
                if (!(item.m_dropPrefab && keep.Contains(item.m_dropPrefab.name)))
                {
                    inventory.RemoveItem(item);
                }
            }
        }

        /// <summary>
        /// Pieces the test character built in an earlier run. The test world is saved on quit, so
        /// they are still standing next time.
        /// </summary>
        private static void RemovePlayerPieces(Player player, float radius)
        {
            var pieces = new List<Piece>();
            Piece.GetAllPiecesInRadius(player.transform.position, radius, pieces);
            var removed = 0;
            foreach (var piece in pieces.Where(p => p.GetCreator() == player.GetPlayerID()))
            {
                ZNetScene.instance.Destroy(piece.gameObject);
                removed++;
            }

            Log($"removed {removed} pieces the test character had built");
        }

        // ---------- screenshots and helpers ----------

        /// <summary>
        /// A PNG in the output folder. Every run of autotest.sh deletes the old ones first. A check
        /// that something looks right is not done until someone opened the picture and said what
        /// is in it.
        /// </summary>
        private static IEnumerator Screenshot(string name)
        {
            yield return new WaitForEndOfFrame();
            var path = Path.Combine(OutDir, name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSeconds(0.5f);
            Log("screenshot " + path);
        }

        /// <summary>A UI rectangle in screen pixels. Every canvas the game draws its windows on is an overlay.</summary>
        private static Rect ScreenBox(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        /// <summary>The whole rectangle is on the screen.</summary>
        private static bool OnScreen(RectTransform rect)
        {
            var box = ScreenBox(rect);
            return box.xMin >= 0f && box.yMin >= 0f && box.xMax <= Screen.width && box.yMax <= Screen.height
                && box.width > 0f && box.height > 0f;
        }

        private static string V(Vector3 v)
        {
            return $"({v.x:0.##},{v.y:0.##},{v.z:0.##})";
        }
    }
}
#endif
