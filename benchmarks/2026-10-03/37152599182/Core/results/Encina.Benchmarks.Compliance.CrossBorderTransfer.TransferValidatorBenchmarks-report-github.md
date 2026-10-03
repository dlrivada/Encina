```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 1.771 μs | 0.6610 μs | 0.0362 μs |  1.00 |    0.03 |    1 | 0.0114 | 0.0095 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 2.962 μs | 1.6720 μs | 0.0916 μs |  1.67 |    0.05 |    2 | 0.0191 | 0.0153 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 5.074 μs | 2.4888 μs | 0.1364 μs |  2.87 |    0.08 |    3 | 0.0305 | 0.0229 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 5.742 μs | 2.4390 μs | 0.1337 μs |  3.24 |    0.09 |    3 | 0.0305 | 0.0229 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 5.016 μs | 0.7672 μs | 0.0421 μs |  2.83 |    0.05 |    3 | 0.0305 | 0.0229 |    2944 B |        2.97 |
