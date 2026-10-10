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
| CompareFilePositions |     4.030 ns |  2.0172 ns | 0.1106 ns |   0.52 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     7.766 ns |  0.4525 ns | 0.0248 ns |   1.00 |    0.00 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   766.070 ns |  9.2644 ns | 0.5078 ns |  98.64 |    0.28 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,147.667 ns | 23.8139 ns | 1.3053 ns | 147.78 |    0.43 | 0.0286 |     504 B |       12.60 |
