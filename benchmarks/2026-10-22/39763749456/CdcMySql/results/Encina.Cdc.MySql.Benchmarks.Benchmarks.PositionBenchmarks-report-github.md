```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.34GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   1.740 ns |   1.077 ns |  0.0590 ns |   0.30 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   5.856 ns |   1.497 ns |  0.0821 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 442.868 ns | 354.716 ns | 19.4432 ns |  75.63 |    3.02 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 666.702 ns |  84.574 ns |  4.6358 ns | 113.86 |    1.54 | 0.0296 |     504 B |       12.60 |
