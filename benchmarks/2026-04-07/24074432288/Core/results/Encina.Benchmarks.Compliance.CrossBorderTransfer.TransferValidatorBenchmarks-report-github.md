```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.324 μs | 0.0874 μs | 0.1282 μs |  1.00 |    0.08 |    1 | 0.0515 | 0.0248 |     912 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.647 μs | 0.2454 μs | 0.3673 μs |  2.00 |    0.19 |    2 | 0.0992 | 0.0458 |    1760 B |        1.93 |
| &#39;Validate: SCC agreement&#39;                 | 8.593 μs | 0.3095 μs | 0.4536 μs |  3.71 |    0.27 |    5 | 0.1450 | 0.0687 |    2528 B |        2.77 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.768 μs | 0.1981 μs | 0.2903 μs |  3.35 |    0.22 |    4 | 0.1678 | 0.0763 |    2848 B |        3.12 |
| &#39;Validate: block (full cascade)&#39;          | 6.606 μs | 0.2867 μs | 0.4291 μs |  2.85 |    0.24 |    3 | 0.1678 | 0.0839 |    2832 B |        3.11 |
