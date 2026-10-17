```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error         | StdDev      | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|--------------:|------------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   1.739 ns |     0.1131 ns |   0.0062 ns |   0.33 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |   5.218 ns |     1.1665 ns |   0.0639 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 442.085 ns |   364.9264 ns |  20.0028 ns |  84.74 |    3.44 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 729.988 ns | 1,920.8615 ns | 105.2889 ns | 139.92 |   17.54 | 0.0296 |     504 B |       12.60 |
