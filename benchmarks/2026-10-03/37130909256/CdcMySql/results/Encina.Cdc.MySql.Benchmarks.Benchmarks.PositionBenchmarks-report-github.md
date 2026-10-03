```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.429 ns |   0.1450 ns |  0.0079 ns |   0.38 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.951 ns |   6.5104 ns |  0.3569 ns |   1.00 |    0.05 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   715.346 ns | 365.4760 ns | 20.0330 ns |  80.00 |    3.42 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,078.377 ns |  44.8789 ns |  2.4600 ns | 120.60 |    4.25 | 0.0286 |     504 B |       12.60 |
