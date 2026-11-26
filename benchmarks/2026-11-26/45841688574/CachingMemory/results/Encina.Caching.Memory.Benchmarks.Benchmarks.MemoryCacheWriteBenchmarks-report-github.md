```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 1.837 μs | 1.540 μs | 0.0844 μs |  0.65 |    0.03 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 2.605 μs | 3.954 μs | 0.2167 μs |  0.92 |    0.07 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 2.828 μs | 1.919 μs | 0.1052 μs |  1.00 |    0.05 | 0.0420 | 0.0381 |     712 B |        1.00 |
