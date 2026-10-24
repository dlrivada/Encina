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
| CompareFilePositions |     3.972 ns |  0.0510 ns | 0.0028 ns |   0.46 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.688 ns |  3.2865 ns | 0.1801 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   785.052 ns | 35.8012 ns | 1.9624 ns |  90.38 |    1.62 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,160.892 ns | 32.9557 ns | 1.8064 ns | 133.65 |    2.38 | 0.0286 |     504 B |       12.60 |
