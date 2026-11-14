```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 1.986 μs | 0.8112 μs | 0.0445 μs |  0.69 |    0.06 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 2.845 μs | 4.2930 μs | 0.2353 μs |  0.99 |    0.11 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 2.884 μs | 4.6152 μs | 0.2530 μs |  1.01 |    0.11 | 0.0420 | 0.0381 |     712 B |        1.00 |
