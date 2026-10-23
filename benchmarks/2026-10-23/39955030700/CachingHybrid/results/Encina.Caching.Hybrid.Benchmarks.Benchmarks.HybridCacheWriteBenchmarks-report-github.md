```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.501 μs | 0.4169 μs | 0.0229 μs |  0.78 |    0.01 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 7.011 μs | 9.8602 μs | 0.5405 μs |  2.18 |    0.15 | 0.1450 | 0.1373 |   2.39 KB |        2.30 |
| SetAsync                      | 3.213 μs | 0.2242 μs | 0.0123 μs |  1.00 |    0.00 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.292 μs | 1.6765 μs | 0.0919 μs |  1.02 |    0.02 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
