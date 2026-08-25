using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace AllInOne.Logic
{
    public static class Patterns
    {
        public static string[] AdvModules;
        private static string AdvModulesSmali, AdvModulesXml, smaliUrls, smaliExactMatchUrls, activityNames, recservNames, methodNames, idNames;
        public static Dictionary<string, string> linksPattern, linksExactMatchPattern, methodsPatterns, activityPatterns, servicePatterns, receiverPatterns, LayoutPatterns;

        // Compiled regex caches for performance
        public static Dictionary<Regex, string> C_linksPattern, C_linksExactMatchPattern, C_methodsPatterns, C_activityPatterns, C_servicePatterns, C_receiverPatterns, C_LayoutPatterns, C_XmlPatterns, C_ManifestPatterns, C_adsModulesOnly;

        public static void LoadPatterns()
        {
            AdvModules = File.ReadAllLines(Program.pathToMyPluginDir + "\\antiADS\\AdvModules.txt");
            AdvModulesSmali = String.Join("|", AdvModules);
            AdvModulesXml = AdvModulesSmali.Replace("/", "\\.");
            methodNames = String.Join("|", File.ReadAllLines(Program.pathToMyPluginDir + "\\antiADS\\methodNames.txt"));
            smaliUrls = String.Join("|", File.ReadAllLines(Program.pathToMyPluginDir + "\\antiADS\\urls.txt"));
            smaliExactMatchUrls = String.Join("|", File.ReadAllLines(Program.pathToMyPluginDir + "\\antiADS\\urls_exact_match.txt"));
            activityNames = String.Join("|", File.ReadAllLines(Program.pathToMyPluginDir + "\\antiADS\\activityNames.txt"));
            recservNames = String.Join("|", File.ReadAllLines(Program.pathToMyPluginDir + "\\antiADS\\receiverServiceNames.txt"));
            idNames = String.Join("|", File.ReadAllLines(Program.pathToMyPluginDir + "\\antiADS\\idNames.txt"));
            

            linksPattern = new Dictionary<string, string>
            {
                {@"const-string(.+)([pv]\d+), \"(https*:|//).+("+smaliUrls+@").*\"",
                "const-string$1$2, \""+Settings.ReplaceLinksTo+"\""},
                {@"\.field(.+):Ljava/lang/String; = \"(https*:|//).+("+smaliUrls+@").*\"",
                ".field$1:Ljava/lang/String; = \""+Settings.ReplaceLinksTo+"\""}
            };

            linksExactMatchPattern = new Dictionary<string, string>
            {
                {@"const-string(.+)([pv]\d+), \".+("+smaliExactMatchUrls+@").*\"",
                "const-string$1$2, \""+Settings.ReplaceLinksTo+"\""},
                {@"\.field(.+):Ljava/lang/String; = \".+("+smaliExactMatchUrls+@").*\"",
                ".field$1:Ljava/lang/String; = \""+Settings.ReplaceLinksTo+"\""}
            };


            methodsPatterns = new Dictionary<string, string>
            {
                { @"const-string(.+)([pv]\d+), \"ca-app-pub.+?\"",
                "const-string$1$2, \"ca-app-pub-0000000000000000~0000000000\""},
                {@"([ais]*get-object.*Lcom/google/android/gms/(?:internal|ads).*)[\r\n]+\s+invoke-.+Landroid/.*;->addView\([^\)]*\)V",
                "$1"},
                {@"const/(\d+) ([pv]\d+), 0x(4|0)[\r\n]+\s+invoke-virtual \{([pv]\d+), ([pv]\d+)\}, Lcom/google/android/gms/ads/AdView;->setVisibility\(I\)V",
                "const $2, 0x8\n\n    invoke-virtual {$4, $2}, Lcom/google/android/gms/ads/AdView;->setVisibility(I)V"},
                {@"const/*\d* ([pv]\d+), 0x(?:4|0)[\r\n]+\s+(invoke-.+(?:/ads/|/adview|/ad/|Interstitial|banner|" + AdvModulesSmali + @").*;->setVisibility\([^\)]*\))",
                "const $1, 0x8\n\n    $2"},
                {@"invoke-.*(" + AdvModulesSmali + @").*;->(" + methodNames + @")\(.*\)V",
                "invoke-static {}, Lcom/PinkiePie;->DianePie()V"},
                {@"invoke-.*(" + AdvModulesSmali + @").*;->(" + methodNames + @")\(.*\)Z",
                "invoke-static {}, Lcom/PinkiePie;->DianePieNull()Z"},
                {@"invoke-.*Lcom/google/android/gms/ads.*;->a\(Lcom/google/android/gms/ads/AdRequest;.*\)V",
                "invoke-static {}, Lcom/PinkiePie;->DianePie()V"},
                {@"invoke-.+;->(addHtmlAdView|animateAdView|bannerAdmobMainActivity|expandAd|internalLoadAd|loadAd|loadAds|loadBannerAd|loadChildAds|loadInterstitial|loadInterstitialAd|loadNativeA",
                "invoke-static {}, Lcom/PinkiePie;->DianePie()V"},
                {@"invoke-.+Lcom/flurry/.+;->(onStartSession|onEndSession|onEvent|logEvent)\(.+",
                "invoke-static {}, Lcom/PinkiePie;->DianePie()V"},
                {@"invoke-.+Lcom/google/android/gms/(internal|ads).*;->addView\([^\)]*\)V",
                "invoke-static {}, Lcom/PinkiePie;->DianePie()V"},
                {@"invoke-super(.+);->getAdSize\(\)(.+)AdSize;[\r\n]+\s+move-result-object ([pv]\d+)",
                "invoke-super$1;->getAdSize()$2AdSize;\n\n    const $3, 0x0"}
            };

            activityPatterns = new Dictionary<string, string>
            {
                {@"[\r\n]+\s+<activity.*android:name=\".*(" + activityNames + "|" + AdvModulesXml + @").*>(?<!/>)?(\r|\n|.)+?</activity>",
                ""},
                {@"[\r\n]+\s+<activity.*android:name=\".*(" + activityNames + "|" + AdvModulesXml + @").*/>",
                ""}
            };
            servicePatterns = new Dictionary<string, string>
            {
                {@"[\r\n]+\s+<service.*android:name=\".*(" + recservNames + "|" + activityNames + "|" + AdvModulesXml + @").*>(?<!/>)?(\r|\n|.)+?</service>",
                ""},
                {@"[\r\n]+\s+<service.*android:name=\".*(" + recservNames + "|" + activityNames + "|" + AdvModulesXml + @").*/>",
                ""},
                {@"[\r\n\s]+<service[^>]+>(?<!/>)([\r\n\s]+<meta-data[^>]*>)*?[\r\n\s]+<intent-filter>[\r\n\s]+(<(category|data)[^>]*>[\r\n\s]+)*?<action.+android:name=\"[^\"]*(analytics|AppMeas",
                ""}
            };
            receiverPatterns = new Dictionary<string, string>
            {
                {@"[\r\n]+\s+<receiver.*android:name=\".*(" + recservNames + "|" + activityNames + "|" + AdvModulesXml + @").*>(?<!/>)?(\r|\n|.)+?</receiver>",
                ""},
                {@"[\r\n]+\s+<receiver.*android:name=\".*(" + recservNames + "|" + activityNames + "|" + AdvModulesXml + @").*/>",
                ""},
                {@"[\r\n\s]+<receiver[^>]+>(?<!/>)([\r\n\s]+<meta-data[^>]*>)*?[\r\n\s]+<intent-filter>[\r\n\s]+(<(category|data)[^>]*>[\r\n\s]+)*?<action.+android:name=\"[^\"]*(analytics|AppMea",
                ""}
            };

            LayoutPatterns = new Dictionary<string, string>
            {
                {@"(android|n\d+):visibility=\"(?:visible|invisible)\"(.*)((?:android|n\d+):id=\"@id/(?:" + idNames + @")\")",       "$1:visibility=\"gone\"$2$3"},
                {@"((android|n\d+):id=\"@id/(?:" + idNames + @")\")(.*)(android|n\d+):visibility=\"(?:visible|invisible)\"",       "$1$3$4:visibility=\"gone\""},
                {@"(?<!visibility.*)((android|n\d+):id=\"@id/(?:" + idNames + @")\")(?!.*visibility)",                               "$1 $2:visibility=\"gone\""},
                {@"(android|n\d+):layout_(height)=\"[^\"]+\"(.*)((?:android|n\d+):id=\"@id/(?:" + idNames + @")\")",                "$1:layout_$2=\"0.0dip\"$3$4"},
                {@"(android|n\d+):layout_(width)=\"[^\"]+\"(.*)((?:android|n\d+):id=\"@id/(?:" + idNames + @")\")",                 "$1:layout_$2=\"0.0dip\"$3$4"},
                {@"((?:android|n\d+):id=\"@id/(?:" + idNames + @")\")(.*)(android|n\d+):layout_(height)=\"[^\"]+\"",                "$1$2$3:layout_$4=\"0.0dip\""},
                {@"((?:android|n\d+):id=\"@id/(?:" + idNames + @")\")(.*)(android|n\d+):layout_(width)=\"[^\"]+\"",                 "$1$2$3:layout_$4=\"0.0dip\""},
                {@"(<[^\s]*(?:" + AdvModulesXml + @").+)(android|n\d+):layout_(height)=\"[^\"]+\"",                                      "$1$2:layout_$3=\"0.0dip\""},
                {@"(<[^\s]*(?:" + AdvModulesXml + @").+)(android|n\d+):layout_(width)=\"[^\"]+\"",                                       "$1$2:layout_$3=\"0.0dip\""},
                {@"(<[^\s]*(?:" + AdvModulesXml + @")[^\s]*)(?!.*?:visibility)(.*?\s(android|n\d+):\w+=\"[^\"]+\")",                     "$1$2 $3:visibility=\"gone\""},
                {@"(<[^\s]*(?:" + AdvModulesXml + @")[^\s]*)(.*)(android|n\d+):visibility=\"(?:visible|invisible)\"",                     "$1$2$3:visibility=\"gone\""}
            };

            // ... other dictionaries defined below (unchanged) ...

            XmlPatterns = new Dictionary<string, string>
            {
                {@"\"ca-app-pub[^\"]*\"",    "\"ca-app-pub-0000000000000000~0000000000\""},
                {@">ca-app-pub[^<]*</",        ">ca-app-pub-0000000000000000~0000000000</"}
            };

            ManifestPatterns = new Dictionary<string, string>
            {
                {@"\"ca-app-pub[^\"]*\"",
                    "\"ca-app-pub-0000000000000000~0000000000\""},
                {@"<meta-data(.+)android:name=\"com\\.crashlytics\\.ApiKey\"(.+)android:value=\".+\"(.*)/>",
                "<meta-data$1android:name=\"com.crashlytics.ApiKey\"$2android:value=\"Deleted By AllInOne\"$3/>"},
                {@"<meta-data(.+)android:name=\"io\\.fabric\\.ApiKey\"(.+)android:value=\".+\"(.*)/>",
                "<meta-data$1android:name=\"io.fabric.ApiKey\"$2android:value=\"Deleted By AllInOne\"$3/>"},
                {@"<meta-data(.+)android:name=\"com\\.montexi\\.sdk\\.FLURRY_KEY\"(.+)android:value=\".+\"(.*)/>",
                "<meta-data$1android:name=\"com.montexi.sdk.FLURRY_KEY\"$2android:value=\"Deleted By AllInOne\"$3/>"}
            };

            // After building string-based patterns, precompile to Regex for faster repeated use
            PrecompilePatterns();
        }

        private static void PrecompilePatterns()
        {
            // Helper to compile dictionary
            Func<Dictionary<string,string>, Dictionary<Regex,string>> compile = (dict) =>
            {
                var res = new Dictionary<Regex,string>();
                if (dict == null) return res;
                foreach (var kv in dict)
                {
                    try
                    {
                        var rx = new Regex(kv.Key, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
                        res[rx] = kv.Value;
                    }
                    catch
                    {
                        // ignore invalid regex patterns
                    }
                }
                return res;
            };

            C_linksPattern = compile(linksPattern);
            C_linksExactMatchPattern = compile(linksExactMatchPattern);
            C_methodsPatterns = compile(methodsPatterns);
            C_activityPatterns = compile(activityPatterns);
            C_servicePatterns = compile(servicePatterns);
            C_receiverPatterns = compile(receiverPatterns);
            C_LayoutPatterns = compile(LayoutPatterns);
            C_XmlPatterns = compile(XmlPatterns);
            C_ManifestPatterns = compile(ManifestPatterns);
            C_adsModulesOnly = compile(adsModulesOnly);
        }

        // rest of file unchanged - keep previous dictionaries and definitions
    }
}
