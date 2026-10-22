```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 1.904 μs | 0.0121 μs | 0.0174 μs |  0.56 |    0.03 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.679 μs | 0.2330 μs | 0.3487 μs |  1.09 |    0.12 | 0.0496 | 0.0458 |     858 B |        1.21 |
| SetAsync                      | 3.386 μs | 0.1339 μs | 0.1833 μs |  1.00 |    0.07 | 0.0420 | 0.0381 |     712 B |        1.00 |
