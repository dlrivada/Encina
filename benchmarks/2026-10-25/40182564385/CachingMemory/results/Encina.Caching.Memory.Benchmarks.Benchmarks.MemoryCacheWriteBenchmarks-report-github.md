```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.408 μs | 0.5507 μs | 0.0302 μs |  0.94 |    0.07 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.331 μs | 2.2582 μs | 0.1238 μs |  0.92 |    0.07 | 0.0381 | 0.0305 |     720 B |        1.01 |
| SetAsync                      | 3.639 μs | 5.4447 μs | 0.2984 μs |  1.00 |    0.10 | 0.0420 | 0.0381 |     712 B |        1.00 |
