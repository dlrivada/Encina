```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.097 μs | 0.4830 μs | 0.0265 μs |  0.64 |    0.05 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.409 μs | 3.3224 μs | 0.1821 μs |  1.04 |    0.09 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 3.310 μs | 5.2081 μs | 0.2855 μs |  1.01 |    0.11 | 0.0420 | 0.0381 |     712 B |        1.00 |
