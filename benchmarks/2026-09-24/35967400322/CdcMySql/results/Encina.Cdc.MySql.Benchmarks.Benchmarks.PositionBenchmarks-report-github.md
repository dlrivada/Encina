```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.973 ns |   0.0219 ns | 0.0012 ns |   0.47 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.462 ns |   4.3871 ns | 0.2405 ns |   1.00 |    0.04 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   776.636 ns | 108.0923 ns | 5.9249 ns |  91.83 |    2.36 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,121.875 ns |  33.5283 ns | 1.8378 ns | 132.65 |    3.30 | 0.0286 |     504 B |       12.60 |
