```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2.821 μs | 0.3454 μs | 0.0189 μs |  1.00 |    3 | 0.0496 | 0.0229 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.358 μs | 0.3833 μs | 0.0210 μs |  0.48 |    1 | 0.0248 | 0.0114 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 2.228 μs | 0.2958 μs | 0.0162 μs |  0.79 |    2 | 0.0343 | 0.0305 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1.924 μs | 0.5863 μs | 0.0321 μs |  0.68 |    2 | 0.0267 | 0.0229 |     680 B |        0.54 |
