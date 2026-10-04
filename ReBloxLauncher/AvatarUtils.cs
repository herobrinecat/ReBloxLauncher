using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace ReBloxLauncher
{
    public class AvatarUtils
    {
        static byte currentVersion = 0x01;
        public class AssetData
        {
            public ulong id { get; set; } = 0;
        }

        public class BodyColors
        {
            public uint headColor { get; set; } = 194;
            public uint leftArmColor { get; set; } = 194;
            public uint leftLegColor { get; set; } = 194;
            public uint rightArmColor { get; set; } = 194;
            public uint rightLegColor { get; set; } = 194;
            public uint torsoColor { get; set; } = 194;
        }

        public class AvatarType
        {
            public string bodyType { get; set; } = "R6";
            public IList<AssetData> asset { get; set; }
            public BodyColors colors { get; set; }
            public string[] inventory { get; set; }
            public string base64FullBody { get; set; }
            public string base64HeadShot { get; set; }
        }

        public class SaveAvatarType
        {
            public string name { get; set; } = "Untitled Character 1";
            public string bodyType { get; set; } = "R6";
            public IList<AssetData> asset { get; set; }
            public BodyColors colors { get; set; }
            public string base64FullBody { get; set; } = "";
            public string base64HeadShot { get; set; } = "";
        }

        public static void writeCharacterFile(string path, SaveAvatarType avatar, bool includeImages = false)
        {
            using (FileStream stream = File.Open(path, FileMode.Create, FileAccess.ReadWrite))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write("RBCF");
                    writer.Write(293712399); // magic number
                    writer.Write(currentVersion); //Version
                    writer.Write(includeImages && (avatar.base64HeadShot != "" || avatar.base64FullBody != "")); //The additional check is for when you decided to try and include images that doesn't even exist
                    writer.Write(Uri.EscapeDataString(avatar.name));
                    //Body Colors (expects 6)
                    writer.Write(avatar.colors.headColor);
                    writer.Write(avatar.colors.torsoColor);
                    writer.Write(avatar.colors.leftArmColor);
                    writer.Write(avatar.colors.leftLegColor);
                    writer.Write(avatar.colors.rightArmColor);
                    writer.Write(avatar.colors.rightLegColor);
                    //Body type (bruh)
                    writer.Write((byte)(avatar.bodyType == "R6" ? 0 : 1));
                    //Assets
                    int assetcount = avatar.asset.Count;
                    writer.Write(assetcount);
                    for (int i = 0; i < assetcount; i++)
                    {
                        writer.Write(avatar.asset[i].id);
                    }
                    //Images (if you have any)
                    if (includeImages && (avatar.base64HeadShot != "" || avatar.base64FullBody != ""))
                    {
                        if (avatar.base64FullBody != "" && avatar.base64HeadShot != "")
                        {
                            writer.Write((byte)2);
                        }
                        else
                        {
                            writer.Write((byte)1);
                        }

                        if (avatar.base64HeadShot != "")
                        {
                            byte[] compressed = ServerUtils.GzipCompress(Convert.FromBase64String(avatar.base64FullBody));
                            writer.Write(compressed.Length);
                            writer.Write(compressed);
                        }
                        if (avatar.base64FullBody != "")
                        {
                            byte[] compressed = ServerUtils.GzipCompress(Convert.FromBase64String(avatar.base64HeadShot));
                            writer.Write(compressed.Length);
                            writer.Write(compressed);
                        }
                    }
                    writer.Write(293712399); //Write the magic number one more time to end it all, don't confuse this format with RBDF (ReBlox Datastore File) in a future release
                }
            }
        }

        public static SaveAvatarType readCharacterFile(string path)
        {
            if (File.Exists(path))
            {
                using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        if (reader.ReadString() == "RBCF")
                        {
                            if (reader.ReadInt32() == 293712399)
                            {
                                //any previous version may stay here
                                byte version = reader.ReadByte();
                                if (version == currentVersion)
                                {
                                    string headshotBase64 = "";
                                    string fullbodyBase64 = "";

                                    bool imagesIncluded = reader.ReadBoolean();
                                    string name = Uri.UnescapeDataString(reader.ReadString());
                                    uint headColor = reader.ReadUInt32();
                                    uint torsoColor = reader.ReadUInt32();
                                    uint leftArmColor = reader.ReadUInt32();
                                    uint leftLegColor = reader.ReadUInt32();
                                    uint rightArmColor = reader.ReadUInt32();
                                    uint rightLegColor = reader.ReadUInt32();

                                    byte rigType = reader.ReadByte();
                                    int assetCount = reader.ReadInt32();
                                    List<AssetData> assets = new List<AssetData>();
                                    for (int i = 0; i < assetCount; i++)
                                    {
                                        assets.Add(new AssetData { id = reader.ReadUInt64() });
                                    }

                                    if (imagesIncluded)
                                    {
                                        byte imagecount = reader.ReadByte();
                                        for (int i = 0; i < imagecount; i++)
                                        {
                                            int imagelength = reader.ReadInt32();
                                            byte[] compressedimage = reader.ReadBytes(imagelength);
                                            string uncompressedBase64 = Convert.ToBase64String(ServerUtils.GzipDecompress(compressedimage));

                                            if (imagecount == 0)
                                            {
                                                fullbodyBase64 = uncompressedBase64;
                                            }
                                            else if (imagecount == 1)
                                            {
                                                headshotBase64 = uncompressedBase64;
                                            }
                                            else {
                                                Console.WriteLine("<WARN> Expecting 1-2 image(s), got " + imagecount + " images, skipping...");
                                            }
                                        }
                                    }
                                    if (reader.ReadInt32() == 293712399)
                                    {
                                        //signature verified! (hopefully)

                                        return new SaveAvatarType { name = name, asset = assets.ToArray(), base64FullBody = fullbodyBase64, base64HeadShot = headshotBase64, bodyType = rigType == 0 ? "R6" : "R15", colors = new BodyColors { headColor = headColor, leftArmColor = leftArmColor, leftLegColor = leftLegColor, rightLegColor = rightLegColor, rightArmColor = rightArmColor, torsoColor = torsoColor } };
                                    }
                                    else
                                    {
                                        throw new InvalidDataException("This character file may be corrupted, please try exporting the character again!");
                                    }
                                }
                                else if (version > currentVersion)
                                {
                                    throw new PlatformNotSupportedException("This file is a newer format of ReBlox Character File, please try again with the latest version of ReBlox!");
                                }
                            }
                            else
                            {
                                throw new InvalidDataException("This character file may be corrupted, please try exporting the character again!");
                            }
                        }
                        else
                        {
                            throw new InvalidDataException("This character file may be corrupted, please try exporting the character again!");
                        }
                    }
                }
            }
            else
            {
                throw new FileNotFoundException("The character file you're trying to access doesn't exist.");
            }
            return default(SaveAvatarType); //It fixes this for some reason
        }
        
    }
}