using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Label = UnityEngine.UIElements.Label;
using Debug = UnityEngine.Debug;
using System.IO;
using System.Numerics;
using Vector2 = UnityEngine.Vector2;
using System.Text;

public class Default : MonoBehaviour
{
    private string _langCode;//多语言
    StyleColor goodColor = new StyleColor(new Color32(204, 0, 0, 255));//红
    StyleColor normalColor = new StyleColor(new Color32(0, 102, 102, 255));//深绿

    UIDocument uiDocument;//根界面
    VisualElement root;//根界面下的默认首元素

    SwipeView swipe;//滑动页面

    VisualElement calMenu;
    List<Button> menuBtn;

    VisualElement calConts;//月历区

    ScrollView normalCont;//初始文字内容
    VisualElement contsPage, detailsCont, ziweiPan;//内容容器

    List<Label> luckTimeNum, luckTime;//吉时

    AudioClip[] popSounds;//泡泡声音组
    Sprite normalSprite, burstSprite, dynamicTexture;//泡泡背景

    Button weekBtn;//星期按钮

    DateTime currentDate;//选择后的日期

    TextField noteInput;
    string noteKey;//记事本键
    Label currentLab;// 当前点击日期的小红点

    HolidayImageLoader holidayLoader;
    CalendarData cal=new CalendarData();

    Dictionary<string, DateTime> solarTerms;
    List<(int Day, string Name, bool Jia)> holidays;
    string holiToday;


    List<VisualElement> nineElement;


