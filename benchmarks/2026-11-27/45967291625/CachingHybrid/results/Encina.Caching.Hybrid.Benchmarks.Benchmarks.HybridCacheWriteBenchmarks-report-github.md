```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.55GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.790 μs | 1.210 μs | 0.0663 μs |  0.87 |    0.03 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 8.699 μs | 1.255 μs | 0.0688 μs |  2.01 |    0.06 | 0.1678 | 0.1526 |   2.97 KB |        2.86 |
| SetAsync                      | 4.340 μs | 2.501 μs | 0.1371 μs |  1.00 |    0.04 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.263 μs | 1.193 μs | 0.0654 μs |  0.98 |    0.03 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
