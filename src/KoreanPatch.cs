// 한글패치 (모드 로더용): 게임 파일을 바꾸지 않고 실행 중에 한글 번역을 넣는다.
//  - 번역 파일: Mods\KoreanPatch\*-en.txt  (게임 언어를 English 로 두면 한글이 나옴)
//  - 메인 메뉴의 고정 문구(제작진 화면 등) 번역
//  - 한글 글꼴: 게임 글꼴에 없는 한글은 Windows 글꼴로 대신 그림 (원본 게임의 중국어와 같은 방식)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ModLoader;
using UnityEngine;
using UnityEngine.UI;

[assembly: ModInfo("한글패치", "1.2.1", Description = "게임 파일을 바꾸지 않고 한글 번역을 적용합니다. 게임 설정의 언어를 English 로 두세요.",
    SettingsNote = "글꼴 설정은 게임을 다시 시작하면 적용됩니다.")]

namespace KoreanPatch
{
    public static class Cfg
    {
        [Setting("글꼴", "게임 글꼴", Options = "워헤이븐|명조 (마루 부리 · 바탕)|맑은 고딕|게임 기본 글꼴 + 한글만 대체|fonts 폴더에 넣은 글꼴",
            Desc = "게임 화면 글꼴을 바꿉니다. 글꼴 파일은 Mods\\KoreanPatch\\fonts → 내 계정 글꼴 → Windows 글꼴 순으로 찾습니다. 없으면 명조 → 맑은 고딕 순. 다시 시작하면 적용됩니다.")]
        public static int FontChoice = 0;
        [Setting("번역", "메인 메뉴 고정 문구 번역", Desc = "제작진 화면 등 번역 파일에 없는 메인 메뉴 문구를 한글로 바꿉니다.")]
        public static bool TranslateMenu = true;
    }

    public static class Patch
    {
        static readonly Dictionary<string, string> MenuText = new Dictionary<string, string>
        {
            {"3D Art","3D 아트"},{"Achievements","업적"},{"Animations","애니메이션"},{"Best Heroes","최고의 영웅들"},
            {"CONTRIBUTORS","도움을 주신 분들"},{"CREATORS","제작진"},{"TRANSLATORS","번역가"},{"Disabled","사용 안 함"},
            {"Game Design","게임 디자인"},{"Illustrations","일러스트"},{"Level Design","레벨 디자인"},{"Music","음악"},
            {"Narrative Design","내러티브 디자인"},{"Programming","프로그래밍"},{"QA Testing","QA 테스트"},{"Scenario","시나리오"},
            {"Sketches & visual design","스케치 및 비주얼 디자인"},{"Sound & Music","사운드 및 음악"},{"VFX","시각 효과"},
            {"Simple","단순"},{"STORY","스토리"},
            {"✶ Created by Stellar Fish ✶","✶ 제작: Stellar Fish ✶"},
            {"✶ Some of the languages may include AI-assisted translations ✶","✶ 일부 언어는 AI 지원 번역을 포함할 수 있습니다 ✶"},
        };

        static string Dir { get { return Path.Combine(Loader.ModsDir ?? "", "KoreanPatch"); } }
        static int _logged;
        static bool _fontsDone;

        static void Log(string m) { if (_logged++ < 30) Loader.Log("[한글패치] " + m); }

        // LocalizationManager.LoadLocalization 안의 TextAsset.text 읽기를 대체
        [Hook(HookKind.ReplaceCall, "UI.LocalizationManager", "LoadLocalization", Params = 1, CallType = "UnityEngine.TextAsset", CallMethod = "get_text", Count = 0)]
        public static string GetText(TextAsset asset)
        {
            string original = asset ? asset.text : null;
            try
            {
                if (!asset || !asset.name.EndsWith("-en", StringComparison.Ordinal)) return original;
                string path = Path.Combine(Dir, asset.name + ".txt");
                if (!File.Exists(path)) return original;
                string kor = File.ReadAllText(path, Encoding.UTF8);
                if (asset.name == "localization-main-en") kor = MergeMissing(kor, original);
                return kor;
            }
            catch (Exception e) { Log(asset.name + " 적용 실패: " + e.Message); return original; }
        }

