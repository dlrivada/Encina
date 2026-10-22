```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 4.780 μs | 0.7613 μs | 0.0417 μs |  1.04 |    0.02 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 9.987 μs | 1.6309 μs | 0.0894 μs |  2.17 |    0.05 | 0.1373 | 0.1221 |   2.39 KB |        2.30 |
| SetAsync                      | 4.610 μs | 2.1253 μs | 0.1165 μs |  1.00 |    0.03 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.593 μs | 3.1581 μs | 0.1731 μs |  1.00 |    0.04 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
