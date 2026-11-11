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
| RemoveAsync                   | 3.865 μs | 0.2889 μs | 0.0158 μs |  0.91 |    0.01 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 8.380 μs | 2.9616 μs | 0.1623 μs |  1.98 |    0.04 | 0.1678 | 0.1526 |   2.97 KB |        2.86 |
| SetAsync                      | 4.233 μs | 0.6757 μs | 0.0370 μs |  1.00 |    0.01 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.163 μs | 0.4219 μs | 0.0231 μs |  0.98 |    0.01 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