    void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;
        //内容滑动展示
        contsPage = root.Q<VisualElement>("Contents");
        swipe = new SwipeView(contsPage);
        contsPage.AddManipulator(swipe);
    }

    async void Start()
    {
        _langCode = Application.systemLanguage switch
        {
            SystemLanguage.Japanese => "ja",
            SystemLanguage.English => "en",
            _ => "zh"
        };
        //_langCode = "en";
        await LangManager.LoadAsync(_langCode);

        //加载点击月历时的声音
        popSounds = Resources.LoadAll<AudioClip>("popsound");
        normalSprite = Resources.Load<Sprite>("dayIbackimg");
        burstSprite = Resources.Load<Sprite>("dayIback_burst");
        dynamicTexture = Resources.Load<Sprite>("s02");

        //初始文字-相当于序言
        MainText();

        //吉时
        luckTimeNum = root.Q<VisualElement>("LuckTimeNum").Query<Label>().ToList();
        InitLuckTime(DateTime.Now);

        //导航按钮
        calMenu = uiDocument.rootVisualElement.Q<VisualElement>("Menu");
        menuBtn = calMenu.Query<Button>().ToList();
        GetMoonPhaseIndex(currentDate);
        if (menuBtn.Count == 3)
        {
            menuBtn[0].clicked += () => ChangeMonth(-1);
            menuBtn[1].clicked += () => ChangeMonth(0);
            menuBtn[2].clicked += () => ChangeMonth(1);
        }

        //月历
        holidayLoader = new HolidayImageLoader();
        InitCalendar(DateTime.Now.Year, DateTime.Now.Month);

        //日历
        InitCalDetail(DateTime.Now);

        //记事本
        noteInput = root.Q<TextField>("NoteInput");
        noteInput.RegisterValueChangedCallback(OnNoteChanged);

    }

    void Update()
    {
        //当前时间显示
        if (luckTimeNum == null || luckTimeNum.Count != 12) return;

        int hour = DateTime.Now.Hour;
        int minute = DateTime.Now.Minute;
        string colon = (DateTime.Now.Second % 2 == 0) ? ":" : " ";
        int index = 0;
        for (int i = 0; i < 12; i++)
        {
            int startHour = i == 0 ? 23 : (i * 2 - 1); // 对应 JSON 键序列
            if (startHour == 23)
            {
                if (hour >= 23 || hour < 1)
                {
                    index = i;
                    break;
                }
            }
            else
            {
                if (hour >= startHour && hour < startHour + 2)
                {
                    index = i;
                    break;
                }
            }
        }

        luckTimeNum[index].text = $"{hour:D2}{colon}{minute:D2}";
    }


    /// <summary>
    /// 初始化吉时
    /// </summary>
    void InitLuckTime(DateTime date)
    {
        luckTime = root.Q<VisualElement>("LuckTimes").Query<Label>().ToList();

        if (luckTime.Count != 12) return;

        // 获取十二地支
        string[] shiChenArray = new string[12];
        for (int i = 0; i < 12; i++)
        {
            string key = (i == 0) ? "23" : ((i - 1) * 2 + 1).ToString();
            shiChenArray[i] = LangManager.Get("ShiChenTable", key);
            luckTime[i].text = shiChenArray[i]; // 先赋地支
        }
    }

    /// <summary>
    /// 上个月下个月
    /// </summary>
    /// <param name="offset"></param>
    void ChangeMonth(int offset)
    {
        DateTime curDate = offset switch
        {
            -1 => new DateTime(currentDate.AddMonths(-1).Year, currentDate.AddMonths(-1).Month, 1),
            0 => DateTime.Now,
            1 => new DateTime(currentDate.AddMonths(1).Year, currentDate.AddMonths(1).Month, 1),
            _ => currentDate
        };
        InitCalendar(curDate.Year, curDate.Month);
    }

    /// <summary>
    /// 初始化月历
    /// </summary>
    /// <param name="year"></param>
    /// <param name="month"></param>
    async void InitCalendar(int year, int month)
    {
        if (year < 1901 || year > 2040)
        {
            return;
        }
        calConts = root.Q<VisualElement>("Calendars");
        calConts.Clear();

        solarTerms = CalendarData.LoadSolarTerms(year);

        DateTime today = DateTime.Today;
        DateTime firstDay = new DateTime(year, month, 1);
        int startOffset = (int)firstDay.DayOfWeek; // Sunday=0
        int daysInMonth = DateTime.DaysInMonth(year, month);

        int totalCols = 7;
        int dateRows = 6;

        JArray weekNamesList = JArray.Parse(LangManager.Get("UI", "WeeksName"));

        for (int col = 0; col < totalCols; col++)
        {
            weekBtn = new Button();
            weekBtn.text = totalCols == weekNamesList.Count ? weekNamesList[col].ToString() : "";

            weekBtn.style.height = new StyleLength(new Length(10f, LengthUnit.Percent));
            weekBtn.style.width = new StyleLength(new Length(100f / totalCols, LengthUnit.Percent));

            // 高亮当天对应的星期按钮
            if (col == (int)DateTime.Today.DayOfWeek)
                weekBtn.AddToClassList("highlight");

            calConts.Add(weekBtn);
        }

        //节日活动背景图 这里正常，测试暂时避免请求
        //var monthTextures = await holidayLoader.LoadMonthTexturesAsync(year, month);
        var monthTextures = new Dictionary<int, List<Texture2D>>();

        // 日期行
        for (int row = 0; row < dateRows; row++)
        {
            for (int col = 0; col < totalCols; col++)
            {
                int dayIndex = row * totalCols + col - startOffset + 1;

                StyleLength h = new StyleLength(new Length(15f, LengthUnit.Percent));
                StyleLength w = new StyleLength(new Length(100f / totalCols, LengthUnit.Percent));

                Button dateBtn = new Button();
                dateBtn.style.height = h;
                dateBtn.style.width = w;
                dateBtn.style.backgroundImage = new StyleBackground(normalSprite);

                // lab：记事小红点，角标
                Label lab = new Label();
                lab.style.width = Length.Percent(25);
                lab.style.height = Length.Percent(25);
                lab.style.backgroundColor = Color.red; // 红色背景
                lab.style.display = DisplayStyle.None;//默认隐藏
                dateBtn.Add(lab);

                //节假日广告等图片
                Label labAD = new Label();
                labAD.style.position = Position.Absolute;
                labAD.style.width = Length.Percent(100);
                labAD.style.height = Length.Percent(100);
                labAD.style.opacity = 0.5f; // 半透明
                dateBtn.Add(labAD);

                if (dayIndex >= 1 && dayIndex <= daysInMonth)
                {
                    int day = dayIndex;
                    DateTime selectedDate = new DateTime(year, month, day);
                    currentDate = selectedDate;
                    noteKey = selectedDate.ToString("yyyyMMdd");

                    dateBtn.text = dayIndex.ToString();

                    // 高亮当天日期
                    if (selectedDate.Date == DateTime.Now.Date)
                    {
                        dateBtn.AddToClassList("highlight"); // USS 样式类控制颜色
                    }

                    if (PlayerPrefs.HasKey(noteKey))
                    {
                        lab.style.display = DisplayStyle.Flex;
                    }

                    if (monthTextures.TryGetValue(day, out List<Texture2D> textures) && textures.Count > 0)
                    {
                        labAD.style.backgroundImage = new StyleBackground(textures[0]);
                    }

                    dateBtn.clicked += () =>
                    {
                        if (popSounds != null && popSounds.Length > 0)
                        {
                            int index = UnityEngine.Random.Range(0, popSounds.Length);
                            AudioClip popSound = popSounds[index];
                            AudioSource.PlayClipAtPoint(popSound, Camera.main.transform.position);
                        }

                        ClickDayMessage(lab, selectedDate);

                        dateBtn.experimental.animation
                   .Scale(0.85f, 100) // 缩小到 85%，100ms
                   .OnCompleted(() =>
                   {
                       dateBtn.experimental.animation.Scale(1f, 200); // 回弹到 100%，200ms
                   });

                        // 切换背景图并在 1 秒后恢复
                        if (burstSprite != null)
                        {
                            dateBtn.style.backgroundImage = new StyleBackground(burstSprite);
                        }
                        dateBtn.schedule.Execute(_ =>
                        {
                            if (normalSprite != null)
                                dateBtn.style.backgroundImage = new StyleBackground(normalSprite);
                        }).ExecuteLater(1000);

                    };
                }
                else
                {
                    dateBtn.SetEnabled(false);// 按钮不可点击
                    dateBtn.style.opacity = 0.2f;
                }

                calConts.Add(dateBtn);
            }
        }
    }

    /// <summary>
    /// 日期按钮被点击时
    /// </summary>
    /// <param name="lab"></param>
    /// <param name="date"></param>
    void ClickDayMessage(Label lab, DateTime date)
    {
        swipe.JumpToPage(1);//跳到第2页

        InitLuckTime(date);//每天吉时不同

        GetMoonPhaseIndex(date);//改变导航背景图

        InitCalDetail(date);//显示日历

        //显示记事
        currentLab = lab;
        noteKey = date.ToString("yyyyMMdd");
        if (PlayerPrefs.HasKey(noteKey))
            noteInput.SetValueWithoutNotify(PlayerPrefs.GetString(noteKey));
        else
            noteInput.SetValueWithoutNotify("");
    }

    /// <summary>
    /// 初始化日历
    /// </summary>
    /// <param name="date"></param>
    async void InitCalDetail(DateTime date)
    {
        detailsCont = root.Q<VisualElement>("Details");

        Label dongnan = detailsCont.Q<VisualElement>("DongNan").Query<Label>();
        Label nan = detailsCont.Q<VisualElement>("Nan").Query<Label>();
        Label xinan = detailsCont.Q<VisualElement>("XiNan").Query<Label>();
        Label dong = detailsCont.Q<VisualElement>("Dong").Query<Label>();
        List<Label> zhong = detailsCont.Q<VisualElement>("Zhong").Query<Label>().ToList();
        Label xi = detailsCont.Q<VisualElement>("Xi").Query<Label>();
        Label dongbei = detailsCont.Q<VisualElement>("DongBei").Query<Label>();
        Label bei = detailsCont.Q<VisualElement>("Bei").Query<Label>();
        Label xibei = detailsCont.Q<VisualElement>("XiBei").Query<Label>();
        List<Label> others = detailsCont.Q<VisualElement>("Others").Query<Label>().ToList();
        List<Label> nineLabelY = detailsCont.Q<VisualElement>("NineGongY").Query<Label>().ToList();
        List<Label> nineLabelD = detailsCont.Q<VisualElement>("NineGongD").Query<Label>().ToList();

        var result = cal.GetChineseCalendar(date);

        //1,中国77年 民国114年 令和7年
        dongnan.text = $"{date.Year}-{date.Month}-{date.Day} \n";
        dongnan.text += result.LunarDate+ "\n";
        dongnan.text += result.LunarYear.ToString();

        dong.text = result.GzYear;

        /*
        TextAsset ta = Resources.Load<TextAsset>($"solarterms_year/{date.Year}");
        dongbei.text = ta == null ? "TA NULL" : ta.text;
        */

        foreach (var kv in solarTerms)
        {
            if (kv.Value.Month == date.Month)
                dongbei.text = $"{kv.Key} : {kv.Value}";
        }
    }

    /// <summary>
    /// 保存记事
    /// </summary>
    /// <param name="evt"></param>
    void OnNoteChanged(ChangeEvent<string> evt)
    {
        string text = evt.newValue ?? "";

        // 过滤危险字符
        text = System.Text.RegularExpressions.Regex.Replace(text, "[\'\";\\\\/<>\\{}]", "");

        // 保存或者删除
        if (string.IsNullOrWhiteSpace(text))
            PlayerPrefs.DeleteKey(noteKey);
        else
            PlayerPrefs.SetString(noteKey, text);
        PlayerPrefs.Save();

        // 更新小红点（无需遍历，直接更新当前选中的）
        if (currentLab != null)
            currentLab.style.display = PlayerPrefs.HasKey(noteKey) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    /// <summary>
    /// 月相 导航按钮背景图
    /// 待完成按不同大时区显示不同图片
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    int GetMoonPhaseIndex(DateTime date)
    {
        // 基准新月
        DateTime baseNewMoon = new DateTime(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc);
        DateTime utcDate = date.ToUniversalTime();

        const double synodicMonth = 29.530588853;

        // 月龄 = 当前时间 - 基准新月 
        double moonAge = ((utcDate - baseNewMoon).TotalDays) % synodicMonth;
        if (moonAge < 0) moonAge += synodicMonth;

        // 转换为8阶段索引（0=新月, 4=满月）
        int phaseIndex = (int)Math.Floor((moonAge / synodicMonth) * 8 + 0.5) % 8;

        string moonPath = $"moonpic/{phaseIndex + 1}";
        Sprite sprite = Resources.Load<Sprite>(moonPath);

        var bg = new StyleBackground(sprite);
        foreach (var btn in menuBtn)
            btn.style.backgroundImage = bg;
        return phaseIndex;
    }

    /// <summary>
    /// 竖排固定文字
    /// </summary>
    /// <param name="text"></param>
    /// <param name="linesPerColumn"></param>
    /// <param name="root"></param>
    void MainText()
    {
        normalCont = root.Q<ScrollView>("NormalCont");
        normalCont.Clear();

        // 获取 MainText JSON
        string mainTextJson = LangManager.Get("UI", "MainText");

        List<string> lines = new List<string>();
        try
        {
            if (!string.IsNullOrEmpty(mainTextJson))
            {
                JArray arr = JArray.Parse(mainTextJson);
                foreach (var item in arr)
                {
                    if (item != null)
                        lines.Add(item.ToString());
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"解析 MainText 失败: {e.Message}");
        }

        if (_langCode == "zh")
        {
            // 中文竖排：关键在 RowReverse
            normalCont.contentContainer.style.flexDirection = FlexDirection.RowReverse;

            foreach (var line in lines)
            {
                AddOneColumn(normalCont.contentContainer, new List<string> { line });
            }
        }
        else
        {
            // 英文/日文横排：恢复默认 Column，否则会被 RowReverse 挤掉
            normalCont.contentContainer.style.flexDirection = FlexDirection.Column;

            Label lbl = new Label();
            lbl.style.fontSize = 16;
            lbl.style.whiteSpace = WhiteSpace.Normal;    // 自动换行
            lbl.style.flexWrap = Wrap.Wrap;
            lbl.style.width = Length.Percent(100);

            lbl.text = string.Join("\n", lines);

            normalCont.contentContainer.Add(lbl);
        }
    }

    //竖排文字次方法
    void AddOneColumn(VisualElement container, List<string> lines)
    {
        Label lbl = new Label();
        lbl.style.whiteSpace = WhiteSpace.Normal;
        lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
        lbl.style.fontSize = 12;
        lbl.style.width = 14; // 每列窄宽度，竖排效果

        lbl.text = string.Join("\n", lines);

        container.Add(lbl);
    }

    /// <summary>
    /// 六曜字体颜色
    /// </summary>
    /// <param name="liuyao"></param>
    /// <param name="ve"></param>
    string GetLiuYaoColor(string liuyao)
    {
        int hour = DateTime.Now.Hour;
        switch (liuyao)
        {
            case "大安": return "green";
            case "赤口": return (hour > 11 && hour < 13) ? "green" : "black";
            case "先勝": return (hour < 12) ? "green" : "black";
            case "友引": return (hour < 11 || hour > 13) ? "green" : "black";
            case "先負": return (hour > 12) ? "green" : "black";
            default: return "black";
        }
    }

    /// <summary>
    /// 绿色吉黑色凶，红色用于节日不使用
    /// </summary>
    /// <param name="name"></param>
    /// <param name="luck"></param>
    /// <returns></returns>
    string FormatLuckText(string name, string luck)
    {
        return luck == "吉" ? $" <color=green>{name} {luck}</color>" : $" <color=black>{name} {luck}</color>";
    }
}


///
///另一个方法
/// 
/// <summary>
/// 轻量绘制容器
/// </summary>
public class ImmediateVisualElement : VisualElement
{
    public ImmediateVisualElement()
    {
        // 允许使用绘制回调
        generateVisualContent += _ => { };
    }
}