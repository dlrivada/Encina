```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.91GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.793 μs | 0.2529 μs | 0.0139 μs |  0.98 |    0.09 | 0.0076 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.217 μs | 8.1916 μs | 0.4490 μs |  1.13 |    0.17 | 0.0076 | 0.0038 |     720 B |        1.01 |
| SetAsync                      | 2.868 μs | 5.0184 μs | 0.2751 μs |  1.01 |    0.12 | 0.0076 | 0.0038 |     712 B |        1.00 |
