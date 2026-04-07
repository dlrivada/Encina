```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                               | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |-----:|------:|------:|--------:|------------:|
| MaskObject_SingleField_WarmCache     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_MultiField_WarmCache      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_NoAttributes_WarmCache    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_MixedAttributes_WarmCache | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |   NA |    NA |     ? |       ? |           ? |
| MaskForAudit_SingleField             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |   NA |    NA |     ? |       ? |           ? |
| MaskForAudit_NonGeneric              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |   NA |    NA |     ? |       ? |           ? |
|                                      |            |                |             |             |              |             |      |       |       |         |             |
| MaskObject_SingleField_WarmCache     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_MultiField_WarmCache      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_NoAttributes_WarmCache    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_MixedAttributes_WarmCache | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   NA |    NA |     ? |       ? |           ? |
| MaskForAudit_SingleField             | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   NA |    NA |     ? |       ? |           ? |
| MaskForAudit_NonGeneric              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  PropertyScannerBenchmarks.MaskObject_SingleField_WarmCache: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PropertyScannerBenchmarks.MaskObject_MultiField_WarmCache: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PropertyScannerBenchmarks.MaskObject_NoAttributes_WarmCache: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PropertyScannerBenchmarks.MaskObject_MixedAttributes_WarmCache: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PropertyScannerBenchmarks.MaskForAudit_SingleField: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PropertyScannerBenchmarks.MaskForAudit_NonGeneric: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PropertyScannerBenchmarks.MaskObject_SingleField_WarmCache: Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
  PropertyScannerBenchmarks.MaskObject_MultiField_WarmCache: Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
  PropertyScannerBenchmarks.MaskObject_NoAttributes_WarmCache: Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
  PropertyScannerBenchmarks.MaskObject_MixedAttributes_WarmCache: Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
  PropertyScannerBenchmarks.MaskForAudit_SingleField: Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
  PropertyScannerBenchmarks.MaskForAudit_NonGeneric: Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
