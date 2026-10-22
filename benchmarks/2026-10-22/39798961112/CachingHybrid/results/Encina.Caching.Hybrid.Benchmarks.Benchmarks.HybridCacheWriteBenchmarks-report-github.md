```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.729 μs | 0.0494 μs | 0.0739 μs |  0.90 |    0.03 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 8.317 μs | 0.1824 μs | 0.2617 μs |  2.00 |    0.08 | 0.1373 | 0.1221 |   2.39 KB |        2.30 |
| SetAsync                      | 4.156 μs | 0.0676 μs | 0.0970 μs |  1.00 |    0.03 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.076 μs | 0.0854 μs | 0.1110 μs |  0.98 |    0.03 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
