```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.14GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 5.090 μs | 0.3941 μs | 0.0216 μs |  1.13 |    0.04 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 9.937 μs | 5.9813 μs | 0.3279 μs |  2.21 |    0.11 | 0.1678 | 0.1526 |   2.98 KB |        2.86 |
| SetAsync                      | 4.506 μs | 3.7960 μs | 0.2081 μs |  1.00 |    0.06 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.385 μs | 0.8238 μs | 0.0452 μs |  0.97 |    0.04 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
