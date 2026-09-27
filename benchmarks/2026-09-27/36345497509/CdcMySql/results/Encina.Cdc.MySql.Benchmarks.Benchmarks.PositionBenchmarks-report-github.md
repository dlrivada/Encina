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
| CompareFilePositions |     3.977 ns |   0.2243 ns | 0.0123 ns |   0.48 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.283 ns |   3.4647 ns | 0.1899 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   779.242 ns | 106.0413 ns | 5.8125 ns |  94.11 |    1.99 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,136.077 ns |  38.0513 ns | 2.0857 ns | 137.20 |    2.77 | 0.0286 |     504 B |       12.60 |
