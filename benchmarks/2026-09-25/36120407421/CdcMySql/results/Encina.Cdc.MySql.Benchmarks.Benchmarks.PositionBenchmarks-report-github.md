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
| CompareFilePositions |     3.975 ns |   0.0381 ns | 0.0021 ns |   0.45 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.754 ns |   4.5085 ns | 0.2471 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   793.347 ns |  15.2713 ns | 0.8371 ns |  90.67 |    2.22 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,158.231 ns | 123.1960 ns | 6.7528 ns | 132.38 |    3.31 | 0.0286 |     504 B |       12.60 |
