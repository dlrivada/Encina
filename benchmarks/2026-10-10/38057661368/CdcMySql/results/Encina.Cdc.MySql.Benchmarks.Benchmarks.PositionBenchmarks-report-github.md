```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   1.616 ns |   2.301 ns |  0.1261 ns |   0.34 |    0.02 |      - |         - |        0.00 |
| CreateGtidPosition   |   4.751 ns |   2.038 ns |  0.1117 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 432.871 ns |  87.835 ns |  4.8145 ns |  91.14 |    2.08 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 666.904 ns | 437.452 ns | 23.9782 ns | 140.42 |    5.25 | 0.0296 |     504 B |       12.60 |
