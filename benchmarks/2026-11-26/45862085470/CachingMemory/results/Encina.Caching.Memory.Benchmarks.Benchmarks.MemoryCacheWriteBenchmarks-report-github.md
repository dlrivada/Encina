```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 1.816 μs | 0.1390 μs | 0.0076 μs |  0.59 |    0.04 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.015 μs | 2.8156 μs | 0.1543 μs |  0.98 |    0.07 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 3.091 μs | 4.2101 μs | 0.2308 μs |  1.00 |    0.09 | 0.0420 | 0.0381 |     712 B |        1.00 |
