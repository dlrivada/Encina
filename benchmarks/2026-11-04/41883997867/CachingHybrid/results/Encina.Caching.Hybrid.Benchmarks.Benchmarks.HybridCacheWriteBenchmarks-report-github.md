```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 4.911 μs | 1.826 μs | 0.1001 μs |  1.12 |    0.03 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 9.950 μs | 2.584 μs | 0.1416 μs |  2.27 |    0.05 | 0.1373 | 0.1221 |   2.39 KB |        2.30 |
| SetAsync                      | 4.380 μs | 1.690 μs | 0.0927 μs |  1.00 |    0.03 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.432 μs | 2.937 μs | 0.1610 μs |  1.01 |    0.04 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
