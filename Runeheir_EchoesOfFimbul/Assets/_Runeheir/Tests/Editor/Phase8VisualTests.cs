using System;
using System.Linq;
using NUnit.Framework;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Items;
using Runeheir.UI;
using Runeheir.Visuals;

namespace Runeheir.Tests
{
    /// <summary>Phase 8 visuals that need the runtime: item pictograms and costume looks.</summary>
    public sealed class Phase8VisualTests
    {
        [Test]
        public void EveryIconShape_DrawsSomething()
        {
            foreach (IconShape shape in Enum.GetValues(typeof(IconShape)))
            {
                var pixels = ItemIcons.Render(Shapes.Of(shape));
                int solid = pixels.Count(p => p.a > 200);
                Assert.Greater(solid, 300, $"{shape} is drawn");
                Assert.Less(solid, pixels.Length * 9 / 10, $"{shape} leaves a margin");
            }
        }

        [Test]
        public void EveryItem_GetsAPictogram_ThatFitsWhatItIs()
        {
            foreach (var item in ItemCatalog.All)
            {
                ItemIcons.ShapeOf(item); // never throws
            }

            var sword = ItemCatalog.All.First(i => i.IsWeapon && i.WeaponType == WeaponType.OneHandSword);
            Assert.AreEqual(IconShape.Sword, ItemIcons.ShapeOf(sword));
            Assert.AreEqual(IconShape.Bow, ItemIcons.ShapeOf(ItemCatalog.All.First(i => i.IsWeapon && i.WeaponType == WeaponType.Bow)));
            Assert.AreEqual(IconShape.Card, ItemIcons.ShapeOf(ItemCatalog.All.First(i => i.IsCard)));
            Assert.AreEqual(IconShape.Egg, ItemIcons.ShapeOf(ItemCatalog.All.First(i => i.Special == ItemSpecialEffect.HatchPet)));
            Assert.AreEqual(IconShape.Wings, ItemIcons.ShapeOf(ItemCatalog.All.First(i => i.IsCostume && i.LookAs.StartsWith("wings"))));
            Assert.AreEqual(IconShape.Potion, ItemIcons.ShapeOf(ItemCatalog.Get(ItemCatalog.LingonberryTonic)));
            var shapes = ItemCatalog.All.Select(ItemIcons.ShapeOf).Distinct().Count();
            Assert.Greater(shapes, 30, "the catalog uses most of the pictograms");
        }

        [Test]
        public void Costumes_ShowOverTheGear_InTheLookCode()
        {
            var record = CharacterFactory.Create(new CharacterCreateRequest { Name = "Look" }, 0, 0);
            record.EquipmentDataVersion = EquipmentSet.CurrentDataVersion;
            record.Equipment = new ItemStack[EquipmentSet.Positions];
            var hat = ItemCatalog.All.First(i => i.IsCostume && i.Slots == EquipSlot.CostumeUpper && i.LookAs == "tophat");
            var aura = ItemCatalog.All.First(i => i.IsCostume && i.LookAs.StartsWith("aura"));
            var wig = ItemCatalog.All.First(i => i.IsCostume && i.LookAs == "wig:4");
            record.Equipment[(int)EquipPosition.CostumeUpper] = ItemStack.NewInstance(hat);
            record.Equipment[(int)EquipPosition.CostumeLower] = ItemStack.NewInstance(aura);
            record.Equipment[(int)EquipPosition.CostumeMid] = ItemStack.NewInstance(wig);

            var look = AvatarLook.FromRecord(record);
            Assert.AreEqual(hat.Id, look.HeadUpper, "the costume hat shows instead of the helm");
            Assert.AreEqual(aura.Id, look.Aura);
            Assert.AreEqual(wig.Id, look.Wig);
            Assert.AreEqual(4, look.HairStyle, "the wig sets the hair style");

            var mirrored = AvatarLook.FromCode(AvatarLook.Code(record));
            Assert.AreEqual(hat.Id, mirrored.HeadUpper);
            Assert.AreEqual(aura.Id, mirrored.Aura);
            Assert.AreEqual(4, mirrored.HairStyle, "other players see the wig too");
        }

        [Test]
        public void EveryMonsterBody_AndVariant_Builds()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var monster in Monsters.MonsterCatalog.All)
            {
                if (!seen.Add(monster.Shape + "/" + monster.Variant))
                {
                    continue;
                }

                var holder = new UnityEngine.GameObject("Holder").transform;
                try
                {
                    PlaceholderAvatar.CreateMonster(holder, monster);
                    Assert.Greater(holder.GetComponentsInChildren<UnityEngine.Renderer>().Length, 3, $"{monster.Name} ({monster.Shape} {monster.Variant})");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(holder.gameObject);
                }
            }

            // The Phase 8 bodies are in use, and names pick them: Ragnarok's Poring is a slime, the Peco Peco a tall bird.
            foreach (Monsters.MonsterShape shape in new[]
                     {
                         Monsters.MonsterShape.Insect, Monsters.MonsterShape.Plant, Monsters.MonsterShape.Slime, Monsters.MonsterShape.Bird,
                         Monsters.MonsterShape.Fish,
                     })
            {
                Assert.IsTrue(Monsters.MonsterCatalog.All.Any(m => m.Shape == shape), shape.ToString());
            }

            Assert.AreEqual(Monsters.MonsterShape.Slime, Monsters.MonsterCatalog.All.First(m => m.RoId == 1002).Shape);
            var peco = Monsters.MonsterCatalog.All.First(m => m.RoId == 1019);
            Assert.AreEqual(Monsters.MonsterShape.Bird, peco.Shape);
            Assert.AreEqual("tall", peco.Variant);
            Assert.AreEqual("spider", Monsters.MonsterCatalog.All.First(m => m.RoId == 1099).Variant);
        }
    }
}
