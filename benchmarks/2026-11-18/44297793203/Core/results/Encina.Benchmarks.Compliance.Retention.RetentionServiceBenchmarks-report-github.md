```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2.607 μs | 1.7705 μs | 0.0970 μs |  1.00 |    0.05 |    3 | 0.0114 | 0.0076 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.361 μs | 0.0701 μs | 0.0038 μs |  0.52 |    0.02 |    1 | 0.0076 | 0.0057 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 2.063 μs | 0.3377 μs | 0.0185 μs |  0.79 |    0.03 |    2 | 0.0076 | 0.0038 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1.867 μs | 0.0988 μs | 0.0054 μs |  0.72 |    0.02 |    2 | 0.0076 | 0.0057 |     680 B |        0.54 |
