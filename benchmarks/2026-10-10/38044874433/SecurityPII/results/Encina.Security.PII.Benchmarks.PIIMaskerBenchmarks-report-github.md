```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-----:|------:|------:|--------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
|                          |            |                |             |      |       |       |         |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| Mask_CreditCard          | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| Mask_Email               | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_MultiField    | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| Mask_Phone               | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  PIIMaskerBenchmarks.Mask_SSN: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PIIMaskerBenchmarks.Mask_WithRegexPattern: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PIIMaskerBenchmarks.MaskObject_NoAttributes: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PIIMaskerBenchmarks.MaskForAudit_SingleField: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PIIMaskerBenchmarks.MaskForAudit_NonGeneric: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PIIMaskerBenchmarks.Mask_CreditCard: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PIIMaskerBenchmarks.Mask_Email: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PIIMaskerBenchmarks.MaskObject_MultiField: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PIIMaskerBenchmarks.MaskObject_SingleField: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PIIMaskerBenchmarks.Mask_Phone: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  PIIMaskerBenchmarks.Mask_SSN: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  PIIMaskerBenchmarks.Mask_WithRegexPattern: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  PIIMaskerBenchmarks.MaskObject_NoAttributes: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  PIIMaskerBenchmarks.MaskForAudit_SingleField: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  PIIMaskerBenchmarks.MaskForAudit_NonGeneric: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  PIIMaskerBenchmarks.Mask_CreditCard: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  PIIMaskerBenchmarks.Mask_Email: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  PIIMaskerBenchmarks.MaskObject_MultiField: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  PIIMaskerBenchmarks.MaskObject_SingleField: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  PIIMaskerBenchmarks.Mask_Phone: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
