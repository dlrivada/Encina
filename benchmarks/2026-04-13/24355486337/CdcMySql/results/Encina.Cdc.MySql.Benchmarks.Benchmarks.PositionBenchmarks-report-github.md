```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean         | Error     | StdDev     | Median       | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|----------:|-----------:|-------------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.972 ns | 0.0023 ns |  0.0032 ns |     3.972 ns |   0.49 |    0.03 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.163 ns | 0.3497 ns |  0.5126 ns |     7.778 ns |   1.00 |    0.09 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   784.593 ns | 6.4160 ns |  9.2016 ns |   787.280 ns |  96.48 |    6.03 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,146.604 ns | 7.4229 ns | 10.8803 ns | 1,145.016 ns | 140.99 |    8.77 | 0.0286 |     504 B |       12.60 |
