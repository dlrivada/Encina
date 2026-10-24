```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.816 μs | 0.6002 μs | 0.0329 μs |  0.96 |    0.01 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 8.500 μs | 2.7789 μs | 0.1523 μs |  2.13 |    0.04 | 0.1678 | 0.1526 |   2.98 KB |        2.87 |
| SetAsync                      | 3.991 μs | 0.8511 μs | 0.0466 μs |  1.00 |    0.01 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.032 μs | 0.2139 μs | 0.0117 μs |  1.01 |    0.01 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
