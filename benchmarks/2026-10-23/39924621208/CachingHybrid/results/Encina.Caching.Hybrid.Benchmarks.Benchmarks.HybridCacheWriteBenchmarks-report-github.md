```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.683 μs | 0.4350 μs | 0.0238 μs |  0.98 |    0.01 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 7.669 μs | 1.1304 μs | 0.0620 μs |  2.05 |    0.02 | 0.1678 | 0.1526 |   2.97 KB |        2.86 |
| SetAsync                      | 3.749 μs | 0.3163 μs | 0.0173 μs |  1.00 |    0.01 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.723 μs | 0.3718 μs | 0.0204 μs |  0.99 |    0.01 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