        // 게임 업데이트로 새로 생긴 문장은 영어 원문으로 채운다 (빈칸 방지)
        static string MergeMissing(string kor, string eng)
        {
            if (string.IsNullOrEmpty(eng)) return kor;
            var kd = new XmlDocument(); kd.LoadXml(kor);
            var ed = new XmlDocument(); ed.LoadXml(eng);
            var have = new HashSet<string>();
            foreach (XmlElement it in kd.DocumentElement.GetElementsByTagName("Item"))
                have.Add(it.GetAttribute("id"));
            int added = 0;
            foreach (XmlElement it in ed.DocumentElement.GetElementsByTagName("Item"))
            {
                string id = it.GetAttribute("id");
                if (have.Contains(id)) continue;
                kd.DocumentElement.AppendChild(kd.ImportNode(it, true));
                added++;
            }
            if (added == 0) return kor;
            Log("번역 파일에 없는 새 문장 " + added + "개는 영어로 표시됩니다.");
            using (var sw = new StringWriter()) { kd.Save(sw); return sw.ToString(); }
        }

        // 번역을 읽기 직전에 한 번: 한글을 그릴 Windows 글꼴을 대체 글꼴로 등록
        [Hook(HookKind.Prefix, "UI.LocalizationManager", "LoadLocalization", Params = 1, Args = new int[0])]
        public static void BeforeLoad()
        {
            if (_fontsDone) return;
            _fontsDone = true;
            try
            {
                string[] extra = { "MaruBuri", "MaruBuri Regular", "마루 부리", "마루부리", "Batang", "바탕", "Malgun Gothic", "맑은 고딕" };
                int n = 0;
                foreach (Font f in Resources.FindObjectsOfTypeAll<Font>())
                {
                    if (!f || !f.dynamic) continue;
                    var names = (f.fontNames ?? new string[0]).ToList();
                    foreach (var x in extra) if (!names.Contains(x)) names.Add(x);
                    f.fontNames = names.ToArray();
                    n++;
                }
                Log("글꼴 " + n + "개에 한글 대체 글꼴 등록");
            }
            catch (Exception e) { Log("글꼴 설정 실패: " + e.Message); }
            try { if (Cfg.FontChoice != 3) InitFonts(); }
            catch (Exception e) { Log("글꼴 준비 실패: " + e.Message); _regular = _bold = null; }
            _fontInit = true;
        }

        // ------------------------------------------------------------ 글꼴 통째로 바꾸기
        // UnityEngine.UI.Text.font 를 읽을 때 게임 글꼴(NotoSerif) 대신 고른 OS 글꼴을 돌려준다.
        static readonly Dictionary<int, Font> FontMap = new Dictionary<int, Font>();
        static bool _fontInit;
        static Font _regular, _bold;

        [Hook(HookKind.Postfix, "UnityEngine.UI.Text", "get_font", Assembly = "UnityEngine.UI", Params = 0)]
        public static Font TextFont(Font result, Text text)
        {
            long perf = Perf.Begin();
            try
            {
                if ((object)result == null || Cfg.FontChoice == 3) return result;
                int id = result.GetInstanceID();
                Font repl;
                if (!_fontInit) return result;          // 글꼴 준비 전에는 원래 글꼴
                if (FontMap.TryGetValue(id, out repl)) return repl ?? result;
                repl = null;
                string n = result.name ?? "";
                if (n.StartsWith("NotoSerif", StringComparison.OrdinalIgnoreCase))
                    repl = n.IndexOf("Bold", StringComparison.OrdinalIgnoreCase) >= 0 ? (_bold ?? _regular) : _regular;
                FontMap[id] = repl;
                return repl ?? result;
            }
            catch (Exception e) { Log("글꼴 교체 오류: " + e.Message); return result; }
            finally { Perf.End("한글패치 글꼴 교체", perf); }
        }

        static readonly string[] SymbolFallback = { "Malgun Gothic", "맑은 고딕", "Segoe UI Symbol", "Segoe UI" };

