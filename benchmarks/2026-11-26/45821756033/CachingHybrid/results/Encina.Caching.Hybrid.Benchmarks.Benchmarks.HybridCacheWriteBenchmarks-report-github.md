```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.677 μs | 0.4630 μs | 0.0254 μs |  0.78 |    0.03 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 7.253 μs | 2.4046 μs | 0.1318 μs |  2.11 |    0.08 | 0.1831 | 0.1678 |   3.56 KB |        3.43 |
| SetAsync                      | 3.442 μs | 2.4734 μs | 0.1356 μs |  1.00 |    0.05 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.397 μs | 1.0413 μs | 0.0571 μs |  0.99 |    0.04 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
