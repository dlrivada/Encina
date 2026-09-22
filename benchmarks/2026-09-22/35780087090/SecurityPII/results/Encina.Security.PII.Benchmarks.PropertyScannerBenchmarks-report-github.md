```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |-----:|------:|------:|--------:|------------:|
| MaskObject_SingleField_WarmCache     | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| MaskObject_MultiField_WarmCache      | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| MaskObject_NoAttributes_WarmCache    | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| MaskObject_MixedAttributes_WarmCache | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| MaskForAudit_SingleField             | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| MaskForAudit_NonGeneric              | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
|                                      |            |                |             |      |       |       |         |             |
| MaskObject_SingleField_WarmCache     | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_MultiField_WarmCache      | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_NoAttributes_WarmCache    | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_MixedAttributes_WarmCache | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| MaskForAudit_SingleField             | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| MaskForAudit_NonGeneric              | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  PropertyScannerBenchmarks.MaskObject_SingleField_WarmCache: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PropertyScannerBenchmarks.MaskObject_MultiField_WarmCache: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PropertyScannerBenchmarks.MaskObject_NoAttributes_WarmCache: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PropertyScannerBenchmarks.MaskObject_MixedAttributes_WarmCache: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PropertyScannerBenchmarks.MaskForAudit_SingleField: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PropertyScannerBenchmarks.MaskForAudit_NonGeneric: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PropertyScannerBenchmarks.MaskObject_SingleField_WarmCache: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  PropertyScannerBenchmarks.MaskObject_MultiField_WarmCache: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  PropertyScannerBenchmarks.MaskObject_NoAttributes_WarmCache: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  PropertyScannerBenchmarks.MaskObject_MixedAttributes_WarmCache: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  PropertyScannerBenchmarks.MaskForAudit_SingleField: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  PropertyScannerBenchmarks.MaskForAudit_NonGeneric: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
