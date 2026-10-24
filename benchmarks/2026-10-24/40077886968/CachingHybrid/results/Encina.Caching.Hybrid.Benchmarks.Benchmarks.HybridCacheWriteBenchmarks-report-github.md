```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   |  4.985 μs | 1.1198 μs | 0.0614 μs |  1.12 |    0.03 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 10.227 μs | 5.3655 μs | 0.2941 μs |  2.29 |    0.08 | 0.1373 | 0.1221 |   2.39 KB |        2.30 |
| SetAsync                      |  4.466 μs | 2.2663 μs | 0.1242 μs |  1.00 |    0.03 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.159 μs | 0.7074 μs | 0.0388 μs |  0.93 |    0.02 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
