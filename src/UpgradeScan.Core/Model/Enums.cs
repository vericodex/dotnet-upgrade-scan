namespace UpgradeScan.Core.Model;

public enum ProjectStyle { SdkStyle, Legacy, Unknown }

public enum AnalysisTier { Semantic, Syntactic, Manifest }

public enum DiagnosticSeverity { Info, Warning, Error }

public enum ProjectType { Unknown, Library, Console, WinForms, Wpf, AspNetMvc, AspNetWebForms, WcfService, WcfClient }

public enum PackageVerdict { Compatible, Replace, Partial, Incompatible, Deprecated }

public enum FindingSeverity { Info, Warning, Blocker }

public enum EffortBand { S, M, L, XL }
