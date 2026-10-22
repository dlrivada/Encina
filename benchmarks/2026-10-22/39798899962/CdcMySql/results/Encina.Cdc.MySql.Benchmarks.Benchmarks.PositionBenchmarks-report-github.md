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
| CompareFilePositions |   1.722 ns |   1.730 ns |  0.0948 ns |   0.31 |    0.02 |      - |         - |        0.00 |
| CreateGtidPosition   |   5.490 ns |   1.928 ns |  0.1057 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 444.267 ns | 188.781 ns | 10.3477 ns |  80.94 |    2.11 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 661.299 ns | 154.521 ns |  8.4698 ns | 120.49 |    2.39 | 0.0296 |     504 B |       12.60 |
