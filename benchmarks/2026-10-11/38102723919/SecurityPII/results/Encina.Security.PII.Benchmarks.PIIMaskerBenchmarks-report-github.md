```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |-----:|------:|------:|--------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| Mask_Email               | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
|                          |            |                |             |             |      |       |       |         |             |
| Mask_SSN                 | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| Mask_WithRegexPattern    | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| MaskObject_NoAttributes  | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| MaskForAudit_SingleField | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| MaskForAudit_NonGeneric  | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| Mask_CreditCard          | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| Mask_Email               | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| MaskObject_MultiField    | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| MaskObject_SingleField   | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| Mask_Phone               | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |

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
  PIIMaskerBenchmarks.Mask_SSN: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  PIIMaskerBenchmarks.Mask_WithRegexPattern: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  PIIMaskerBenchmarks.MaskObject_NoAttributes: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  PIIMaskerBenchmarks.MaskForAudit_SingleField: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  PIIMaskerBenchmarks.MaskForAudit_NonGeneric: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  PIIMaskerBenchmarks.Mask_CreditCard: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  PIIMaskerBenchmarks.Mask_Email: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  PIIMaskerBenchmarks.MaskObject_MultiField: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  PIIMaskerBenchmarks.MaskObject_SingleField: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  PIIMaskerBenchmarks.Mask_Phone: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
