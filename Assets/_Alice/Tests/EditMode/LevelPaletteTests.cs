using System.Collections.Generic;
using System.Linq;
using Alice.Editor;
using Alice.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alice.Tests
{
    /// <summary>Level Palette geometry: painted cells become the same blocks the builders make.</summary>
    public sealed class LevelPaletteTests
    {
        private Scene _scene;

        [SetUp]
        public void SetUp()
        {
            _scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void BakedArtLinesUpWithTheBlocks()
        {
            AliceLevelGeometry.Paint(_scene, LevelBrush.Solid, new RectInt(-7, -3, 20, 3), false);
            AliceLevelGeometry.Paint(_scene, LevelBrush.OneWay, new RectInt(2, 3, 5, 1), false);
            AliceLevelGeometry.Paint(_scene, LevelBrush.BackWall, new RectInt(-7, -3, 20, 12), false);
            string prefix = AliceRoomArtBaker.BakedPathPrefix(_scene);
            try
            {
                AliceRoomArtBaker.BakeScene(_scene, false);

                // All three layers cover the room's bounding cells, from (-7, -3) to (13, 9).
                SpriteRenderer[] layers = AliceLevelGeometry.FindRoot(_scene, "World")
                    .GetComponentsInChildren<SpriteRenderer>(true)
                    .Where(renderer => renderer.enabled && renderer.sprite != null && renderer.sprite.texture.width > 16)
                    .ToArray();
                Assert.That(layers.Length, Is.EqualTo(3));
                foreach (SpriteRenderer layer in layers)
                {
                    Assert.That((Vector2)layer.bounds.min, Is.EqualTo(new Vector2(-7f, -3f)), layer.name);
                    Assert.That((Vector2)layer.bounds.max, Is.EqualTo(new Vector2(13f, 9f)), layer.name);
                }
            }
            finally
            {
                foreach (string layer in new[] { "_backwall", "_masonry", "_catwalks" })
                {
                    AssetDatabase.DeleteAsset(prefix + layer + ".png");
                }
            }
        }

        private List<BoxCollider2D> Blocks()
        {
            Transform world = AliceLevelGeometry.FindRoot(_scene, "World");
            Transform geometry = world != null ? world.Find("Geometry") : null;
            return geometry == null
                ? new List<BoxCollider2D>()
                : geometry.GetComponentsInChildren<BoxCollider2D>(true).ToList();
        }

        [Test]
        public void SolidRectangleBecomesOneBlockOfThatSize()
        {
            AliceLevelGeometry.Paint(_scene, LevelBrush.Solid, new RectInt(2, -1, 6, 3), false);

            List<BoxCollider2D> blocks = Blocks();
            Assert.That(blocks.Count, Is.EqualTo(1));
            Assert.That(blocks[0].isTrigger, Is.False);
            Assert.That(blocks[0].size, Is.EqualTo(new Vector2(6f, 3f)));
            Assert.That((Vector2)blocks[0].transform.position, Is.EqualTo(new Vector2(5f, 0.5f)));
        }

        [Test]
        public void OneWayCellsBecomeOnePlatformPerRow()
        {
            AliceLevelGeometry.Paint(_scene, LevelBrush.OneWay, new RectInt(0, 0, 4, 2), false);

            List<BoxCollider2D> blocks = Blocks();
            Assert.That(blocks.Count, Is.EqualTo(2));
            Assert.That(blocks.All(block => block.usedByEffector && block.GetComponent<PlatformEffector2D>() != null), Is.True);
            Assert.That(blocks.All(block => block.size == new Vector2(4f, 1f)), Is.True);
        }

        [Test]
        public void HazardCellsAreTriggersThatReturnAlice()
        {
            AliceLevelGeometry.Paint(_scene, LevelBrush.Hazard, new RectInt(0, -4, 3, 1), false);

            BoxCollider2D hazard = Blocks().Single();
            Assert.That(hazard.isTrigger, Is.True);
            Assert.That(hazard.GetComponent<FallHazard>(), Is.Not.Null);
        }

        [Test]
        public void SolidPaintedOverCatwalkReplacesIt()
        {
            AliceLevelGeometry.Paint(_scene, LevelBrush.OneWay, new RectInt(0, 0, 4, 1), false);
            AliceLevelGeometry.Paint(_scene, LevelBrush.Solid, new RectInt(0, 0, 4, 1), false);

            List<BoxCollider2D> blocks = Blocks();
            Assert.That(blocks.Count, Is.EqualTo(1));
            Assert.That(blocks[0].GetComponent<PlatformEffector2D>(), Is.Null);
        }

        [Test]
        public void ErasingTheMiddleSplitsTheBlock()
        {
            AliceLevelGeometry.Paint(_scene, LevelBrush.Solid, new RectInt(0, 0, 9, 1), false);
            AliceLevelGeometry.Paint(_scene, LevelBrush.Solid, new RectInt(4, 0, 1, 1), true);

            List<float> widths = Blocks().Select(block => block.size.x).OrderBy(width => width).ToList();
            Assert.That(widths, Is.EqualTo(new[] { 4f, 4f }));
        }

        [Test]
        public void BackWallHasNoCollider()
        {
            AliceLevelGeometry.Paint(_scene, LevelBrush.BackWall, new RectInt(0, 0, 5, 5), false);

            Assert.That(Blocks(), Is.Empty);
            Transform geometry = AliceLevelGeometry.FindRoot(_scene, "World").Find("Geometry");
            Assert.That(geometry.childCount, Is.EqualTo(1));
        }

        [Test]
        public void CameraFitsThePaintedSolidCells()
        {
            Room room = new GameObject("Room").AddComponent<Room>();
            AliceLevelGeometry.Paint(_scene, LevelBrush.Solid, new RectInt(-3, -2, 50, 30), false);

            Assert.That(AliceLevelRoomTools.FitCamera(_scene), Is.True);
            Assert.That(room.CameraBounds, Is.EqualTo(new Rect(-3f, -2f, 50f, 30f)));
            Assert.That(room.ControlsCamera, Is.True);
        }

        [Test]
        public void PlacedObjectsGetFreshIdsAndStandOnTheFloor()
        {
            var doorway = AssetDatabase.LoadAssetAtPath<GameObject>(AliceLevelPaletteCatalog.Root + "/Transitions/Doorway.prefab");
            var chair = AssetDatabase.LoadAssetAtPath<GameObject>(AliceLevelPaletteCatalog.Root + "/Rest/RestChair.prefab");
            Assume.That(doorway != null && chair != null, "Palette prefabs are filled by Alice/Level/Fill Palette From Existing Objects.");
            new GameObject("Room").AddComponent<Room>().RoomId = "L99";

            RoomDoorway first = AliceLevelRoomTools.Place(_scene, doorway, new Vector2(3f, 2f)).GetComponent<RoomDoorway>();
            RoomDoorway second = AliceLevelRoomTools.Place(_scene, doorway, new Vector2(9f, 2f)).GetComponent<RoomDoorway>();
            GameObject rest = AliceLevelRoomTools.Place(_scene, chair, new Vector2(5.5f, 4f));

            Assert.That(first.DoorwayId, Is.EqualTo("D_L99_01"));
            Assert.That(second.DoorwayId, Is.EqualTo("D_L99_02"));
            Assert.That(rest.GetComponent<RestPoint>().RestId, Is.EqualTo("R_L99_01"));
            BoxCollider2D trigger = first.GetComponent<BoxCollider2D>();
            float doorwayBottom = first.transform.position.y + trigger.offset.y - trigger.size.y * 0.5f;
            Assert.That(doorwayBottom, Is.EqualTo(2f).Within(0.001f));
            float chairBottom = rest.GetComponentsInChildren<Renderer>().Min(renderer => renderer.bounds.min.y);
            Assert.That(chairBottom, Is.EqualTo(4f).Within(0.001f));
            Assert.That(rest.transform.position.x, Is.EqualTo(5.5f));
        }

        [Test]
        public void CheckFlagsAMissingRoomAndAnUnsetDoorway()
        {
            Assert.That(AliceLevelRoomTools.Validate(_scene), Has.Count.EqualTo(1));

            new GameObject("Room").AddComponent<Room>();
            var doorway = new GameObject("Doorway").AddComponent<RoomDoorway>();
            doorway.gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
            List<string> problems = AliceLevelRoomTools.Validate(_scene);

            Assert.That(problems.Any(problem => problem.Contains("не задан ID")), Is.True);
            Assert.That(problems.Any(problem => problem.Contains("не задано, куда")), Is.True);
            Assert.That(problems.Any(problem => problem.Contains("ромашки")), Is.True);
        }
    }
}
