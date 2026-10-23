```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     4.057 ns |   0.1110 ns |  0.0061 ns |   0.48 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.424 ns |   0.6323 ns |  0.0347 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   756.434 ns |  62.8162 ns |  3.4432 ns |  89.79 |    0.48 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,173.060 ns | 424.4308 ns | 23.2645 ns | 139.25 |    2.44 | 0.0286 |     504 B |       12.60 |
