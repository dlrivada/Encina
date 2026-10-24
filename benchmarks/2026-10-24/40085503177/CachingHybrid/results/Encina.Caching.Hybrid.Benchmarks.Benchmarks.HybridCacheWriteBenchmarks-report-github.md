```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.97GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 4.782 μs | 1.607 μs | 0.0881 μs |  1.10 |    0.04 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 9.748 μs | 2.505 μs | 0.1373 μs |  2.25 |    0.08 | 0.1831 | 0.1678 |   3.56 KB |        3.43 |
| SetAsync                      | 4.344 μs | 3.011 μs | 0.1651 μs |  1.00 |    0.05 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.250 μs | 1.669 μs | 0.0915 μs |  0.98 |    0.04 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
