using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.Emit;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReBloxLauncher
{
    public partial class LaunchScreen : Form
    {
        string robloxversion = "";
        string datafolder = Path.GetDirectoryName(Application.ExecutablePath) + @"\data";
        string hostargument = "";
        string joinargument = "";
        bool UseJoinJSONLink = false;
        bool useIPForwarder = false;
        bool ReserveAssetIdForMap = false;
        int placeid = 1;
        private object syncLock = new object();
        bool shutdown = false;
        bool useNewRoblox = false;
        public LaunchScreen(string version, string customDataFolder = null)
        {
            InitializeComponent();
            robloxversion = version;
            if (customDataFolder != null)
            {
                datafolder = customDataFolder;
            }
        }

        private Color convertBrickColortoColor(int brickcolor)
        {
            switch (brickcolor)
            {
                case 1:
                    return Color.FromArgb(242, 243, 243);
                case 2:
                    return Color.FromArgb(161, 165, 162);
                case 3:
                    return Color.FromArgb(249, 233, 153);
                case 5:
                    return Color.FromArgb(215, 197, 154);
                case 6:
                    return Color.FromArgb(194, 218, 184);
                case 9:
                    return Color.FromArgb(232, 186, 200);
                case 11:
                    return Color.FromArgb(128, 187, 219);
                case 12:
                    return Color.FromArgb(203, 132, 66);
                case 18:
                    return Color.FromArgb(204, 142, 105);
                case 21:
                    return Color.FromArgb(196, 40, 28);
                case 22:
                    return Color.FromArgb(196, 112, 160);
                case 23:
                    return Color.FromArgb(13, 105, 172);
                case 24:
                    return Color.FromArgb(245, 205, 48);
                case 25:
                    return Color.FromArgb(98, 71, 50);
                case 26:
                    return Color.FromArgb(27, 42, 53);
                case 27:
                    return Color.FromArgb(109, 110, 108);
                case 28:
                    return Color.FromArgb(40, 127, 71);
                case 29:
                    return Color.FromArgb(161, 196, 140);
                case 36:
                    return Color.FromArgb(243, 207, 155);
                case 37:
                    return Color.FromArgb(75, 151, 75);
                case 38:
                    return Color.FromArgb(160, 95, 53);
                case 39:
                    return Color.FromArgb(193, 202, 222);
                case 40:
                    return Color.FromArgb(236, 236, 236);
                case 41:
                    return Color.FromArgb(205, 84, 75);
                case 42:
                    return Color.FromArgb(193, 223, 240);
                case 43:
                    return Color.FromArgb(123, 182, 232);
                case 44:
                    return Color.FromArgb(247, 241, 141);
                case 45:
                    return Color.FromArgb(180, 210, 228);
                case 47:
                    return Color.FromArgb(217, 133, 108);
                case 48:
                    return Color.FromArgb(132, 182, 141);
                case 49:
                    return Color.FromArgb(248, 241, 132);
                case 50:
                    return Color.FromArgb(236, 232, 222);
                case 100:
                    return Color.FromArgb(238, 196, 182);
                case 101:
                    return Color.FromArgb(218, 134, 122);
                case 102:
                    return Color.FromArgb(110, 153, 202);
                case 103:
                    return Color.FromArgb(199, 193, 183);
                case 104:
                    return Color.FromArgb(107, 50, 124);
                case 105:
                    return Color.FromArgb(226, 155, 64);
                case 106:
                    return Color.FromArgb(218, 133, 65);
                case 107:
                    return Color.FromArgb(0, 143, 156);
                case 108:
                    return Color.FromArgb(104, 92, 67);
                case 110:
                    return Color.FromArgb(67, 84, 147);
                case 111:
                    return Color.FromArgb(191, 183, 177);
                case 112:
                    return Color.FromArgb(104, 116, 172);
                case 113:
                    return Color.FromArgb(229, 173, 200);
                case 115:
                    return Color.FromArgb(199, 210, 60);
                case 116:
                    return Color.FromArgb(85, 165, 175);
                case 118:
                    return Color.FromArgb(183, 215, 213);
                case 119:
                    return Color.FromArgb(164, 179, 71);
                case 120:
                    return Color.FromArgb(217, 228, 167);
                case 121:
                    return Color.FromArgb(231, 172, 88);
                case 123:
                    return Color.FromArgb(211, 111, 76);
                case 124:
                    return Color.FromArgb(146, 57, 120);
                case 125:
                    return Color.FromArgb(234, 184, 146);
                case 126:
                    return Color.FromArgb(165, 165, 203);
                case 127:
                    return Color.FromArgb(220, 188, 129);
                case 128:
                    return Color.FromArgb(174, 122, 89);
                case 131:
                    return Color.FromArgb(156, 163, 168);
                case 133:
                    return Color.FromArgb(213, 115, 61);
                case 134:
                    return Color.FromArgb(216, 221, 86);
                case 135:
                    return Color.FromArgb(116, 134, 157);
                case 136:
                    return Color.FromArgb(135, 124, 144);
                case 137:
                    return Color.FromArgb(224, 152, 100);
                case 138:
                    return Color.FromArgb(149, 138, 115);
                case 140:
                    return Color.FromArgb(32, 58, 86);
                case 141:
                    return Color.FromArgb(39, 70, 45);
                case 143:
                    return Color.FromArgb(207, 226, 247);
                case 145:
                    return Color.FromArgb(121, 136, 161);
                case 146:
                    return Color.FromArgb(149, 142, 163);
                case 147:
                    return Color.FromArgb(147, 135, 104);
                case 148:
                    return Color.FromArgb(87, 88, 87);
                case 149:
                    return Color.FromArgb(22, 29, 50);
                case 150:
                    return Color.FromArgb(171, 173, 172);
                case 151:
                    return Color.FromArgb(120, 144, 130);
                case 153:
                    return Color.FromArgb(149, 121, 119);
                case 154:
                    return Color.FromArgb(123, 46, 47);
                case 157:
                    return Color.FromArgb(255, 246, 123);
                case 158:
                    return Color.FromArgb(225, 164, 194);
                case 168:
                    return Color.FromArgb(117, 108, 98);
                case 176:
                    return Color.FromArgb(151, 105, 91);
                case 178:
                    return Color.FromArgb(180, 132, 85);
                case 179:
                    return Color.FromArgb(137, 135, 136);
                case 180:
                    return Color.FromArgb(215, 169, 75);
                case 190:
                    return Color.FromArgb(249, 214, 46);
                case 191:
                    return Color.FromArgb(232, 171, 45);
                case 192:
                    return Color.FromArgb(105, 64, 40);
                case 193:
                    return Color.FromArgb(207, 96, 36);
                case 194:
                    return Color.FromArgb(163, 162, 165);
                case 195:
                    return Color.FromArgb(70, 103, 164);
                case 196:
                    return Color.FromArgb(35, 71, 139);
                case 198:
                    return Color.FromArgb(142, 66, 133);
                case 199:
                    return Color.FromArgb(99, 95, 98);
                case 200:
                    return Color.FromArgb(130, 138, 93);
                case 208:
                    return Color.FromArgb(229, 228, 223);
                case 209:
                    return Color.FromArgb(176, 142, 68);
                case 210:
                    return Color.FromArgb(112, 149, 120);
                case 211:
                    return Color.FromArgb(121, 181, 181);
                case 212:
                    return Color.FromArgb(159, 195, 233);
                case 213:
                    return Color.FromArgb(108, 129, 183);
                case 216:
                    return Color.FromArgb(144, 76, 42);
                case 217:
                    return Color.FromArgb(124, 92, 70);
                case 218:
                    return Color.FromArgb(150, 112, 159);
                case 219:
                    return Color.FromArgb(107, 96, 155);
                case 220:
                    return Color.FromArgb(167, 169, 206);
                case 221:
                    return Color.FromArgb(205, 98, 152);
                case 222:
                    return Color.FromArgb(228, 173, 200);
                case 223:
                    return Color.FromArgb(220, 144, 149);
                case 224:
                    return Color.FromArgb(240, 213, 160);
                case 225:
                    return Color.FromArgb(235, 184, 127);
                case 226:
                    return Color.FromArgb(253, 234, 141);
                case 232:
                    return Color.FromArgb(125, 187, 221);
                case 268:
                    return Color.FromArgb(52, 43, 117);
                case 301:
                    return Color.FromArgb(80, 109, 84);
                case 302:
                    return Color.FromArgb(91, 93, 105);
                case 303:
                    return Color.FromArgb(0, 16, 176);
                case 304:
                    return Color.FromArgb(44, 101, 29);
                case 305:
                    return Color.FromArgb(82, 124, 174);
                case 306:
                    return Color.FromArgb(51, 88, 130);
                case 307:
                    return Color.FromArgb(16, 42, 220);
                case 308:
                    return Color.FromArgb(61, 21, 133);
                case 309:
                    return Color.FromArgb(52, 142, 64);
                case 310:
                    return Color.FromArgb(91, 154, 76);
                case 311:
                    return Color.FromArgb(159, 161, 172);
                case 312:
                    return Color.FromArgb(89, 34, 89);
                case 313:
                    return Color.FromArgb(31, 128, 29);
                case 314:
                    return Color.FromArgb(159, 173, 192);
                case 315:
                    return Color.FromArgb(9, 137, 207);
                case 316:
                    return Color.FromArgb(123, 0, 123);
                case 317:
                    return Color.FromArgb(124, 156, 107);
                case 318:
                    return Color.FromArgb(138, 171, 133);
                case 319:
                    return Color.FromArgb(185, 196, 177);
                case 320:
                    return Color.FromArgb(202, 203, 209);
                case 321:
                    return Color.FromArgb(167, 94, 155);
                case 322:
                    return Color.FromArgb(123, 47, 123);
                case 323:
                    return Color.FromArgb(148, 190, 129);
                case 324:
                    return Color.FromArgb(168, 189, 153);
                case 325:
                    return Color.FromArgb(223, 223, 222);
                case 327:
                    return Color.FromArgb(151, 0, 0);
                case 328:
                    return Color.FromArgb(177, 229, 166);
                case 329:
                    return Color.FromArgb(152, 194, 219);
                case 330:
                    return Color.FromArgb(255, 152, 220);
                case 331:
                    return Color.FromArgb(255, 89, 89);
                case 332:
                    return Color.FromArgb(117, 0, 0);
                case 333:
                    return Color.FromArgb(239, 184, 56);
                case 334:
                    return Color.FromArgb(248, 217, 190);
                case 335:
                    return Color.FromArgb(231, 231, 236);
                case 336:
                    return Color.FromArgb(199, 212, 228);
                case 337:
                    return Color.FromArgb(255, 148, 148);
                case 338:
                    return Color.FromArgb(190, 104, 98);
                case 339:
                    return Color.FromArgb(86, 36, 36);
                case 340:
                    return Color.FromArgb(241, 231, 199);
                case 341:
                    return Color.FromArgb(254, 243, 187);
                case 342:
                    return Color.FromArgb(224, 178, 208);
                case 343:
                    return Color.FromArgb(212, 144, 189);
                case 344:
                    return Color.FromArgb(150, 85, 85);
                case 345:
                    return Color.FromArgb(143, 76, 42);
                case 346:
                    return Color.FromArgb(211, 190, 150);
                case 347:
                    return Color.FromArgb(226, 220, 188);
                case 348:
                    return Color.FromArgb(237, 234, 234);
                case 349:
                    return Color.FromArgb(233, 218, 218);
                case 350:
                    return Color.FromArgb(136, 62, 62);
                case 351:
                    return Color.FromArgb(188, 155, 93);
                case 352:
                    return Color.FromArgb(199, 172, 120);
                case 353:
                    return Color.FromArgb(202, 191, 163);
                case 354:
                    return Color.FromArgb(187, 179, 178);
                case 355:
                    return Color.FromArgb(108, 88, 75);
                case 356:
                    return Color.FromArgb(160, 132, 79);
                case 357:
                    return Color.FromArgb(149, 137, 136);
                case 358:
                    return Color.FromArgb(171, 168, 158);
                case 359:
                    return Color.FromArgb(175, 148, 131);
                case 360:
                    return Color.FromArgb(150, 103, 102);
                case 361:
                    return Color.FromArgb(86, 66, 54);
                case 362:
                    return Color.FromArgb(126, 103, 63);
                case 363:
                    return Color.FromArgb(105, 102, 92);
                case 364:
                    return Color.FromArgb(90, 76, 66);
                case 365:
                    return Color.FromArgb(106, 57, 9);
                case 1001:
                    return Color.FromArgb(248, 248, 248);
                case 1002:
                    return Color.FromArgb(205, 205, 205);
                case 1003:
                    return Color.FromArgb(17, 17, 17);
                case 1004:
                    return Color.FromArgb(255, 0, 0);
                case 1005:
                    return Color.FromArgb(255, 176, 0);
                case 1006:
                    return Color.FromArgb(180, 128, 255);
                case 1007:
                    return Color.FromArgb(163, 75, 75);
                case 1008:
                    return Color.FromArgb(193, 190, 66);
                case 1009:
                    return Color.FromArgb(255, 255, 0);
                case 1010:
                    return Color.FromArgb(0, 0, 255);
                case 1011:
                    return Color.FromArgb(0, 32, 96);
                case 1012:
                    return Color.FromArgb(33, 84, 185);
                case 1013:
                    return Color.FromArgb(4, 175, 236);
                case 1014:
                    return Color.FromArgb(170, 85, 0);
                case 1015:
                    return Color.FromArgb(170, 0, 170);
                case 1016:
                    return Color.FromArgb(255, 102, 204);
                case 1017:
                    return Color.FromArgb(255, 175, 0);
                case 1018:
                    return Color.FromArgb(18, 238, 212);
                case 1019:
                    return Color.FromArgb(0, 255, 255);
                case 1020:
                    return Color.FromArgb(0, 255, 0);
                case 1021:
                    return Color.FromArgb(50, 125, 21);
                case 1022:
                    return Color.FromArgb(127, 142, 100);
                case 1023:
                    return Color.FromArgb(140, 91, 159);
                case 1024:
                    return Color.FromArgb(175, 221, 255);
                case 1025:
                    return Color.FromArgb(255, 201, 201);
                case 1026:
                    return Color.FromArgb(177, 167, 255);
                case 1027:
                    return Color.FromArgb(159, 243, 233);
                case 1028:
                    return Color.FromArgb(204, 255, 204);
                case 1029:
                    return Color.FromArgb(255, 255, 204);
                case 1030:
                    return Color.FromArgb(255, 204, 153);
                case 1031:
                    return Color.FromArgb(98, 37, 209);
                case 1032:
                    return Color.FromArgb(255, 0, 191);
                default:
                    return Color.Empty;
            }
        }
        private string GenerateUUID()
        {
            lock (syncLock)
            {
                return Guid.NewGuid().ToString();
            }
        }
        private void SetupJoinScript(string ipaddr, int port)
        {
            Console.WriteLine("<INFO> Setting up join script for " + Properties.Settings.Default.lastselectedversion);
            label1.Invoke(new Action(() => { label1.Text = "Setting up join script..."; }));
            string waitingForCharacterGuid = GenerateUUID().ToLower();
            string sessionId = GenerateUUID().ToLower();
            if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\joinscript.txt")) File.Delete(datafolder + @"\tools\RobloxAssetFixer\joinscript.txt");
            File.WriteAllText(datafolder + @"\tools\RobloxAssetFixer\joinscript.txt", @"--rbxsig%PwqWEjzB5MasktbXBzHRjH8kwI5ltVM/lIAaQnwpristI8AQsHzKQGJGBrne9l2OFJLiao7SFrJ86rwehnjlHbBC63KBr4ihUTE2EHFHaWde95xF1Jb37jJ3wg1uJRDEXq9lZWCcYsz/2HOIYRMddAytnxX4ZLBig6mfLfcrOWA=%
{""ClientPort"":0,""MachineAddress"":""" + ipaddr + @""",""ServerPort"":" + port.ToString() + @",""PingUrl"":"""",""PingInterval"":120,""UserName"":""" + Properties.Settings.Default.username + @""",""SeleniumTestMode"":false,""UserId"":" + Properties.Settings.Default.UserId + @",""SuperSafeChat"":false,""CharacterAppearance"":""http://assetgame.reblox.zip/Asset/CharacterFetch.ashx?userId=" + Properties.Settings.Default.UserId + @"&placeId=1"",""ClientTicket"":""" + DateTime.UtcNow.ToString("G") + @";h0eeFX/hZrNHXjP01PeaXT8dA8yVZbGKSMR6omd818fXJwuc/RceXUA8EJwdlfn7IWDfqjF2e22EhFyPXhucHqxQjY3GQd+zPAfS7KfQzItRVIFnjXbfWEGPKKFFEP4QcTs9Q141sd3G83ye9ZdGbOXPjy9VwpdvEnFToarYX7Q=;TCtJG0d2d0pFaHYnHDzJQttKfZlZyHZmcRtUNcy9vyivgiwQtB/illTbHvaUc/9w+oy8XRi+giLEvwuRmRttGKKnpA5Qt7dwCyXz2UIzt5/8TSJYqIKT99iPjBg0/PQFmguI7LoSk1KfElEDwzCWGT3tryAiT7S7a1SjInteSAU="",""GameId"":""00000000-0000-0000-0000-000000000000"",""PlaceId"":" + (ReserveAssetIdForMap ? placeid : 1) + @",""MeasurementUrl"":"""",""WaitingForCharacterGuid"":""" + waitingForCharacterGuid + @""",""BaseUrl"":""http://www.reblox.zip/"",""ChatStyle"":""" + Properties.Settings.Default.ChatStyle + @""",""VendorId"":0,""ScreenShotInfo"":"""",""VideoInfo"":""<?xml version=\""1.0\""?><entry xmlns=\""http://www.w3.org/2005/Atom\"" xmlns:media=\""http://search.yahoo.com/mrss/\"" xmlns:yt=\""http://gdata.youtube.com/schemas/2007\""><media:group><media:title type=\""plain\""><![CDATA[ROBLOX Place]]></media:title><media:description type=\""plain\""><![CDATA[ For more games visit http://www.roblox.com]]></media:description><media:category scheme=\""http://gdata.youtube.com/schemas/2007/categories.cat\"">Games</media:category><media:keywords>ROBLOX, video, free game, online virtual world</media:keywords></media:group></entry>"",""CreatorId"":1,""CreatorTypeEnum"":""User"",""MembershipType"":""None"",""AccountAge"":365,""CookieStoreFirstTimePlayKey"":""rbx_evt_ftp"",""CookieStoreFiveMinutePlayKey"":""rbx_evt_fmp"",""CookieStoreEnabled"":true,""IsRobloxPlace"":false,""GenerateTeleportJoin"":false,""IsUnknownOrUnder13"":" + (!Properties.Settings.Default.AccountOver13).ToString().ToLower() + @",""SessionId"":""" + sessionId + @"|00000000-0000-0000-0000-000000000000|0|204.236.226.210|8|" + DateTime.UtcNow.ToString("") + @"Z|0|null|null|null|null"",""DataCenterId"":0,""UniverseId"":2,""BrowserTrackerId"":0,""UsePortraitMode"":false,""FollowUserId"":0,""characterAppearanceId"":0}");
        }

        private void SetupGameFiles()
        {
            label1.Invoke(new Action(() => { label1.Text = "Setting up game files..."; }));
            Console.WriteLine("<INFO> Checking for FFlags for " + Properties.Settings.Default.lastselectedversion);
            if (Directory.Exists(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings"))
            {
                Console.WriteLine("<INFO> Copying the ClientAppSettings.json to the RobloxAssetFixer");
                if (File.Exists(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json"))
                {
                    File.Copy(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json", datafolder + @"\tools\RobloxAssetFixer\ClientAppSettings.json", true);
                }
            }
            if (UseJoinJSONLink == true)
            {
                Console.WriteLine("<INFO> Checking for the game folder for " + Properties.Settings.Default.lastselectedversion);
                if (Directory.Exists(datafolder + @"\tools\RobloxAssetFixer\game") == false && Directory.Exists(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\game"))
                {
                    Console.WriteLine("<INFO> Creating the game folder and copying its content to the folder for " + Properties.Settings.Default.lastselectedversion);
                    Directory.CreateDirectory(datafolder + @"\tools\RobloxAssetFixer\game");
                    foreach (string file in Directory.GetFiles(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\game"))
                    {
                        if (shutdown) return;
                        if (Path.GetFileName(file) == "join.ashx")
                        {
                            if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file))) File.Delete(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file));
                            File.WriteAllText(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file), File.ReadAllText(file).Replace("{ip}", "127.0.0.1").Replace("{port}", "53640").Replace("{username}", Properties.Settings.Default.username).Replace("{id}", Properties.Settings.Default.UserId.ToString()).Replace("{13}", (!Properties.Settings.Default.AccountOver13).ToString().ToLower()));
                        }
                        else if (Path.GetFileName(file) == "gameserver.ashx")
                        {
                            if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file))) File.Delete(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file));
                            File.WriteAllText(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file), File.ReadAllText(file).Replace("{ip}", "127.0.0.1").Replace("{port}", "53640").Replace("{username}", Properties.Settings.Default.username).Replace("{id}", Properties.Settings.Default.UserId.ToString()).Replace("{13}", (!Properties.Settings.Default.AccountOver13).ToString().ToLower()));
                        }
                        else if (Path.GetFileName(file) == "placespecificscript.ashx")
                        {
                            if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file))) File.Delete(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file));
                            File.WriteAllText(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file), File.ReadAllText(file).Replace("{id}", placeid.ToString()));
                        }
                        else
                        {
                            File.Copy(file, datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file));
                        }
                    }
                }
            }
        }

        private void RemoveGameFiles()
        {
            Console.WriteLine("<INFO> Cleaning up game files and removing the ClientAppSettings file");
            if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\ClientAppSettings.json")) File.Delete(datafolder + @"\tools\RobloxAssetFixer\ClientAppSettings.json");

            if (Directory.Exists(datafolder + @"\tools\RobloxAssetFixer\clothes") == true) Directory.Delete(datafolder + @"\tools\RobloxAssetFixer\clothes", true);

            if (UseJoinJSONLink == true)
            {
                if (Directory.Exists(datafolder + @"\tools\RobloxAssetFixer\game") == true) Directory.Delete(datafolder + @"\tools\RobloxAssetFixer\game", true);
                if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\joinscript.txt")) File.Delete(datafolder + @"\tools\RobloxAssetFixer\joinscript.txt");
            }
        }

        private void ClearAssets()
        {
            string[] files = Directory.GetFiles(datafolder + @"\tools\RobloxAssetFixer\assets");
            foreach (string file in files)
            {
                if (file.EndsWith("avatar.png") == false && file.EndsWith("gameicon.png") == false && file.EndsWith("headshot.png") == false)
                {
                    File.Delete(file);
                }
            }
        }

        private void Roblox_Exited(object sender, EventArgs e)
        {
            Process[] processesr = Process.GetProcessesByName("RobloxStudioBeta");
            if (processesr.Length <= 0)
            {
                Process[] processes = Process.GetProcessesByName("node");
                if (processes.Length > 0)
                {
                    foreach (Process process in processes)
                    {
                        if (process.MainModule.FileName == datafolder + @"\tools\node\node.exe")
                        {
                            Console.WriteLine("<INFO> Shutting down the node server");
                            process.Kill();
                        }
                    }
                }
                processes = Process.GetProcessesByName("IPForwarder");
                if (processes.Length > 0)
                {
                    foreach (Process process in processes)
                    {
                        Console.WriteLine("<INFO> Shutting down IPForwarder");
                        process.Kill();
                    }
                }

                if (Properties.Settings.Default.ClearTemp)
                {
                    Console.WriteLine("<INFO> Clearing ROBLOX's temporary files");
                    if (Directory.Exists(Path.GetTempPath() + "Roblox")) Directory.Delete(Path.GetTempPath() + "Roblox", true);
                }
                RemoveGameFiles();
                ClearAssets();
                Application.Exit();
            }
        }

        private int CountAssets() 
        {
            label1.Invoke(new Action(() => { label1.Text = "Loading assets..."; }));
            string[] splited = Properties.Settings.Default.AssetPackEnabled.Split('|');
            int count = 0;
            foreach (string s in splited)
            {
                if (Directory.Exists(s))
                {
                    bool compatible = true;
                    string[] config = File.ReadAllLines(s + @"\ReBlox.ini");
                    for (int i = 0; i < config.Length; i++)
                    {
                        if (config[i].Trim().StartsWith("Clients="))
                        {
                            string[] splited1 = config[i].Trim().Split(new char[] { '=' }, 2);
                            if (splited1[1] == "*")
                            {
                                compatible = true;
                                break;
                            }
                            else if (splited1[1].Contains(","))
                            {
                                bool valuefound = false;
                                string[] splitedtwo = splited1[1].Split(',');
                                foreach (string version in splitedtwo)
                                {
                                    if (version == Properties.Settings.Default.lastselectedversion)
                                    {
                                        valuefound = true;
                                        compatible = true;
                                        break;
                                    }
                                }
                                if (valuefound == false) compatible = false; break;
                            }
                            else if (splited1[1] == Properties.Settings.Default.lastselectedversion)
                            {
                                compatible = true; break;
                            }
                            else
                            {
                                compatible = false; break;
                            }
                        }
                    }
                    if (compatible)
                    {
                        string[] files = Directory.GetFiles(s);
                        count = count + (files.Length - 1);
                    }

                }
            }
            return count;
        }
        private void LoadAssets()
        {
            progressBar1.Invoke(new Action(() => { progressBar1.Style = ProgressBarStyle.Continuous; }));
            progressBar1.Invoke(new Action(() => { progressBar1.Value = 0; }));
            progressBar1.Invoke(new Action(() => { progressBar1.Maximum = CountAssets(); }));
            label1.Invoke(new Action(() => { label1.Text = "Loading assets..."; }));
            string[] splited = Properties.Settings.Default.AssetPackEnabled.Split('|');
            foreach (string s in splited)
            {
                if (Directory.Exists(s))
                {
                    bool compatible = true;
                    string[] config = File.ReadAllLines(s + @"\ReBlox.ini");
                    for (int i = 0; i < config.Length; i++)
                    {
                        if (config[i].Trim().StartsWith("Clients="))
                        {
                            string[] splited1 = config[i].Trim().Split(new char[] { '=' }, 2);
                            if (splited1[1] == "*")
                            {
                                compatible = true;
                                break;
                            }
                            else if (splited1[1].Contains(","))
                            {
                                bool valuefound = false;
                                string[] splitedtwo = splited1[1].Split(',');
                                foreach (string version in splitedtwo)
                                {
                                    if (version == Properties.Settings.Default.lastselectedversion)
                                    {
                                        valuefound = true;
                                        compatible = true;
                                        break;
                                    }
                                }
                                if (valuefound == false) compatible = false; break;
                            }
                            else if (splited1[1] == Properties.Settings.Default.lastselectedversion)
                            {
                                compatible = true; break;
                            }
                            else
                            {
                                compatible = false; break;
                            }
                        }
                    }
                    if (compatible)
                    {
                        string[] files = Directory.GetFiles(s);
                        foreach (string file in files)
                        {
                            if (file.EndsWith(".ini") == false)
                            {
                                File.Copy(file, datafolder + @"\tools\RobloxAssetFixer\assets\" + Path.GetFileName(file), true);
                                progressBar1.Invoke(new Action(() => { progressBar1.Value++; }));
                                if (shutdown) return;
                            }
                        }
                    }

                }
                if (shutdown) return;
            }
            progressBar1.Invoke(new Action(() => { progressBar1.Style = ProgressBarStyle.Marquee; }));
            progressBar1.Invoke(new Action(() => { progressBar1.Value = 0; }));
            progressBar1.Invoke(new Action(() => { progressBar1.Maximum = 100; }));
        }

        public class AssetData
        {
            public int id { get; set; } = 0;
        }
        public class BodyColors
        {
            public int headColor { get; set; } = 194;
            public int leftArmColor { get; set; } = 194;
            public int leftLegColor { get; set; } = 194;
            public int rightArmColor { get; set; } = 194;
            public int rightLegColor { get; set; } = 194;
            public int torsoColor { get; set; } = 194;
        }
        public class AvatarType
        {
            public string bodyType { get; set; } = "R6";
            public IList<AssetData> asset { get; set; }
            public BodyColors colors { get; set; }
        }

        private bool IsNodeFromAppRunning()
        {
            bool isRunning = false;
            try
            {
                if (WineDetector.IsRunningOnWine() == false)
                {
                    Process[] processes = Process.GetProcessesByName("node");
                    foreach (Process process in processes)
                    {
                        if (process.MainModule.FileName == datafolder + @"\tools\node\node.exe")
                        {
                            isRunning = true; break;
                        }
                    }
                }
                else
                {
                    isRunning = true;
                }
            }
            catch
            {
                return false;
            }
            return isRunning;
        }
        private async void setupAvatarOnServer()
        {
            if (IsNodeFromAppRunning())
            {
                var jsondata = new AvatarType { };
                if (Properties.Settings.Default.ClothesArray.Length > 0)
                {
                    List<AssetData> data = new List<AssetData>();
                    foreach (var assetid in Properties.Settings.Default.ClothesArray.Split('|'))
                    {
                        data.Add(new AssetData { id = int.Parse(assetid) });
                    }
                    jsondata = new AvatarType
                    {
                        bodyType = (Properties.Settings.Default.avatarR15 ? "R15" : "R6"),
                        asset = data,
                        colors = new BodyColors { headColor = Properties.Settings.Default.HeadColor, leftArmColor = Properties.Settings.Default.LeftArmColor, leftLegColor = Properties.Settings.Default.LeftLegColor, rightArmColor = Properties.Settings.Default.RightArmColor, rightLegColor = Properties.Settings.Default.RightLegColor, torsoColor = Properties.Settings.Default.TorsoColor }
                    };
                }
                else
                {
                    jsondata = new AvatarType
                    {
                        bodyType = (Properties.Settings.Default.avatarR15 ? "R15" : "R6"),
                        asset = { },
                        colors = new BodyColors { headColor = Properties.Settings.Default.HeadColor, leftArmColor = Properties.Settings.Default.LeftArmColor, leftLegColor = Properties.Settings.Default.LeftLegColor, rightArmColor = Properties.Settings.Default.RightArmColor, rightLegColor = Properties.Settings.Default.RightLegColor, torsoColor = Properties.Settings.Default.TorsoColor }
                    };
                }

                try
                {
                    HttpClient client = new HttpClient();

                    client.BaseAddress = new Uri("http://reblox.zip");

                    string result = JsonConvert.SerializeObject(jsondata);
                    var buffer = Encoding.UTF8.GetBytes(result);
                    var byteContent = new ByteArrayContent(buffer);
                    byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    HttpResponseMessage response = await client.PostAsync("/v1/avatar/set-avatar?userId=" + Properties.Settings.Default.UserId, byteContent);

                    response.Dispose();

                    client.Dispose();

                    result = "";
                }
                catch
                {
                    // do nothing
                }
            }
        }
        public bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator);
        }
        private void LaunchScreen_Load(object sender, EventArgs e)
        {
            if (File.Exists(datafolder + @"\clients\" + robloxversion + @"\ReBlox.ini"))
            {
                string[] config = File.ReadAllLines(datafolder + @"\clients\" + robloxversion + @"\ReBlox.ini");
                if (config[0] == "[Reblox]")
                {
                    placeid = -1;
                    UseJoinJSONLink = false;
                    useIPForwarder = false;
                    ReserveAssetIdForMap = false;
                    useNewRoblox = false;
                    for (int i = 0; i < config.Length; i++)
                    {
                        if (config[i].Trim().StartsWith("JoinArgument=\""))
                        {
                            string[] splited = config[i].Trim().Split(new char[] { '=' }, 2);
                            joinargument = splited[1].Remove(0, 1).Remove(splited[1].Length - 2, 1);
                        }
                        else if (config[i].Trim().StartsWith("HostArgument=\""))
                        {
                            string[] splited = config[i].Trim().Split(new char[] { '=' }, 2);
                            hostargument = splited[1].Remove(0, 1).Remove(splited[1].Length - 2, 1);
                        }
                        else if (config[i].Trim() == "UseJoinScript=true")
                        {
                            UseJoinJSONLink = true;
                        }
                        else if (config[i].Trim() == "UseJoinScript=false")
                        {
                            UseJoinJSONLink = false;
                        }
                        else if (config[i].Trim() == "2018OverMessage=true")
                        {
                            if (Properties.Settings.Default.UsePatchInStudio == false)
                            {
                                MessageBox.Show("Note that RobloxAssetFixer is strongly recommended to be able to use this client property and login (which you just put your username in and your random letter in password.) You should turn on \"Use RobloxAssetFixer when opening in Studio mode\", located in Settings [If you're launching in Studio]", "You should turn this on...", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                        else if (config[i].Trim() == "UseIPForwarder=true")
                        {
                            useIPForwarder = true;
                        }
                        else if (config[i].Trim() == "UseIPForwarder=false")
                        {
                            useIPForwarder = false;
                        }
                        else if (config[i].Trim().StartsWith("PlaceId="))
                        {
                            if (int.TryParse(config[i].Trim().Replace("PlaceId=", ""), out _) == true)
                            {
                                placeid = int.Parse(config[i].Trim().Replace("PlaceId=", ""));
                            }
                        }
                        else if (config[i].Trim() == "UsePlaceAsAsset=true")
                        {
                            if (placeid > -1)
                            {
                                ReserveAssetIdForMap = true;
                            }
                            else
                            {
                                MessageBox.Show("PlaceId is required for UsePlaceAsAsset", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                Application.Exit();
                            }
                        }
                        else if (config[i].Trim() == "UsePlaceAsAsset=false")
                        {
                            ReserveAssetIdForMap = false;
                            placeid = -1;
                        }
                        else if (config[i].Trim() == "UseNewRobloxName=true")
                        {
                            useNewRoblox = true;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Config file failed to load!", "Config Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                }

            }
            this.Text = (useNewRoblox ? "Roblox Studio" : "ROBLOX Studio");
            Thread thread = new Thread(async () =>
            {
                try
                {
                    if (File.ReadAllText(@"C:\Windows\System32\drivers\etc\hosts").Contains("\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip") == false && Properties.Settings.Default.UsePatchInStudio == true && WineDetector.IsRunningOnWine() == false)
                    {
                        if (MessageBox.Show("Wanna edit the hosts file to make sure that decals loads? This is recommended for best experience!\n\nIf you wanna load assets, provide your .ROBLOSECURITY in the settings section and enable Use auth for loading assets. (This will not conflict with your latest Roblox Studio)", "hosts File Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            if (IsAdministrator())
                            {

                                File.AppendAllText(@"C:\Windows\System32\drivers\etc\hosts", "\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip");
                                if (Properties.Settings.Default.UsePatchInStudio)
                                {
                                    if (UseJoinJSONLink) SetupJoinScript("127.0.0.1", 53640);
                                    SetupGameFiles();
                                    LoadAssets();
                                }
                                if (shutdown == false)
                                {
                                    button1.Invoke(new Action(() => { button1.Visible = false; }));
                                    ProcessStartInfo ps = new ProcessStartInfo();
                                    ps.UseShellExecute = true;
                                    ps.FileName = datafolder + @"\clients\" + robloxversion + @"\Studio\RobloxStudioBeta.exe";
                                    ProcessStartInfo ps1 = new ProcessStartInfo();
                                    ps1.UseShellExecute = false;
                                    ps1.FileName = datafolder + @"\tools\node\node.exe";
                                    ps1.RedirectStandardOutput = true;
                                    ps1.CreateNoWindow = true;
                                    if (Properties.Settings.Default.useAuth)
                                    {
                                        if (Properties.Settings.Default.avatarR15)
                                        {
                                            if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer" : "");
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer" : "");
                                        }
                                        else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer" : "");
                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer" : "");
                                    }
                                    else
                                    {
                                        if (Properties.Settings.Default.avatarR15)
                                        {
                                            if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer" : "");
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer" : "");
                                        }
                                        else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer" : "");
                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer" : "");
                                    }
                                    ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";
                                    ps1.WindowStyle = ProcessWindowStyle.Hidden;
                                    if (Properties.Settings.Default.username.Length > 2 && Properties.Settings.Default.UserId >= 0)
                                    {
                                        if (IsNodeFromAppRunning() == false)
                                        {
                                            Process test = Process.Start(ps1);
                                            label1.Invoke(new Action(() => { label1.Text = "Waiting for node server to start..."; }));
                                            test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                            {
                                                if (e1.Data != null)
                                                {
                                                    if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                    {
                                                        setupAvatarOnServer();
                                                        label1.Invoke(new Action(() => { label1.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                        Process roblox = Process.Start(ps);
                                                        roblox.EnableRaisingEvents = true;
                                                        roblox.Exited += Roblox_Exited;
                                                        test.CancelOutputRead();
                                                        await Task.Delay(5000);
                                                        this.Invoke(new Action(() => { this.Hide(); }));
                                                    }
                                                }
                                            });
                                            test.BeginOutputReadLine();
                                        }
                                        else
                                        {
                                            setupAvatarOnServer();
                                            button1.Invoke(new Action(() => { button1.Visible = false; }));
                                            label1.Invoke(new Action(() => { label1.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                            Process roblox = Process.Start(ps);
                                            roblox.EnableRaisingEvents = true;
                                            roblox.Exited += Roblox_Exited;
                                            await Task.Delay(5000);
                                            this.Invoke(new Action(() => { this.Hide(); }));
                                        }

                                    }
                                    else
                                    {
                                        MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                            }
                            else
                            {
                                ProcessStartInfo ps = new ProcessStartInfo();
                                ps.UseShellExecute = true;
                                ps.FileName = Application.ExecutablePath;
                                ps.Verb = "runas";
                                Process.Start(ps);
                                Application.Exit();
                            }
                        }
                        else
                        {
                            button1.Invoke(new Action(() => { button1.Visible = false; }));
                            label1.Invoke(new Action(() => { label1.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                            ProcessStartInfo ps = new ProcessStartInfo();
                            ps.FileName = datafolder + @"\clients\" + robloxversion + @"\Studio\RobloxStudioBeta.exe";
                            Process.Start(ps);
                            await Task.Delay(5000);
                            this.Invoke(new Action(() => { this.Hide(); }));
                        }
                    }
                    else
                    {
                        if (Properties.Settings.Default.UsePatchInStudio)
                        {
                            if (UseJoinJSONLink) SetupJoinScript("127.0.0.1", 53640);
                            SetupGameFiles();
                            LoadAssets();
                        }
                       if (shutdown == false)
                        {
                            button1.Invoke(new Action(() => { button1.Visible = false; }));
                            ProcessStartInfo ps = new ProcessStartInfo();
                            ps.FileName = datafolder + @"\clients\" + robloxversion + @"\Studio\RobloxStudioBeta.exe";
                            ProcessStartInfo ps1 = new ProcessStartInfo();
                            ps1.UseShellExecute = false;
                            ps1.FileName = datafolder + @"\tools\node\node.exe";
                            ps1.RedirectStandardOutput = true;
                            ps1.CreateNoWindow = true;

                            if (Properties.Settings.Default.useAuth)
                            {
                                if (Properties.Settings.Default.avatarR15)
                                {
                                    if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer" : "");
                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer" : "");
                                }
                                else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer" : "");
                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer" : "");
                            }
                            else
                            {
                                if (Properties.Settings.Default.avatarR15)
                                {
                                    if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer" : "");
                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer" : "");
                                }
                                else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer" : "");
                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer" : "");
                            }
                            ps1.WindowStyle = ProcessWindowStyle.Hidden;
                            ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";

                            if (Properties.Settings.Default.username.Length > 2 && Properties.Settings.Default.UserId >= 0)
                            {

                                if (Properties.Settings.Default.UsePatchInStudio)
                                {
                                    if (IsNodeFromAppRunning() == false)
                                    {
                                        Process test = Process.Start(ps1);

                                        label1.Invoke(new Action(() => { label1.Text = "Waiting for node server to start..."; }));
                                        test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                        {
                                            if (e1.Data != null)
                                            {
                                                if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                {
                                                    setupAvatarOnServer();
                                                    label1.Invoke(new Action(() => { label1.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                    Process roblox = Process.Start(ps);
                                                    roblox.EnableRaisingEvents = true;
                                                    roblox.Exited += Roblox_Exited;
                                                    test.CancelOutputRead();
                                                    await Task.Delay(5000);
                                                    this.Invoke(new Action(() => { this.Hide(); }));
                                                }
                                            }
                                        });
                                        test.BeginOutputReadLine();
                                    }
                                    else
                                    {
                                        setupAvatarOnServer();
                                        label1.Invoke(new Action(() => { label1.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                        Process roblox = Process.Start(ps);
                                        roblox.EnableRaisingEvents = true;
                                        roblox.Exited += Roblox_Exited;
                                        await Task.Delay(5000);
                                        this.Invoke(new Action(() => { this.Hide(); }));
                                    }
                                }
                                else
                                {
                                    label1.Invoke(new Action(() => { label1.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                    Process roblox = Process.Start(ps);
                                    roblox.EnableRaisingEvents = true;
                                    roblox.Exited += Roblox_Exited;
                                    await Task.Delay(5000);
                                    this.Invoke(new Action(() => { this.Hide(); }));
                                }

                            }
                            else
                            {
                                MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }

                    }
                }
                catch (Exception e1)
                {
                    MessageBox.Show("Something went wrong while trying to launch Studio, please look in the error message for details: " + e1.Message, "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine("<ERROR> Something went wrong when attempting to launch Studio! Please look in the error below and report it to the developer if it's launcher-sided!\n" + e1.Message + "\nStack Trace:\n" + e1.StackTrace);
                }
            });
            thread.TrySetApartmentState(ApartmentState.MTA);
            thread.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ClearAssets();
            RemoveGameFiles();
            Application.Exit();
        }
    }
    }

