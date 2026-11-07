```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.780 μs | 0.2757 μs | 0.0151 μs |  0.92 |    0.01 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 8.805 μs | 1.4900 μs | 0.0817 μs |  2.15 |    0.02 | 0.1831 | 0.1678 |   3.55 KB |        3.42 |
| SetAsync                      | 4.100 μs | 0.6406 μs | 0.0351 μs |  1.00 |    0.01 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.159 μs | 0.2526 μs | 0.0138 μs |  1.01 |    0.01 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
