```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.325 μs | 0.6080 μs | 0.0333 μs |  0.81 |    0.01 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 4.135 μs | 4.3341 μs | 0.2376 μs |  1.00 |    0.05 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 4.119 μs | 1.1227 μs | 0.0615 μs |  1.00 |    0.02 | 0.0420 | 0.0381 |     712 B |        1.00 |
