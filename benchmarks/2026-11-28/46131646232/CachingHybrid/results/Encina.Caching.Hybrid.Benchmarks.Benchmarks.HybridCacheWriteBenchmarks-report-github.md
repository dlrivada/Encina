```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.662 μs | 0.3680 μs | 0.0202 μs |  0.72 |    0.01 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 7.923 μs | 6.7879 μs | 0.3721 μs |  2.14 |    0.10 | 0.1373 | 0.1221 |   2.39 KB |        2.30 |
| SetAsync                      | 3.704 μs | 1.4560 μs | 0.0798 μs |  1.00 |    0.03 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.832 μs | 4.0936 μs | 0.2244 μs |  1.03 |    0.06 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
