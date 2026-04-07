```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

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
  ResidencyPolicyBenchmarks.'Region lookup (baseline)': Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
  ResidencyPolicyBenchmarks.'Policy: IsAllowed (allowed region)': Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
  ResidencyPolicyBenchmarks.'Policy: IsAllowed (blocked region)': Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
  ResidencyPolicyBenchmarks.'Policy: GetAllowedRegions': Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
  ResidencyPolicyBenchmarks.'Transfer: intra-EEA (DE→FR)': Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
  ResidencyPolicyBenchmarks.'Transfer: third country (DE→US)': Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
