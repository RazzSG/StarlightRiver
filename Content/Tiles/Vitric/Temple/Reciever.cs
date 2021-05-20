﻿using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

using StarlightRiver.Core;

namespace StarlightRiver.Content.Tiles.Vitric.Temple
{
    class RecieverPuzzle : ModTile
    {
        public override bool Autoload(ref string name, ref string texture)
        {
            texture = AssetDirectory.VitricTile + name;
            return base.Autoload(ref name, ref texture);
        }

        public override void SetDefaults() => (this).QuickSetFurniture(2, 3, DustType<Dusts.Air>(), SoundID.Tink, false, new Color(0, 255, 255), false, true, "Reciever");
    }

    class RecieverPlacable : ModTile
    {
        public override bool Autoload(ref string name, ref string texture)
        {
            texture = AssetDirectory.VitricTile + name;
            return base.Autoload(ref name, ref texture);
        }

        public override void SetDefaults() => (this).QuickSetFurniture(1, 1, DustType<Dusts.Air>(), SoundID.Tink, false, new Color(0, 255, 255), false, true, "Reciever");
    }

    class RecieverItem : QuickTileItem
    {
        public RecieverItem() : base("Light Reciever", "", TileType<RecieverPlacable>(), 0, AssetDirectory.VitricTile) { }
    }

    class RecieverItem2 : QuickTileItem
    {
        public RecieverItem2() : base("Debug Puzzle Reciever", "", TileType<RecieverPuzzle>(), 0, AssetDirectory.VitricTile) { }
    }
}
