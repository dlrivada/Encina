```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.982 μs | 1.112 μs | 0.0610 μs |  1.03 |    0.06 | 0.0305 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.345 μs | 5.463 μs | 0.2994 μs |  1.16 |    0.11 | 0.0267 | 0.0229 |     720 B |        1.01 |
| SetAsync                      | 2.905 μs | 3.609 μs | 0.1978 μs |  1.00 |    0.08 | 0.0229 | 0.0153 |     712 B |        1.00 |
