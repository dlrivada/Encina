```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.439 μs | 0.3456 μs | 0.0189 μs |  0.93 |    0.03 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 4.119 μs | 3.1568 μs | 0.1730 μs |  1.12 |    0.05 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 3.684 μs | 2.1791 μs | 0.1194 μs |  1.00 |    0.04 | 0.0381 | 0.0305 |     712 B |        1.00 |
