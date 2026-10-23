```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2.494 μs | 2.0424 μs | 0.1119 μs |  1.00 |    0.06 |    3 | 0.0114 | 0.0076 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.324 μs | 0.0853 μs | 0.0047 μs |  0.53 |    0.02 |    1 | 0.0076 | 0.0057 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 1.972 μs | 0.1431 μs | 0.0078 μs |  0.79 |    0.03 |    2 | 0.0076 | 0.0038 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1.775 μs | 0.3570 μs | 0.0196 μs |  0.71 |    0.03 |    2 | 0.0076 | 0.0057 |     680 B |        0.54 |
