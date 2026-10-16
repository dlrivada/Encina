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
| CompareFilePositions |     3.976 ns |   0.0411 ns |  0.0023 ns |   0.44 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.055 ns |   1.4961 ns |  0.0820 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   793.372 ns |  16.3161 ns |  0.8943 ns |  87.63 |    0.69 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,169.022 ns | 287.0766 ns | 15.7356 ns | 129.12 |    1.81 | 0.0286 |     504 B |       12.60 |
