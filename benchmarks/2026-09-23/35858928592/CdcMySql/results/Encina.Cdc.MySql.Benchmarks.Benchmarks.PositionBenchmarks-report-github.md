```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   3.012 ns |  0.0671 ns | 0.0037 ns |   0.45 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.743 ns |  2.0730 ns | 0.1136 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 561.688 ns | 59.9287 ns | 3.2849 ns |  83.32 |    1.28 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 838.071 ns | 60.2015 ns | 3.2998 ns | 124.32 |    1.85 | 0.0296 |     504 B |       12.60 |
