```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.732 ns |  0.1100 ns | 0.0060 ns |   0.47 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     7.907 ns |  2.2646 ns | 0.1241 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   760.846 ns | 37.6433 ns | 2.0634 ns |  96.24 |    1.32 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,123.205 ns | 20.1150 ns | 1.1026 ns | 142.07 |    1.92 | 0.0286 |     504 B |       12.60 |
