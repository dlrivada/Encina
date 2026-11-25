```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error         | StdDev      | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|--------------:|------------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   1.849 ns |     0.5449 ns |   0.0299 ns |   0.43 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   4.333 ns |     1.5573 ns |   0.0854 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 422.782 ns |   150.3863 ns |   8.2432 ns |  97.60 |    2.36 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 710.065 ns | 1,895.5152 ns | 103.8996 ns | 163.92 |   20.97 | 0.0296 |     504 B |       12.60 |
