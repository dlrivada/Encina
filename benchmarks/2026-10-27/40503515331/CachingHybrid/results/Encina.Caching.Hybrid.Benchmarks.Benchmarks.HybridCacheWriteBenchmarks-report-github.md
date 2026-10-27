```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.411 μs | 2.447 μs | 0.1341 μs |  0.95 |    0.04 | 0.0114 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 6.178 μs | 4.646 μs | 0.2547 μs |  1.71 |    0.07 | 0.0305 | 0.0153 |   2.98 KB |        2.86 |
| SetAsync                      | 3.611 μs | 1.874 μs | 0.1027 μs |  1.00 |    0.03 | 0.0114 | 0.0076 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.289 μs | 3.173 μs | 0.1739 μs |  0.91 |    0.05 | 0.0114 | 0.0076 |   1.05 KB |        1.01 |
