```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.07GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.849 μs | 0.5333 μs | 0.0292 μs |  0.90 |    0.04 | 0.0114 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 6.468 μs | 6.2192 μs | 0.3409 μs |  2.05 |    0.12 | 0.0229 | 0.0153 |   2.39 KB |        2.30 |
| SetAsync                      | 3.160 μs | 2.6069 μs | 0.1429 μs |  1.00 |    0.05 | 0.0114 | 0.0076 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.096 μs | 2.3331 μs | 0.1279 μs |  0.98 |    0.05 | 0.0114 | 0.0076 |   1.05 KB |        1.01 |
