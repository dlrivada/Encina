```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.626 μs | 0.9727 μs | 0.0533 μs |  0.89 |    0.03 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 8.512 μs | 1.0822 μs | 0.0593 μs |  2.08 |    0.06 | 0.1831 | 0.1678 |   3.55 KB |        3.42 |
| SetAsync                      | 4.099 μs | 2.6788 μs | 0.1468 μs |  1.00 |    0.04 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.161 μs | 2.5989 μs | 0.1425 μs |  1.02 |    0.04 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
