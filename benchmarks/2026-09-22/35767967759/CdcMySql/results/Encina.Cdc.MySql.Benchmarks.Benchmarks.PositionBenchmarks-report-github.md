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
| CompareFilePositions |     3.974 ns |  0.1484 ns | 0.0081 ns |   0.46 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.667 ns |  2.1153 ns | 0.1159 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   798.766 ns | 58.4707 ns | 3.2050 ns |  92.18 |    1.11 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,180.958 ns | 72.5830 ns | 3.9785 ns | 136.28 |    1.62 | 0.0286 |     504 B |       12.60 |
