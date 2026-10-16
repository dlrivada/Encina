```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   3.268 ns |  2.673 ns | 0.1465 ns |  0.32 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |  10.265 ns |  4.799 ns | 0.2630 ns |  1.00 |    0.03 | 0.0005 |      40 B |        1.00 |
| FromBytes            | 563.224 ns | 40.518 ns | 2.2209 ns | 54.89 |    1.22 | 0.0076 |     688 B |       17.20 |
| ToBytes              | 649.764 ns | 70.366 ns | 3.8570 ns | 63.32 |    1.43 | 0.0057 |     504 B |       12.60 |
