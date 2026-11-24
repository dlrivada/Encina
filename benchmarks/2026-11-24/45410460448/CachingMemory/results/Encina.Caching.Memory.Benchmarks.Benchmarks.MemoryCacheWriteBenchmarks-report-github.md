```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.466 μs | 0.0137 μs | 0.0007 μs |  0.91 |    0.06 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.780 μs | 4.5520 μs | 0.2495 μs |  0.99 |    0.09 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 3.847 μs | 5.6054 μs | 0.3073 μs |  1.00 |    0.10 | 0.0420 | 0.0381 |     712 B |        1.00 |
