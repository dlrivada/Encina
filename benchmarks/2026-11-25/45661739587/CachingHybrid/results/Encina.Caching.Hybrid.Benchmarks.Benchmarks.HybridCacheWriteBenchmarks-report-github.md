```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.733 μs | 2.129 μs | 0.1167 μs |  0.93 |    0.04 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 8.572 μs | 2.532 μs | 0.1388 μs |  2.14 |    0.08 | 0.1678 | 0.1526 |   2.96 KB |        2.85 |
| SetAsync                      | 4.019 μs | 2.895 μs | 0.1587 μs |  1.00 |    0.05 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.305 μs | 2.782 μs | 0.1525 μs |  1.07 |    0.05 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
