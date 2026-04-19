```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

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
  ResidencyPolicyBenchmarks.'Region lookup (baseline)': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  ResidencyPolicyBenchmarks.'Policy: IsAllowed (allowed region)': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  ResidencyPolicyBenchmarks.'Policy: IsAllowed (blocked region)': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  ResidencyPolicyBenchmarks.'Policy: GetAllowedRegions': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  ResidencyPolicyBenchmarks.'Transfer: intra-EEA (DE→FR)': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  ResidencyPolicyBenchmarks.'Transfer: third country (DE→US)': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
