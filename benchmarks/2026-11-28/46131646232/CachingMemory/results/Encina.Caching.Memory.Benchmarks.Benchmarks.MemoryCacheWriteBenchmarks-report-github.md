```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.711 μs | 0.1168 μs | 0.0064 μs |  0.67 |    0.06 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.074 μs | 4.6687 μs | 0.2559 μs |  0.76 |    0.09 | 0.0381 | 0.0305 |     720 B |        1.01 |
| SetAsync                      | 4.065 μs | 7.9084 μs | 0.4335 μs |  1.01 |    0.13 | 0.0420 | 0.0381 |     712 B |        1.00 |
