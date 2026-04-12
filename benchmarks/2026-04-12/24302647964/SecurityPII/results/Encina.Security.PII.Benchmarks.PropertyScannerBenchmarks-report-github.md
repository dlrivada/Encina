```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 3.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                               | Job        | IterationCount | LaunchCount | WarmupCount | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------ |-----:|------:|------:|--------:|------------:|
| MaskObject_SingleField_WarmCache     | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_MultiField_WarmCache      | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_NoAttributes_WarmCache    | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_MixedAttributes_WarmCache | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| MaskForAudit_SingleField             | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| MaskForAudit_NonGeneric              | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
|                                      |            |                |             |             |      |       |       |         |             |
| MaskObject_SingleField_WarmCache     | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| MaskObject_MultiField_WarmCache      | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| MaskObject_NoAttributes_WarmCache    | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| MaskObject_MixedAttributes_WarmCache | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| MaskForAudit_SingleField             | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| MaskForAudit_NonGeneric              | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  PropertyScannerBenchmarks.MaskObject_SingleField_WarmCache: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PropertyScannerBenchmarks.MaskObject_MultiField_WarmCache: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PropertyScannerBenchmarks.MaskObject_NoAttributes_WarmCache: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PropertyScannerBenchmarks.MaskObject_MixedAttributes_WarmCache: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PropertyScannerBenchmarks.MaskForAudit_SingleField: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PropertyScannerBenchmarks.MaskForAudit_NonGeneric: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PropertyScannerBenchmarks.MaskObject_SingleField_WarmCache: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  PropertyScannerBenchmarks.MaskObject_MultiField_WarmCache: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  PropertyScannerBenchmarks.MaskObject_NoAttributes_WarmCache: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  PropertyScannerBenchmarks.MaskObject_MixedAttributes_WarmCache: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  PropertyScannerBenchmarks.MaskForAudit_SingleField: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  PropertyScannerBenchmarks.MaskForAudit_NonGeneric: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
