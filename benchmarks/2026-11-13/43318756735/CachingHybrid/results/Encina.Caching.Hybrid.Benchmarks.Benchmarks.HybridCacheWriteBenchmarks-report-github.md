```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.429 μs | 0.3975 μs | 0.0218 μs |  0.81 |    0.02 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 6.192 μs | 1.8393 μs | 0.1008 μs |  2.05 |    0.05 | 0.1450 | 0.1373 |   2.39 KB |        2.30 |
| SetAsync                      | 3.017 μs | 1.3334 μs | 0.0731 μs |  1.00 |    0.03 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 2.977 μs | 1.0628 μs | 0.0583 μs |  0.99 |    0.03 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
