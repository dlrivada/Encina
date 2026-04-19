```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.002 μs | 0.0202 μs | 0.0303 μs |  1.00 |    0.02 |    1 | 0.0515 | 0.0248 |     912 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 3.719 μs | 0.0616 μs | 0.0903 μs |  1.86 |    0.05 |    2 | 0.1030 | 0.0496 |    1888 B |        2.07 |
| &#39;Validate: SCC agreement&#39;                 | 6.736 μs | 0.1193 μs | 0.1786 μs |  3.37 |    0.10 |    4 | 0.1450 | 0.0687 |    2528 B |        2.77 |
| &#39;Validate: TIA (deep cascade)&#39;            | 6.677 μs | 0.0696 μs | 0.0953 μs |  3.34 |    0.07 |    4 | 0.1678 | 0.0839 |    2848 B |        3.12 |
| &#39;Validate: block (full cascade)&#39;          | 5.425 μs | 0.0400 μs | 0.0586 μs |  2.71 |    0.05 |    3 | 0.1678 | 0.0839 |    2832 B |        3.11 |
