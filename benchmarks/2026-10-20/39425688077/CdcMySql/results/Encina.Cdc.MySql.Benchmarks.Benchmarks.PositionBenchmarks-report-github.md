```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.977 ns |   0.0424 ns | 0.0023 ns |   0.47 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.379 ns |   1.5878 ns | 0.0870 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   795.431 ns |  16.6825 ns | 0.9144 ns |  94.94 |    0.86 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,127.400 ns | 132.1001 ns | 7.2409 ns | 134.57 |    1.42 | 0.0286 |     504 B |       12.60 |
