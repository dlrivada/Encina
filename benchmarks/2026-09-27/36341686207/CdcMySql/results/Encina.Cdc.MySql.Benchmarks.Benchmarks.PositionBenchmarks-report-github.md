```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     4.011 ns |  1.328 ns | 0.0728 ns |   0.47 |    0.02 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.603 ns |  5.866 ns | 0.3215 ns |   1.00 |    0.05 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   766.057 ns | 67.148 ns | 3.6806 ns |  89.13 |    2.87 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,113.795 ns | 57.981 ns | 3.1781 ns | 129.59 |    4.15 | 0.0286 |     504 B |       12.60 |
