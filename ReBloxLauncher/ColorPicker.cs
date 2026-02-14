using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReBloxLauncher
{
    public partial class ColorPicker : Form
    {
        Form1 launcher1;
        public ColorPicker(Form1 launcher)
        {
            InitializeComponent();
            launcher1 = launcher;
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

        private void ColorPicker_Load(object sender, EventArgs e)
        {
            int lastx = -26;
            int lasty = 52;
            for (int i = 0; i < 1033; i++)
            {
                if (i == 366)
                {
                    i = 1000;
                }
                if (convertBrickColortoColor(i) != Color.Empty)
                {
                    if (lastx == 414)
                    {
                        lastx = -26;
                        lasty = lasty + 40;
                    }
                    Panel panel = new Panel();
                    panel.BackColor = convertBrickColortoColor(i);
                    panel.Name = i.ToString();
                    panel.Size = new Size(35, 35);
                    panel.Location = new Point(lastx + 40, lasty);
                    panel.Click += new EventHandler((sender1, e1) => {
                        this.DialogResult = DialogResult.OK;
                        launcher1.resultBrickColor = int.Parse(panel.Name);
                        this.Close();
                    });
                    lastx = lastx + 40;
                    this.Controls.Add(panel);
                }
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void panel1_MouseEnter(object sender, EventArgs e)
        {
            panel1.BackColor = Color.FromArgb(70, 70, 70);
        }

        private void panel1_MouseLeave(object sender, EventArgs e)
        {
            panel1.BackColor = Color.FromArgb(60, 60, 60);
        }

        private void label1_MouseEnter(object sender, EventArgs e)
        {
            panel1.BackColor = Color.FromArgb(70, 70, 70);
        }

        private void panel1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
