# SamplePage strings - paste these keys into Services/Localization/EnStrings.cs,
# EsStrings.cs and FrStrings.cs (match the ## headers to en-US/es-ES/fr-FR),
# then delete this file. The page XAML binds live ({loc:Loc}), so until you
# paste, it shows the key names.
#
# The generated Strings_AreTranslated test fails until every language has the
# keys. es/fr ship as TODO-translate markers: the app runs, but replace them
# with real translations before release (grep TODO-translate to find them).
#
# Tab headers and tab content live in the ViewModel as placeholder literals
# (dynamic tabs cannot bind loc keys): replace LoadSampleTabs with your data
# source. Only the page chrome below needs dictionary keys.

## en-US
["NavSample"] = "PageTitleFallback",
["SampleTitle"] = "PageTitleFallback",
["SampleDescription"] = "PageTitleFallback page.",

## es-ES
["NavSample"] = "TODO-translate: PageTitleFallback",
["SampleTitle"] = "TODO-translate: PageTitleFallback",
["SampleDescription"] = "TODO-translate: PageTitleFallback page.",

## fr-FR
["NavSample"] = "TODO-translate: PageTitleFallback",
["SampleTitle"] = "TODO-translate: PageTitleFallback",
["SampleDescription"] = "TODO-translate: PageTitleFallback page.",
