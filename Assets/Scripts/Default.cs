using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json.Linq;
using Label = UnityEngine.UIElements.Label;
using Debug = UnityEngine.Debug;
using System.IO;

public class Default : MonoBehaviour
{
    private string _langCode;//多语言

    UIDocument uiDocument;//根界面
    VisualElement root;//根界面下的默认首元素

    SwipeView swipe;//滑动页面

    VisualElement calMenu;
    List<Button> menuBtn;

    VisualElement calConts;//月历区

    ScrollView normalCont;//初始文字内容
    VisualElement contsPage, detailsCont,ziweiPan;//内容容器

    List<Label> luckTimeNum, luckTime;//吉时

    AudioClip[] popSounds;//泡泡声音组

    Button weekBtn;

    DateTime currentDate;

    void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;
        //内容滑动展示
        contsPage= root.Q<VisualElement>("Contents");
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

        //初始文字-相当于序言
        MainText();

        //吉时
        luckTimeNum = root.Q<VisualElement>("LuckTimeNum").Query<Label>().ToList();
        InitLuckTime();

        //导航按钮
        calMenu = uiDocument.rootVisualElement.Q<VisualElement>("Menu");
        menuBtn = calMenu.Query<Button>().ToList();
        GetMoonPhaseIndex(currentDate);
        JArray menuNames = JArray.Parse(LangManager.Get("UI", "MenuName"));

        if (menuBtn.Count == 3)
        {
            menuBtn[0].text= menuNames.Count==3 ? menuNames[0].ToString() : "";
            menuBtn[1].text = menuNames.Count == 3 ? menuNames[1].ToString() : "";
            menuBtn[2].text = menuNames.Count == 3 ? menuNames[2].ToString() : "";
            menuBtn[0].clicked += () => ChangeMonth(-1);
            menuBtn[1].clicked += () => ChangeMonth(0);
            menuBtn[2].clicked += () => ChangeMonth(1);
        }



        //月历
        InitCalendar(DateTime.Now.Year, DateTime.Now.Month);
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
    void InitLuckTime()
    {
        luckTime = root.Q<VisualElement>("LuckTimes").Query<Label>().ToList();
        
        for (int i = 0; i < 12; i++)
        {
            string key;
            if (i == 0) key = "23";
            else key = ((i - 1) * 2 + 1).ToString(); // 对应 JSON 的 key

            luckTime[i].text = LangManager.Get("ShiChenTable", key);
        }
    }

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
    void InitCalendar(int year, int month)
    {
        calConts = root.Q<VisualElement>("Calendars");
        calConts.Clear();

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

            // 高亮今天对应的星期按钮
            if (col == (int)DateTime.Today.DayOfWeek)
                weekBtn.AddToClassList("highlight");

            calConts.Add(weekBtn);
        }

        Sprite normalSprite = Resources.Load<Sprite>("dayIbackimg");
        Sprite burstSprite = Resources.Load<Sprite>("dayIback_burst");
        Sprite dynamicTexture = Resources.Load<Sprite>("dayIbackimg");

        // 日期行
        for (int row = 0; row < dateRows; row++)
        {
            for (int col = 0; col < totalCols; col++)
            {
                int dayIndex = row * totalCols + col - startOffset + 1;
                Button dateBtn = new Button();
                dateBtn.style.height = new StyleLength(new Length(15f, LengthUnit.Percent));
                dateBtn.style.width = new StyleLength(new Length(100f / totalCols, LengthUnit.Percent));
                dateBtn.style.backgroundImage = new StyleBackground(normalSprite);

                // === 背景3：动态背景 ===
                VisualElement bg3 = new VisualElement();
                bg3.style.position = Position.Absolute;
                bg3.style.top = 0;
                bg3.style.left = 0;
                bg3.style.right = 0;
                bg3.style.bottom = 0;
                bg3.style.backgroundImage = new StyleBackground(dynamicTexture); // StreamingAssets 载入
                dateBtn.Add(bg3);

                if (dayIndex >= 1 && dayIndex <= daysInMonth)
                {
                    DateTime selectedDate = new DateTime(year, month, dayIndex);
                    currentDate = selectedDate;

                    dateBtn.text = dayIndex.ToString();

                    // 高亮当天日期
                    if (dayIndex == today.Day)
                    {
                        dateBtn.AddToClassList("highlight"); // USS 样式类控制颜色
                    }

                    dateBtn.clicked += () =>
                    {
                        popSounds = Resources.LoadAll<AudioClip>("popsound");
                        if (popSounds != null && popSounds.Length > 0)
                        {
                            int index = UnityEngine.Random.Range(0, popSounds.Length);
                            AudioClip popSound = popSounds[index];
                            AudioSource.PlayClipAtPoint(popSound, Camera.main.transform.position);
                        }

                        //切换背景图
                        if (burstSprite != null)
                        {
                            dateBtn.style.backgroundImage = new StyleBackground(burstSprite);
                        }
                        dateBtn.schedule.Execute(_ =>
                        {
                            if (normalSprite != null)
                                dateBtn.style.backgroundImage = new StyleBackground(normalSprite);
                        }).ExecuteLater(1000);

                        ClickDayMessage(selectedDate);
                    };
                }
                else
                {
                    dateBtn.text = "";
                    dateBtn.SetEnabled(false);
                }

                calConts.Add(dateBtn);
            }
        }
    }


    void ClickDayMessage(DateTime date)
    {
        swipe.JumpToPage(1);//跳到第2页

        GetMoonPhaseIndex(date);//改变导航背景图

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

    void AddOneColumn(VisualElement container, List<string> lines)
    {
        Label lbl = new Label();
        lbl.style.whiteSpace = WhiteSpace.Normal;
        lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
        lbl.style.fontSize = 14;
        lbl.style.width = 18; // 每列窄宽度，竖排效果

        lbl.text = string.Join("\n", lines);

        container.Add(lbl);
    }


}
