```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                    | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|------:|------:|-----:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 37.24 ms |    NA |  1.00 |    1 |     880 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 46.51 ms |    NA |  1.25 |    2 |    1760 B |        2.00 |
| &#39;Validate: SCC agreement&#39;                 | 52.35 ms |    NA |  1.41 |    3 |    2528 B |        2.87 |
| &#39;Validate: TIA (deep cascade)&#39;            | 56.68 ms |    NA |  1.52 |    5 |    2848 B |        3.24 |
| &#39;Validate: block (full cascade)&#39;          | 53.75 ms |    NA |  1.44 |    4 |    2832 B |        3.22 |
