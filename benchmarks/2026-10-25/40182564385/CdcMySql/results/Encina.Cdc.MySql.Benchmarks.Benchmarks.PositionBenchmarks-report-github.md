```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.972 ns |  0.0089 ns | 0.0005 ns |   0.42 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.568 ns |  2.0228 ns | 0.1109 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   792.883 ns | 47.7819 ns | 2.6191 ns |  82.88 |    0.87 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,165.930 ns | 44.9218 ns | 2.4623 ns | 121.87 |    1.25 | 0.0286 |     504 B |       12.60 |
