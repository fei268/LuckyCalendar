using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public class CalendarData
{
    // 天干
    private readonly string[] TianGan = { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };
    // 地支
    private readonly string[] DiZhi = { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };
    readonly string[] ChineseDigits = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
    // 农历月名称
    private readonly string[] LunarMonthNames = { "正月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月", "冬月", "腊月" };
    // 农历日名称
    private readonly string[] LunarDayNames =
    {
        "初一","初二","初三","初四","初五","初六","初七","初八","初九","初十",
        "十一","十二","十三","十四","十五","十六","十七","十八","十九","二十",
        "廿一","廿二","廿三","廿四","廿五","廿六","廿七","廿八","廿九","三十"
    };

    /// <summary>
    /// 农历日期
    /// </summary>
    /// <param name="date"></param>
    /// <returns>返回农历日期，农历数字年月日</returns>
    public (string LunarDate, string RunMonth, int LunarYear, int LunarMonth, int LunarDay,string GzYear) GetChineseCalendar(DateTime date)
    {
        ChineseLunisolarCalendar chineseCalendar = new ChineseLunisolarCalendar();

        // 获取农历年、月、日
        int lunarYear = chineseCalendar.GetYear(date);
        int lunarMonthRaw = chineseCalendar.GetMonth(date); // 可能是 1..13 (若有闰月)
        int lunarDay = chineseCalendar.GetDayOfMonth(date);

        // 处理闰月：把 lunarMonthRaw 转换为真实 1..12，并记录 isLeapMonth
        int leapMonth = chineseCalendar.GetLeapMonth(lunarYear); // 0 表示无闰月
        bool isLeapMonth = false;
        int lunarMonth = lunarMonthRaw;

        if (leapMonth > 0)
        {
            if (lunarMonthRaw == leapMonth)
            {
                // 当前为闰月，如 leapMonth==9 表示闰8月，这时真实月份为 leapMonth-1
                isLeapMonth = true;
                lunarMonth = leapMonth - 1;
            }
            else if (lunarMonthRaw > leapMonth)
            {
                // 闰月之后的月份索引需减 1
                lunarMonth = lunarMonthRaw - 1;
            }
        }

        // 农历日期文本
        string runMonth = isLeapMonth ? "闰" + LunarMonthNames[lunarMonth - 1] : "";
        string lunarMonthStr = LunarMonthNames[lunarMonth - 1];
        string lunarDayStr = LunarDayNames[lunarDay - 1];

        string gzYear = GetGanZhiYear(lunarYear);

        return ($"{lunarMonthStr}{lunarDayStr}", runMonth, lunarYear, lunarMonth, lunarDay, gzYear);
    }

    /// <summary>
    /// 获取天干地支年
    /// </summary>
    string GetGanZhiYear(int lunarYear)
    {
        int ganIndex = Mod(lunarYear - 4, 10); // 甲子年为公元 4 年对应索引 0
        int zhiIndex = Mod(lunarYear - 4, 12);
        return $"{TianGan[ganIndex]}{DiZhi[zhiIndex]}";
    }

    /// <summary>
    /// 简单模运算，保证返回正数
    /// </summary>
    static int Mod(int x, int m)
    {
        int r = x % m;
        if (r < 0) r += m;
        return r;
    }

    /// <summary>
    /// 读取txt24节气
    /// </summary>
    /// <param name="year"></param>
    /// <returns></returns>
    public static Dictionary<string, DateTime> LoadSolarTerms(int year)
    {
        Dictionary<string, DateTime>  solarTerms = new Dictionary<string, DateTime>();

        TextAsset ta = Resources.Load<TextAsset>($"solarterms_year/{year}");
        if (ta == null)
        {
            Debug.LogWarning("节气文件不存在！");
            return solarTerms;
        }

        // 按行解析
        string[] lines = ta.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var parts = line.Split(',');
            if (parts.Length == 2 && DateTime.TryParse(parts[1], out DateTime dt))
            {
                solarTerms[parts[0]] = dt;
            }
        }
        return solarTerms;
    }
}
