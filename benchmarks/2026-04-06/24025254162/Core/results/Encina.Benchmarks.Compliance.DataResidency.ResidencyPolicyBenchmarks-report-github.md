```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                               | Mean | Error | Ratio | RatioSD | Rank | Alloc Ratio |
|------------------------------------- |-----:|------:|------:|--------:|-----:|------------:|
| &#39;Region lookup (baseline)&#39;           |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Policy: IsAllowed (allowed region)&#39; |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Policy: IsAllowed (blocked region)&#39; |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Policy: GetAllowedRegions&#39;          |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Transfer: intra-EEA (DE→FR)&#39;        |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Transfer: third country (DE→US)&#39;    |   NA |    NA |     ? |       ? |    ? |           ? |

Benchmarks with issues:
  ResidencyPolicyBenchmarks.'Region lookup (baseline)': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  ResidencyPolicyBenchmarks.'Policy: IsAllowed (allowed region)': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  ResidencyPolicyBenchmarks.'Policy: IsAllowed (blocked region)': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  ResidencyPolicyBenchmarks.'Policy: GetAllowedRegions': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  ResidencyPolicyBenchmarks.'Transfer: intra-EEA (DE→FR)': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  ResidencyPolicyBenchmarks.'Transfer: third country (DE→US)': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
