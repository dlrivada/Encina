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
| &#39;Validate: adequacy decision (fast path)&#39; | 2.117 μs | 0.0443 μs | 0.0663 μs |  1.00 |    0.04 |    1 | 0.0515 | 0.0248 |     912 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.064 μs | 0.1235 μs | 0.1810 μs |  1.92 |    0.10 |    2 | 0.1030 | 0.0496 |    1888 B |        2.07 |
| &#39;Validate: SCC agreement&#39;                 | 9.013 μs | 0.7497 μs | 1.0989 μs |  4.26 |    0.53 |    5 | 0.1373 | 0.0610 |    2528 B |        2.77 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.336 μs | 0.1080 μs | 0.1514 μs |  3.47 |    0.13 |    4 | 0.1678 | 0.0839 |    2848 B |        3.12 |
| &#39;Validate: block (full cascade)&#39;          | 5.678 μs | 0.0430 μs | 0.0588 μs |  2.69 |    0.09 |    3 | 0.1678 | 0.0839 |    2832 B |        3.11 |
