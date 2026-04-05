```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                        | Mean | Error | Ratio | RatioSD | Rank | Alloc Ratio |
|---------------------------------------------- |-----:|------:|------:|--------:|-----:|------------:|
| &#39;Pipeline — Block mode (valid DPA, allowed)&#39;  |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline — Block mode (no DPA, blocked)&#39;     |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline — Warn mode (logs, proceeds)&#39;       |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline — Disabled mode (skip all)&#39;         |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline — No attribute (cache lookup only)&#39; |   NA |    NA |     ? |       ? |    ? |           ? |

Benchmarks with issues:
  ProcessorAgreementPipelineBenchmarks.'Pipeline — Block mode (valid DPA, allowed)': Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
  ProcessorAgreementPipelineBenchmarks.'Pipeline — Block mode (no DPA, blocked)': Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
  ProcessorAgreementPipelineBenchmarks.'Pipeline — Warn mode (logs, proceeds)': Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
  ProcessorAgreementPipelineBenchmarks.'Pipeline — Disabled mode (skip all)': Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
  ProcessorAgreementPipelineBenchmarks.'Pipeline — No attribute (cache lookup only)': Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
