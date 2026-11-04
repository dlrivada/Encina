```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.736 μs | 0.1486 μs | 0.0081 μs |  0.78 |    0.07 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.562 μs | 2.8027 μs | 0.1536 μs |  1.01 |    0.10 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 3.549 μs | 6.3605 μs | 0.3486 μs |  1.01 |    0.12 | 0.0420 | 0.0381 |     712 B |        1.00 |
