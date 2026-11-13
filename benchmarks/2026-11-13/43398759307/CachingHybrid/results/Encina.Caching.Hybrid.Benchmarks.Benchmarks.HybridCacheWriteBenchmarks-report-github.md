```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.766 μs | 0.4681 μs | 0.0257 μs |  0.94 |    0.03 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 8.586 μs | 2.8425 μs | 0.1558 μs |  2.13 |    0.07 | 0.1678 | 0.1526 |   2.98 KB |        2.86 |
| SetAsync                      | 4.026 μs | 2.2944 μs | 0.1258 μs |  1.00 |    0.04 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.165 μs | 1.4345 μs | 0.0786 μs |  1.04 |    0.03 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
