using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A plain stand-in model for items that have no bespoke one yet — a single tinted primitive
    /// picked by category, the way ungmodelled items have always been drawn on the floor.
    ///
    /// This lives here as a description rather than as loose GameObject code inside
    /// <see cref="WorldItem"/> so that modelled and unmodelled items travel the same path: both bake
    /// to a mesh and both draw instanced. Otherwise every unmodelled item would need a scene object
    /// again and the item count would be capped by whichever kind of item happened to be commonest.
    /// </summary>
    public static class FallbackItemModel
    {
        private static readonly Dictionary<string, ItemModelDesc> s_Cache = new Dictionary<string, ItemModelDesc>(128);

        /// <summary>The stand-in model for <paramref name="def"/>. Never null for a real definition.</summary>
        public static ItemModelDesc Describe(ItemDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.Id)) return null;
            if (s_Cache.TryGetValue(def.Id, out ItemModelDesc cached)) return cached;

            Shape(def, out PartShape shape, out Vector3 scale, out float height);

            var root = new PartNode("Visual");
            root.Add(new Part(shape, Vector3.zero, Quaternion.identity, scale, PartMat.Of(def.Tint, smoothness: 0.15f)));

            ItemModelDesc desc = ItemModelDesc.FromRoot(root, height);
            s_Cache[def.Id] = desc;
            return desc;
        }

        /// <summary>Shape, size and hover height for an item with no model of its own, chosen by
        /// category so a weapon still reads differently from a tin on the floor.</summary>
        private static void Shape(ItemDef def, out PartShape shape, out Vector3 scale, out float height)
        {
            switch (def.Category)
            {
                case ItemCategory.Food:
                    shape = PartShape.Cube;
                    scale = new Vector3(0.20f, 0.16f, 0.20f);
                    height = 0.10f;
                    return;

                case ItemCategory.Drink:
                    shape = PartShape.Cylinder;
                    scale = new Vector3(0.15f, 0.11f, 0.15f);   // cylinder mesh is two units tall
                    height = 0.12f;
                    return;

                case ItemCategory.Medical:
                    shape = PartShape.Cube;
                    scale = new Vector3(0.18f, 0.10f, 0.13f);
                    height = 0.07f;
                    return;

                case ItemCategory.Weapon:
                    shape = PartShape.Cube;
                    scale = new Vector3(0.60f, 0.07f, 0.10f);   // long and flat, like something dropped
                    height = 0.06f;
                    return;

                case ItemCategory.Firearm:
                    shape = PartShape.Cube;
                    scale = new Vector3(0.75f, 0.09f, 0.14f);
                    height = 0.07f;
                    return;

                case ItemCategory.Ammo:
                    shape = PartShape.Cube;
                    scale = new Vector3(0.14f, 0.09f, 0.10f);
                    height = 0.06f;
                    return;

                case ItemCategory.Material:
                    shape = PartShape.Cube;
                    scale = new Vector3(0.26f, 0.14f, 0.20f);
                    height = 0.09f;
                    return;

                case ItemCategory.Clothing:
                    shape = PartShape.Sphere;
                    scale = new Vector3(0.34f, 0.10f, 0.30f);   // a flat bundle of cloth
                    height = 0.07f;
                    return;

                default:
                    shape = PartShape.Sphere;
                    scale = new Vector3(0.18f, 0.18f, 0.18f);
                    height = 0.11f;
                    return;
            }
        }
    }
}
