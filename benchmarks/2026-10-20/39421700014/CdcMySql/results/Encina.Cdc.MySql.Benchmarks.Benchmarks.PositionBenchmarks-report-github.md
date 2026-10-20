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
| CompareFilePositions |     3.982 ns |   0.1590 ns | 0.0087 ns |   0.46 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.697 ns |   2.5471 ns | 0.1396 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   797.029 ns |  39.9524 ns | 2.1899 ns |  91.66 |    1.29 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,125.383 ns | 148.1103 ns | 8.1184 ns | 129.42 |    1.96 | 0.0286 |     504 B |       12.60 |
