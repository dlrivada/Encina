```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean         | Error     | StdDev     | Median       | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|----------:|-----------:|-------------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     4.002 ns | 0.0227 ns |  0.0318 ns |     4.027 ns |   0.46 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.731 ns | 0.0345 ns |  0.0495 ns |     8.724 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   787.080 ns | 2.8485 ns |  3.8026 ns |   785.654 ns |  90.15 |    0.66 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,196.782 ns | 8.0789 ns | 11.0586 ns | 1,196.983 ns | 137.08 |    1.46 | 0.0286 |     504 B |       12.60 |