        // 글꼴 파일을 찾아볼 폴더: 모드 fonts 폴더 → 내 계정 전용 글꼴 → Windows 글꼴
        static IEnumerable<string> FontFiles()
        {
            var dirs = new List<string> { Path.Combine(Dir, "fonts") };
            string local = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrEmpty(local)) dirs.Add(Path.Combine(Path.Combine(Path.Combine(local, "Microsoft"), "Windows"), "Fonts"));
            string win = Environment.GetEnvironmentVariable("WINDIR");
            if (!string.IsNullOrEmpty(win)) dirs.Add(Path.Combine(win, "Fonts"));
            foreach (var d in dirs)
            {
                string[] files;
                try { if (!Directory.Exists(d)) continue; files = Directory.GetFiles(d); }
                catch { continue; }
                foreach (var f in files)
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext == ".ttf" || ext == ".otf") yield return f;
                }
            }
        }

        static bool IsBoldName(string n)
        {
            n = n.ToLowerInvariant();
            return n.Contains("bold") || n.EndsWith("-b") || n.EndsWith("_b") || n.EndsWith(" b");
        }

        // 파일 이름에 key 가 들어간 글꼴 파일 한 쌍(보통/굵게)을 찾는다. key 가 null 이면 fonts 폴더의 아무 글꼴
        static bool FindFiles(string key, bool onlyModFolder, out string reg, out string bold)
        {
            reg = bold = null;
            string modDir = Path.Combine(Dir, "fonts");
            var hits = FontFiles().Where(f =>
            {
                if (onlyModFolder && !f.StartsWith(modDir, StringComparison.OrdinalIgnoreCase)) return false;
                return key == null || Path.GetFileName(f).ToLowerInvariant().Contains(key);
            }).ToList();
            if (hits.Count == 0) return false;
            var names = hits.Select(Path.GetFileNameWithoutExtension).ToList();
            int ri = names.FindIndex(n => n.ToLowerInvariant().Contains("regular"));
            if (ri < 0) ri = names.FindIndex(n => !IsBoldName(n));
            if (ri < 0) ri = 0;
            int bi = names.FindIndex(IsBoldName);
            reg = hits[ri];
            bold = bi >= 0 ? hits[bi] : reg;
            return true;
        }

        static Font LoadFontFile(string path)
        {
            var f = new Font(path);
            f.name = Path.GetFileNameWithoutExtension(path);
            try { f.fontNames = SymbolFallback; } catch { }     // 이 글꼴에 없는 기호(★ ◆ 등)는 Windows 글꼴로
            return f;
        }

        // 파일에서 읽은 글꼴이 실제로 글자를 그릴 수 있는지 확인 (재질이 없으면 붙여 본다)
        static bool Renderable(Font f)
        {
            try
            {
                f.RequestCharactersInTexture("가나다라ABCabc123", 32, FontStyle.Normal);
                if (f.material == null)
                {
                    Font tmpl = Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(x => x && x.name.StartsWith("NotoSerif", StringComparison.Ordinal) && x.material);
                    Material m = tmpl ? new Material(tmpl.material) : new Material(Shader.Find("UI/Default Font") ?? Shader.Find("GUI/Text Shader"));
                    m.mainTexture = null;
                    m.name = f.name + " Material";
                    f.material = m;
                    f.RequestCharactersInTexture("마바사아DEFdef456", 32, FontStyle.Normal);
                }
                bool ok = f.material != null && f.material.mainTexture != null;
                Log("글꼴 검사 " + f.name + ": 재질 " + (f.material != null ? "있음" : "없음") + ", 글자 텍스처 " + (ok ? "있음" : "없음"));
                return ok;
            }
            catch (Exception e) { Log("글꼴 검사 실패 " + f.name + ": " + e.Message); return false; }
        }

        static bool UseFiles(string reg, string bold, string what)
        {
            try
            {
                _regular = LoadFontFile(reg);
                _bold = bold == reg ? _regular : LoadFontFile(bold);
                if (!Renderable(_regular) || (_bold != _regular && !Renderable(_bold)))
                {
                    Log(what + " 글꼴 파일을 게임에서 그릴 수 없어 다른 글꼴로 대신합니다. (글꼴을 '모든 사용자용으로 설치'하면 해결됩니다)");
                    _regular = _bold = null;
                    return false;
                }
                Log("게임 글꼴 → " + what + " (" + Path.GetFileName(reg) + " / 굵게: " + Path.GetFileName(bold) + ")");
                return true;
            }
            catch (Exception e) { Log("글꼴 파일을 읽지 못함 (" + reg + "): " + e.Message); _regular = _bold = null; return false; }
        }

        static bool UseOS(string[] installed, string[] regs, string[] bolds, string what)
        {
            var set = new HashSet<string>(installed, StringComparer.OrdinalIgnoreCase);
            string r = regs.FirstOrDefault(set.Contains);
            if (r == null) return false;
            string b = bolds.FirstOrDefault(set.Contains) ?? r;
            _regular = Font.CreateDynamicFontFromOSFont(new[] { r }.Concat(SymbolFallback).ToArray(), 16);
            _bold = Font.CreateDynamicFontFromOSFont(new[] { b }.Concat(SymbolFallback).ToArray(), 16);
            Log("게임 글꼴 → " + what + " (" + r + " / 굵게: " + b + ")");
            return true;
        }

        static void InitFonts()
        {
            string[] installed = Font.GetOSInstalledFontNames() ?? new string[0];
            string reg, bold;
            int choice = Cfg.FontChoice;

            if (choice == 4)
            {
                if (FindFiles(null, true, out reg, out bold) && UseFiles(reg, bold, "fonts 폴더 글꼴")) return;
                Log("Mods\\KoreanPatch\\fonts 폴더에 글꼴 파일(.ttf/.otf)이 없어 워헤이븐을 찾아봅니다.");
                choice = 0;
            }
            if (choice == 0)
            {
                var war = installed.Where(x => x.IndexOf("warhaven", StringComparison.OrdinalIgnoreCase) >= 0 || x.Contains("워헤이븐")).ToArray();
                if (war.Length > 0 && UseOS(installed, war.Where(x => !IsBoldName(x)).Concat(war).ToArray(), war.Where(IsBoldName).ToArray(), "워헤이븐")) return;
                if (FindFiles("warhaven", false, out reg, out bold) && UseFiles(reg, bold, "워헤이븐")) return;
                Log("워헤이븐 글꼴 파일을 찾지 못해 명조 글꼴을 씁니다.");
                choice = 1;
            }
            if (choice == 1)
            {
                if (UseOS(installed, new[] { "MaruBuri", "MaruBuri Regular", "마루 부리" }, new[] { "MaruBuri Bold", "마루 부리 Bold", "MaruBuri" }, "마루 부리")) return;
                if (FindFiles("marubur", false, out reg, out bold) && UseFiles(reg, bold, "마루 부리")) return;
                if (UseOS(installed, new[] { "Batang", "바탕" }, new[] { "Batang", "바탕" }, "바탕")) return;
                choice = 2;
            }
            if (choice == 2 && UseOS(installed, new[] { "Malgun Gothic", "맑은 고딕" }, new[] { "Malgun Gothic Bold", "맑은 고딕 Bold", "Malgun Gothic" }, "맑은 고딕")) return;
            Log("쓸 수 있는 한글 글꼴을 찾지 못해 게임 기본 글꼴을 유지합니다.");
        }

        // 메인 메뉴 장면의 고정 문구 번역
        [Hook(HookKind.Postfix, "UI.Screens.StartMenu", "Start", Params = 0, Args = new int[0])]
        public static void TranslateMenu()
        {
            if (!Cfg.TranslateMenu) return;
            try
            {
                int n = 0;
                foreach (Text t in Resources.FindObjectsOfTypeAll<Text>())
                {
                    if (!t || !t.gameObject.scene.IsValid()) continue;
                    string k;
                    if (t.text != null && MenuText.TryGetValue(t.text.Trim(), out k)) { t.text = k; n++; }
                }
                if (n > 0) Log("메인 메뉴 고정 문구 " + n + "곳 번역");
            }
            catch (Exception e) { Log("메뉴 번역 실패: " + e.Message); }
        }
    }
}
