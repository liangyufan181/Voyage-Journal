// =============================================================================
// 经济循环粗略模拟器（Editor 菜单一键运行）
// 用途：在不手动跑一局的情况下，粗略检验"初始资源 + 商店价格"是否平衡。
// 运行：Unity 编辑器菜单 → Tools → [模拟] 经济循环  在 Console 查看多局统计。
// 说明：未模拟格子的精确寻路/风向全速/洋流成本，只按地块生成概率聚合抽样，
//       重点验证"扣耗 vs 钓鱼回血 vs 卖余粮换金币 vs 修理/升级"能否转得动。
// =============================================================================
using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

public static class EconomySimulator
{
    [MenuItem("Tools/[模拟] 经济循环")]
    public static void RunAll()
    {
        Debug.Log("========== 经济平衡模拟（每档 4000 局 × 80 天，无瞭望硬闯=最坏情况） ==========");
        Debug.Log("说明：已加入【保底安全走廊】建模——玩家沿走廊航行时不再吃暗礁/风暴，" +
                  "safeShareOrCorridor = 0.72 表示约 72% 的航行天数走的是安全走廊。");
        Sim("Easy   ", 130, 130, 130, 650);
        Sim("Normal ", 100, 100, 110, 500);
        Sim("Hard   ", 85, 85, 100, 450);
        Debug.Log("========== 目标参考（含走廊）：Easy >=98%  Normal 90~99%  Hard ~80~95%（沿走廊必活，偏离抄近道才吃耐久） ==========");
        Debug.Log("想要看“抄近道更狠”（更低 safeShare）下的存活率？请运行 [校准] 安全走廊滑动菜单。");
    }

    // ⚠️ 校准宏观：滑动“偏离走廊抄近道”的意愿（safeShare 越低 = 越是硬闯抄近道），
    //    观察困难模式存活率随"要不要付出耐久"的曲线，用于后续调初始数值。
    [MenuItem("Tools/[校准] 安全走廊·困难档存活曲线")]
    public static void RunHardSweep()
    {
        Debug.Log("========== 困难档【食材85 水85 耐久100 金450】按走廊依赖度扫描 ==========");
        Sim("H-纯走廊  ", 85, 85, 100, 450, safeShare: 0.95d);
        Sim("H-稍抄近道", 85, 85, 100, 450, safeShare: 0.80d);
        Sim("H-对半    ", 85, 85, 100, 450, safeShare: 0.65d);
        Sim("H-硬闯    ", 85, 85, 100, 450, safeShare: 0.45d);
        Debug.Log("目标：纯走廊接近必活（>=95%）；越硬闯越低，体现“牺牲耐久换资源/天数”的代价。");
    }

    private static void Sim(string name, int f, int w, int h, int g, int runs = 4000, int days = 80, double safeShare = 0.72d)
    {
        var rng = new System.Random(12345 + name.GetHashCode());

        int survived = 0, boughtAll = 0, noFish = 0, starved = 0;
        double sumFood = 0, sumWater = 0, sumHull = 0, sumGold = 0, sumRepair = 0, sumUpgrade = 0;

        for (int run = 0; run < runs; run++)
        {
            double food = f, water = w, hull = h, gold = g;
            double totalRepair = 0, totalUpgrade = 0;
            bool dead = false, fishEver = false;

            for (int d = 0; d < days && !dead; d++)
            {
                // ---- 每日基础航行消耗（洋流顺/逆/侧均摊后≈1份） ----
                food -= 1; water -= 1;

                // ---- 保底安全走廊：safeShare 比例的天走在安全航线上，不吃暗礁/风暴 ----
                //     只有"偏离走廊抄近道"的日子，才会按原概率撞暗礁/风暴（这正是不守恒取舍所在） ----
                if (rng.NextDouble() >= safeShare)
                {
                    double roll = rng.NextDouble();
                    if (roll < 0.125d)
                    {
                        hull -= 17;                      // 暗礁
                    }
                    else if (roll < 0.175d)
                    {
                        water += 30; hull -= 15;         // 风暴：+水  损耐久
                    }
                    else if (roll < 0.205d)
                    {
                        food += 5; water += 5;           // 迷惑岛
                    }
                }

                // ---- 钓鱼策略：食物偏少且耐久 >40 时下网 ----
                if (food < 25 && hull > 40)
                {
                    fishEver = true;
                    food += 15 + rng.Next(16);       // 15~30
                    hull -= 10;
                }

                // ---- 修理策略：耐久偏低且买得起（110金/+25耐） ----
                if (hull < 50 && gold >= 110)
                {
                    gold -= 110; hull += 25; totalRepair += 110;
                }

                // ---- 卖余粮筹资：钱少 + 有余粮时，5食+5水→38金（可小幅反复） ----
                while (gold < 240 && food > 30 && water > 30)
                {
                    food -= 5; water -= 5; gold += 38;
                }

                // ---- 应急买水 ----
                if (water < 12 && gold >= 40) { gold -= 40; water += 10; }

                // ---- 攒钱买三大升级（湛蓝 250 / 星盘 180 / 草药 15） ----
                if (!dead && gold >= 250 + 180 + 15)
                {
                    gold -= 250 + 180 + 15; totalUpgrade += 250 + 180 + 15;
                }

                if (food <= 0 || water <= 0 || hull <= 0) { dead = true; if (dead && food <= 0) starved++; }
            }

            if (dead) continue;
            survived++;
            if (totalUpgrade >= 445) boughtAll++;
            if (!fishEver) noFish++;
            sumFood += food; sumWater += water; sumHull += hull; sumGold += gold;
            sumRepair += totalRepair; sumUpgrade += totalUpgrade;
        }

        double n = survived;
        Debug.Log(
            $"[{name}] 存活率 {survived}/{runs} = {(100.0 * survived / runs).ToString("0.0")}%  | " +
            $"存活局均质: 食{sumFood/n:0} 水{sumWater/n:0} 耐{sumHull/n:0} 金{sumGold/n:0} | " +
            $"三升级全买率 {(100.0 * boughtAll / runs).ToString("0.0")}%  | " +
            $"人均修理费 {sumRepair/n:0} 升级费 {sumUpgrade/n:0} | " +
            $"饿死 {starved} / 不钓鱼也想活 {noFish}"
        );
    }
}
#endif