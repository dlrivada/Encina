```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.461 μs | 2.002 μs | 0.1097 μs |  1.01 |    0.11 | 0.0114 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 6.658 μs | 6.595 μs | 0.3615 μs |  1.94 |    0.23 | 0.0305 | 0.0153 |   2.97 KB |        2.86 |
| SetAsync                      | 3.472 μs | 7.990 μs | 0.4379 μs |  1.01 |    0.16 | 0.0114 | 0.0076 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.733 μs | 1.903 μs | 0.1043 μs |  1.09 |    0.12 | 0.0114 | 0.0076 |   1.05 KB |        1.01 |
